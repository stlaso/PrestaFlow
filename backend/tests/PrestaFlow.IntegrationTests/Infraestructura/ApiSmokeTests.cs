using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Services;

namespace PrestaFlow.IntegrationTests.Infraestructura;

[Collection(ColeccionIntegracion.Nombre)]
public class ApiSmokeTests(PrestaFlowApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HttpClient _cliente = factory.CreateClient();

    private async Task<List<ClienteResponse>> ListarClientesAsync()
    {
        var lista = await _cliente.GetFromJsonAsync<List<ClienteResponse>>("/api/clientes", Ct);

        return lista!;
    }

    [Fact]
    public async Task ListarClientes_ConLaBaseLimpia_DevuelveOkYListaVacia()
    {
        await factory.LimpiarAsync();

        var respuesta = await _cliente.GetAsync("/api/clientes", Ct);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(await ListarClientesAsync());
    }

    [Fact]
    public async Task LimpiarAsync_BorraLoCreadoPorLaApi()
    {
        await factory.LimpiarAsync();
        var request = new CrearClienteRequest(
            Nombre: "Ana",
            Apellido: "Pérez",
            Dni: "30123456",
            Provincia: "Mendoza",
            Localidad: "Godoy Cruz",
            Telefono: "2614567890",
            Email: "ana.perez@example.com",
            FechaNacimiento: new DateOnly(1990, 5, 15),
            Ocupacion: "Docente");

        var alta = await _cliente.PostAsJsonAsync("/api/clientes", request, Ct);

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        Assert.Single(await ListarClientesAsync());

        await factory.LimpiarAsync();

        Assert.Empty(await ListarClientesAsync());
    }

    [Fact]
    public void RelojDeLaApi_UsaElTiempoFalso()
    {
        var reloj = factory.Services.GetRequiredService<RelojNegocio>();

        Assert.Equal(PrestaFlowApiFactory.InstanteInicial.UtcDateTime, reloj.AhoraUtc());
    }
}
