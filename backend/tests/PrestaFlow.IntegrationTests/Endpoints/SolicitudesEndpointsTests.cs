using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Services;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;
using PrestaFlow.IntegrationTests.Infraestructura;

namespace PrestaFlow.IntegrationTests.Endpoints;

public class SolicitudesEndpointsTests(PrestaFlowApiFactory factory) : PruebaDeIntegracion(factory)
{
    private const string Ruta = "/api/solicitudes";
    private const string RutaClientes = "/api/clientes";
    private const int IdInexistente = 999_999;

    // Con una cuota de 10.000: 2%, 25% y 50% del ingreso.
    private const decimal IngresoRiesgoBajo = 500_000m;
    private const decimal IngresoRiesgoMedio = 40_000m;
    private const decimal IngresoRiesgoAlto = 20_000m;

    private DateTime Ahora => Factory.Tiempo.GetUtcNow().UtcDateTime;

    private CrearSolicitudRequest CrearRequest(int clienteId, decimal ingreso = IngresoRiesgoBajo, int cuotas = 12)
    {
        var hoy = Factory.Services.GetRequiredService<RelojNegocio>().Hoy();

        return new CrearSolicitudRequest(clienteId, 120_000m, cuotas, ingreso, hoy.AddMonths(1));
    }

    private async Task<ClienteResponse> CrearClienteAsync(string dni = "30123456")
    {
        var request = new CrearClienteRequest(
            Nombre: "Ana",
            Apellido: "Pérez",
            Dni: dni,
            Provincia: "Mendoza",
            Localidad: "Godoy Cruz",
            Telefono: "2614567890",
            Email: "ana.perez@example.com",
            FechaNacimiento: new DateOnly(1990, 5, 15),
            Ocupacion: "Docente");

        var respuesta = await Http.PostAsJsonAsync(RutaClientes, request, Ct);
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ClienteResponse>(Ct))!;
    }

    private async Task<SolicitudResponse> CrearSolicitudAsync(int clienteId, decimal ingreso = IngresoRiesgoBajo)
    {
        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(clienteId, ingreso), Ct);
        respuesta.EnsureSuccessStatusCode();

        return (await LeerSolicitudAsync(respuesta))!;
    }

    private static Task<SolicitudResponse?> LeerSolicitudAsync(HttpResponseMessage respuesta) =>
        respuesta.Content.ReadFromJsonAsync<SolicitudResponse>(JsonApi, Ct);

    private async Task<SolicitudResponse> ObtenerAsync(int id) =>
        (await Http.GetFromJsonAsync<SolicitudResponse>($"{Ruta}/{id}", JsonApi, Ct))!;

    private async Task<PaginaResponse<SolicitudResponse>> ListarAsync(string query = "") =>
        (await Http.GetFromJsonAsync<PaginaResponse<SolicitudResponse>>($"{Ruta}{query}", JsonApi, Ct))!;

    private Task<HttpResponseMessage> AprobarAsync(int id) =>
        Http.PostAsync($"{Ruta}/{id}/aprobar", null, Ct);

    private Task<HttpResponseMessage> RechazarAsync(int id, string motivo = "Ingresos insuficientes") =>
        Http.PostAsJsonAsync($"{Ruta}/{id}/rechazar", new RechazarSolicitudRequest(motivo), Ct);

    // Tres solicitudes de tres clientes, con estado y riesgo distintos.
    private async Task<(SolicitudResponse Pendiente, SolicitudResponse Aprobada, SolicitudResponse Rechazada)> CrearUnaDeCadaEstadoAsync()
    {
        var pendiente = await CrearSolicitudAsync((await CrearClienteAsync("30000001")).Id, IngresoRiesgoBajo);
        var aprobada = await CrearSolicitudAsync((await CrearClienteAsync("30000002")).Id, IngresoRiesgoMedio);
        var rechazada = await CrearSolicitudAsync((await CrearClienteAsync("30000003")).Id, IngresoRiesgoAlto);

        (await AprobarAsync(aprobada.Id)).EnsureSuccessStatusCode();
        (await RechazarAsync(rechazada.Id)).EnsureSuccessStatusCode();

        return (pendiente, aprobada, rechazada);
    }

    [Fact]
    public async Task Crear_ConDatosValidos_Devuelve201ConLoCalculadoYLoPersiste()
    {
        var cliente = await CrearClienteAsync();
        var request = CrearRequest(cliente.Id, IngresoRiesgoMedio);

        var respuesta = await Http.PostAsJsonAsync(Ruta, request, Ct);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creada = await LeerSolicitudAsync(respuesta);
        Assert.NotNull(creada);
        Assert.True(creada.Id > 0);
        Assert.Equal($"{Ruta}/{creada.Id}", respuesta.Headers.Location?.AbsolutePath);
        Assert.Equal(cliente.Id, creada.Cliente.Id);
        Assert.Equal(10_000m, creada.Cuota);
        Assert.Equal(NivelRiesgo.Medio, creada.Riesgo);
        Assert.Equal(EstadoSolicitud.Pendiente, creada.Estado);
        Assert.Equal(Ahora, creada.FechaCreacion);
        Assert.Equal(creada, await ObtenerAsync(creada.Id));
    }

    [Fact]
    public async Task Crear_ConClienteInexistente_Devuelve400ConElErrorEnClienteId()
    {
        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(IdInexistente), Ct);

        var problema = await LeerProblemaAsync<ValidationProblemDetails>(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains(nameof(CrearSolicitudRequest.ClienteId), problema.Errors.Keys);
    }

    [Fact]
    public async Task Crear_QueIncumpleUnaReglaDeNegocio_Devuelve400()
    {
        var cliente = await CrearClienteAsync();

        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(cliente.Id, cuotas: 61), Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains("cuotas", problema.Detail);
    }

    [Fact]
    public async Task Crear_ConOtraPendienteDelMismoCliente_Devuelve409()
    {
        var cliente = await CrearClienteAsync();
        await CrearSolicitudAsync(cliente.Id);

        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(cliente.Id), Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("ya tiene una solicitud pendiente", problema.Detail);
    }

    [Fact]
    public async Task Crear_ConClienteDadoDeBaja_Devuelve409()
    {
        var cliente = await CrearClienteAsync();
        (await Http.DeleteAsync($"{RutaClientes}/{cliente.Id}", Ct)).EnsureSuccessStatusCode();

        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(cliente.Id), Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("dado de baja", problema.Detail);
    }

    [Fact]
    public async Task Crear_ConSolicitudesYaResueltas_PermiteUnaNueva()
    {
        var cliente = await CrearClienteAsync();
        var aprobada = await CrearSolicitudAsync(cliente.Id);
        (await AprobarAsync(aprobada.Id)).EnsureSuccessStatusCode();
        var rechazada = await CrearSolicitudAsync(cliente.Id);
        (await RechazarAsync(rechazada.Id)).EnsureSuccessStatusCode();

        await CrearSolicitudAsync(cliente.Id);

        var historial = await ListarAsync($"?clienteId={cliente.Id}");
        Assert.Equal(3, historial.Total);
    }

    [Fact]
    public async Task ObtenerPorId_ConIdInexistente_Devuelve404()
    {
        var respuesta = await Http.GetAsync($"{Ruta}/{IdInexistente}", Ct);

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClienteDadoDeBaja_SusSolicitudesSiguenVisibles()
    {
        var cliente = await CrearClienteAsync();
        var solicitud = await CrearSolicitudAsync(cliente.Id);
        (await AprobarAsync(solicitud.Id)).EnsureSuccessStatusCode();
        (await Http.DeleteAsync($"{RutaClientes}/{cliente.Id}", Ct)).EnsureSuccessStatusCode();

        var obtenida = await ObtenerAsync(solicitud.Id);
        var listado = await ListarAsync();

        Assert.Equal(cliente.Dni, obtenida.Cliente.Dni);
        Assert.Equal(solicitud.Id, Assert.Single(listado.Items).Id);
        Assert.Equal(1, listado.Total);
    }

    [Fact]
    public async Task Listar_FiltraPorEstado()
    {
        var (_, aprobada, _) = await CrearUnaDeCadaEstadoAsync();

        var pagina = await ListarAsync("?estado=Aprobada");

        Assert.Equal(aprobada.Id, Assert.Single(pagina.Items).Id);
        Assert.Equal(1, pagina.Total);
    }

    [Fact]
    public async Task Listar_FiltraPorRiesgo()
    {
        var (_, _, rechazada) = await CrearUnaDeCadaEstadoAsync();

        var pagina = await ListarAsync("?riesgo=Alto");

        Assert.Equal(rechazada.Id, Assert.Single(pagina.Items).Id);
    }

    [Fact]
    public async Task Listar_FiltraPorCliente()
    {
        var (pendiente, _, _) = await CrearUnaDeCadaEstadoAsync();

        var pagina = await ListarAsync($"?clienteId={pendiente.Cliente.Id}");

        Assert.Equal(pendiente.Id, Assert.Single(pagina.Items).Id);
    }

    [Fact]
    public async Task Listar_CombinaLosFiltros()
    {
        await CrearUnaDeCadaEstadoAsync();

        var pagina = await ListarAsync("?estado=Pendiente&riesgo=Alto");

        Assert.Empty(pagina.Items);
        Assert.Equal(0, pagina.Total);
    }

    [Fact]
    public async Task Listar_PaginaLosResultadosConElTotalReal()
    {
        await CrearUnaDeCadaEstadoAsync();

        var primera = await ListarAsync("?pagina=1&tamanioPagina=2");
        var segunda = await ListarAsync("?pagina=2&tamanioPagina=2");

        Assert.Equal(2, primera.Items.Count);
        Assert.Single(segunda.Items);
        Assert.Equal(3, primera.Total);
        Assert.Equal(3, segunda.Total);
        Assert.Empty(primera.Items.Select(s => s.Id).Intersect(segunda.Items.Select(s => s.Id)));
    }

    [Fact]
    public async Task Listar_ConPaginaFueraDeRango_Devuelve200VacioConElTotalReal()
    {
        await CrearUnaDeCadaEstadoAsync();

        var pagina = await ListarAsync("?pagina=5");

        Assert.Empty(pagina.Items);
        Assert.Equal(3, pagina.Total);
    }

    [Theory]
    [InlineData("?tamanioPagina=101", nameof(ListarSolicitudesRequest.TamanioPagina))]
    [InlineData("?pagina=0", nameof(ListarSolicitudesRequest.Pagina))]
    public async Task Listar_ConPaginacionInvalida_Devuelve400(string query, string campo)
    {
        var respuesta = await Http.GetAsync($"{Ruta}{query}", Ct);

        var problema = await LeerProblemaAsync<ValidationProblemDetails>(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains(campo, problema.Errors.Keys);
    }

    [Fact]
    public async Task Listar_OrdenaPorFechaDescendenteYDesempataPorId()
    {
        var primera = await CrearSolicitudAsync((await CrearClienteAsync("30000001")).Id);
        var mismoInstante = await CrearSolicitudAsync((await CrearClienteAsync("30000002")).Id);
        Factory.Tiempo.Advance(TimeSpan.FromHours(1));
        var masNueva = await CrearSolicitudAsync((await CrearClienteAsync("30000003")).Id);

        var pagina = await ListarAsync();

        Assert.Equal([masNueva.Id, mismoInstante.Id, primera.Id], pagina.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Aprobar_UnaPendiente_Devuelve200YPersisteLaFechaDelReloj()
    {
        var solicitud = await CrearSolicitudAsync((await CrearClienteAsync()).Id);
        Factory.Tiempo.Advance(TimeSpan.FromHours(1));

        var respuesta = await AprobarAsync(solicitud.Id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var persistida = await ObtenerAsync(solicitud.Id);
        Assert.Equal(EstadoSolicitud.Aprobada, persistida.Estado);
        Assert.Equal(Ahora, persistida.FechaResolucion);
    }

    [Fact]
    public async Task Aprobar_ConIdInexistente_Devuelve404()
    {
        await LeerProblemaAsync(await AprobarAsync(IdInexistente), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Aprobar_UnaYaResuelta_Devuelve409()
    {
        var solicitud = await CrearSolicitudAsync((await CrearClienteAsync()).Id);
        (await RechazarAsync(solicitud.Id)).EnsureSuccessStatusCode();

        var problema = await LeerProblemaAsync(await AprobarAsync(solicitud.Id), HttpStatusCode.Conflict);

        Assert.Contains("pendiente", problema.Detail);
        Assert.Equal(EstadoSolicitud.Rechazada, (await ObtenerAsync(solicitud.Id)).Estado);
    }

    [Fact]
    public async Task Rechazar_UnaPendiente_Devuelve200YPersisteElMotivo()
    {
        var solicitud = await CrearSolicitudAsync((await CrearClienteAsync()).Id);

        var respuesta = await RechazarAsync(solicitud.Id, "Ingresos insuficientes");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var persistida = await ObtenerAsync(solicitud.Id);
        Assert.Equal(EstadoSolicitud.Rechazada, persistida.Estado);
        Assert.Equal("Ingresos insuficientes", persistida.MotivoRechazo);
        Assert.Equal(Ahora, persistida.FechaResolucion);
    }

    [Fact]
    public async Task Rechazar_ConMotivoEnBlanco_Devuelve400YSigueSiendoPendiente()
    {
        var solicitud = await CrearSolicitudAsync((await CrearClienteAsync()).Id);

        var problema = await LeerProblemaAsync(await RechazarAsync(solicitud.Id, "   "), HttpStatusCode.BadRequest);

        Assert.Contains("motivo", problema.Detail);
        Assert.Equal(EstadoSolicitud.Pendiente, (await ObtenerAsync(solicitud.Id)).Estado);
    }

    [Fact]
    public async Task Rechazar_ConMotivoDemasiadoLargo_Devuelve400ConElErrorEnMotivo()
    {
        var solicitud = await CrearSolicitudAsync((await CrearClienteAsync()).Id);
        var motivo = new string('a', Solicitud.LargoMaximoMotivo + 1);

        var respuesta = await RechazarAsync(solicitud.Id, motivo);

        var problema = await LeerProblemaAsync<ValidationProblemDetails>(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains(nameof(RechazarSolicitudRequest.Motivo), problema.Errors.Keys);
    }

    [Fact]
    public async Task Rechazar_ConIdInexistente_Devuelve404()
    {
        await LeerProblemaAsync(await RechazarAsync(IdInexistente), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rechazar_UnaYaResuelta_Devuelve409()
    {
        var solicitud = await CrearSolicitudAsync((await CrearClienteAsync()).Id);
        (await AprobarAsync(solicitud.Id)).EnsureSuccessStatusCode();

        var problema = await LeerProblemaAsync(await RechazarAsync(solicitud.Id), HttpStatusCode.Conflict);

        Assert.Contains("pendiente", problema.Detail);
        Assert.Equal(EstadoSolicitud.Aprobada, (await ObtenerAsync(solicitud.Id)).Estado);
    }

    // JSON crudo: JsonApi replica Program.cs y no detectaría que los enums dejen de viajar como texto.
    [Fact]
    public async Task Contrato_ElJsonUsaEnumsComoTextoYNoExponeDatosPersonales()
    {
        await CrearSolicitudAsync((await CrearClienteAsync()).Id);

        var cuerpo = await Http.GetStringAsync(Ruta, Ct);

        using var json = JsonDocument.Parse(cuerpo);
        var pagina = json.RootElement;
        Assert.Equal(["items", "pagina", "tamanioPagina", "total"], pagina.EnumerateObject().Select(p => p.Name));

        var solicitud = pagina.GetProperty("items").EnumerateArray().Single();
        Assert.Equal("Bajo", solicitud.GetProperty("riesgo").GetString());
        Assert.Equal("Pendiente", solicitud.GetProperty("estado").GetString());
        Assert.EndsWith("Z", solicitud.GetProperty("fechaCreacion").GetString());
        Assert.Equal(
            ["id", "nombre", "apellido", "dni"],
            solicitud.GetProperty("cliente").EnumerateObject().Select(p => p.Name));
    }
}
