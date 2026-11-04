using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Application.Services;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;
using PrestaFlow.Domain.Exceptions;
using PrestaFlow.Infrastructure.Persistence;
using PrestaFlow.IntegrationTests.Infraestructura;

namespace PrestaFlow.IntegrationTests.Persistencia;

// Cada scope equivale a un request: los dos leen antes de que el otro guarde, y el orden lo fija el test.
public class ConcurrenciaTests(PrestaFlowApiFactory factory) : PruebaDeIntegracion(factory)
{
    private const string Dni = "30123456";
    private const int ErrorIndiceUnico = 2601;

    private AsyncServiceScope NuevoRequest() => Factory.Services.CreateAsyncScope();

    private static T Obtener<T>(AsyncServiceScope request) where T : notnull =>
        request.ServiceProvider.GetRequiredService<T>();

    private static Task GuardarAsync(AsyncServiceScope request) =>
        Obtener<IUnitOfWork>(request).GuardarCambiosAsync(Ct);

    private static Cliente NuevoCliente() => new(
        "Ana", "Pérez", Dni, "Mendoza", "Godoy Cruz", "2614567890",
        "ana.perez@example.com", new DateOnly(1990, 5, 15), "Docente");

    private Solicitud NuevaSolicitud(Cliente cliente)
    {
        var reloj = Factory.Services.GetRequiredService<RelojNegocio>();
        var hoy = reloj.Hoy();

        return new Solicitud(cliente, 120_000m, 12, 500_000m, hoy.AddMonths(1), reloj.AhoraUtc(), hoy);
    }

    private async Task<int> CrearClienteAsync()
    {
        await using var request = NuevoRequest();
        var cliente = NuevoCliente();
        Obtener<IClienteRepository>(request).Agregar(cliente);
        await GuardarAsync(request);

        return cliente.Id;
    }

    private static void AssertLoFrenoElIndice(ConflictoException conflicto, string indice)
    {
        Assert.Equal("Ya existe un registro con esos datos.", conflicto.Message);
        var errorSql = Assert.IsType<SqlException>(conflicto.InnerException?.InnerException);
        Assert.Equal(ErrorIndiceUnico, errorSql.Number);
        Assert.Contains(indice, errorSql.Message);
    }

    [Fact]
    public async Task DosAltasConElMismoDni_LaSegundaLaFrenaElIndiceUnico()
    {
        await using var requestA = NuevoRequest();
        await using var requestB = NuevoRequest();
        var clientesA = Obtener<IClienteRepository>(requestA);
        var clientesB = Obtener<IClienteRepository>(requestB);

        Assert.Null(await clientesA.ObtenerPorDniIncluyendoBajasAsync(Dni, Ct));
        Assert.Null(await clientesB.ObtenerPorDniIncluyendoBajasAsync(Dni, Ct));

        clientesA.Agregar(NuevoCliente());
        clientesB.Agregar(NuevoCliente());

        await GuardarAsync(requestA);
        var conflicto = await Assert.ThrowsAsync<ConflictoException>(() => GuardarAsync(requestB));

        AssertLoFrenoElIndice(conflicto, "IX_Clientes_Dni");
        await using var lectura = NuevoRequest();
        Assert.Equal(1, await Obtener<PrestaFlowDbContext>(lectura).Clientes.CountAsync(Ct));
    }

    [Fact]
    public async Task DosSolicitudesPendientesDelMismoCliente_LaSegundaLaFrenaElIndiceFiltrado()
    {
        var clienteId = await CrearClienteAsync();
        await using var requestA = NuevoRequest();
        await using var requestB = NuevoRequest();
        var solicitudesA = Obtener<ISolicitudRepository>(requestA);
        var solicitudesB = Obtener<ISolicitudRepository>(requestB);

        Assert.False(await solicitudesA.ExistenPendientesAsync(clienteId, Ct));
        Assert.False(await solicitudesB.ExistenPendientesAsync(clienteId, Ct));

        var clienteA = await Obtener<IClienteRepository>(requestA).ObtenerPorIdAsync(clienteId, Ct);
        var clienteB = await Obtener<IClienteRepository>(requestB).ObtenerPorIdAsync(clienteId, Ct);
        solicitudesA.Agregar(NuevaSolicitud(clienteA!));
        solicitudesB.Agregar(NuevaSolicitud(clienteB!));

        await GuardarAsync(requestA);
        var conflicto = await Assert.ThrowsAsync<ConflictoException>(() => GuardarAsync(requestB));

        AssertLoFrenoElIndice(conflicto, "UX_Solicitudes_ClienteId_Pendiente");
        await using var lectura = NuevoRequest();
        Assert.Equal(1, await Obtener<PrestaFlowDbContext>(lectura).Solicitudes.CountAsync(Ct));
    }

    [Fact]
    public async Task ResolverLaMismaSolicitudDosVeces_GanaElPrimeroYElSegundoLoFrenaRowVersion()
    {
        var clienteId = await CrearClienteAsync();
        int solicitudId;
        await using (var alta = NuevoRequest())
        {
            var cliente = await Obtener<IClienteRepository>(alta).ObtenerPorIdAsync(clienteId, Ct);
            var solicitud = NuevaSolicitud(cliente!);
            Obtener<ISolicitudRepository>(alta).Agregar(solicitud);
            await GuardarAsync(alta);
            solicitudId = solicitud.Id;
        }

        await using var requestA = NuevoRequest();
        await using var requestB = NuevoRequest();
        var enA = await Obtener<ISolicitudRepository>(requestA).ObtenerPorIdAsync(solicitudId, Ct);
        var enB = await Obtener<ISolicitudRepository>(requestB).ObtenerPorIdAsync(solicitudId, Ct);
        var ahora = Factory.Services.GetRequiredService<RelojNegocio>().AhoraUtc();

        // Los dos ven una Pendiente, así que el chequeo de estado de la entidad deja pasar a ambos.
        enA!.Aprobar(ahora);
        enB!.Rechazar("Ingresos insuficientes", ahora);

        await GuardarAsync(requestA);
        var conflicto = await Assert.ThrowsAsync<ConflictoException>(() => GuardarAsync(requestB));

        Assert.Equal("El registro fue modificado por otro usuario. Volvé a cargarlo.", conflicto.Message);
        Assert.IsType<DbUpdateConcurrencyException>(conflicto.InnerException);

        await using var lectura = NuevoRequest();
        var guardada = await Obtener<ISolicitudRepository>(lectura).ObtenerPorIdAsync(solicitudId, Ct);
        Assert.Equal(EstadoSolicitud.Aprobada, guardada!.Estado);
        Assert.Null(guardada.MotivoRechazo);
    }
}
