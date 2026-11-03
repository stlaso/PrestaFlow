using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Services;

namespace PrestaFlow.IntegrationTests.Infraestructura;

public class ApiSmokeTests(PrestaFlowApiFactory factory) : PruebaDeIntegracion(factory)
{
    private async Task<List<ClienteResponse>> ListarClientesAsync()
    {
        var lista = await Http.GetFromJsonAsync<List<ClienteResponse>>("/api/clientes", Ct);

        return lista!;
    }

    [Fact]
    public async Task ListarClientes_ConLaBaseLimpia_DevuelveOkYListaVacia()
    {
        var respuesta = await Http.GetAsync("/api/clientes", Ct);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(await ListarClientesAsync());
    }

    [Fact]
    public async Task LimpiarAsync_BorraLoCreadoPorLaApi()
    {
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

        var alta = await Http.PostAsJsonAsync("/api/clientes", request, Ct);

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        Assert.Single(await ListarClientesAsync());

        await Factory.LimpiarAsync();

        Assert.Empty(await ListarClientesAsync());
    }

    [Fact]
    public void RelojDeLaApi_UsaElTiempoFalso()
    {
        var reloj = Factory.Services.GetRequiredService<RelojNegocio>();

        Assert.Equal(Factory.Tiempo.GetUtcNow().UtcDateTime, reloj.AhoraUtc());
    }
}
