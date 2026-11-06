using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Domain.Entities;
using PrestaFlow.IntegrationTests.Infraestructura;

namespace PrestaFlow.IntegrationTests.Endpoints;

public class ClientesEndpointsTests(PrestaFlowApiFactory factory) : PruebaDeIntegracion(factory)
{
    private const string Ruta = "/api/clientes";
    private const string DniValido = "30123456";
    private const int IdInexistente = 999_999;

    private static CrearClienteRequest CrearRequest(
        string dni = DniValido,
        string nombre = "Ana",
        string apellido = "Pérez") => new(
        Nombre: nombre,
        Apellido: apellido,
        Dni: dni,
        Provincia: "Mendoza",
        Localidad: "Godoy Cruz",
        Telefono: "2614567890",
        Email: "ana.perez@example.com",
        FechaNacimiento: new DateOnly(1990, 5, 15),
        Ocupacion: "Docente");

    private async Task<ClienteResponse> CrearClienteAsync(string dni = DniValido, string apellido = "Pérez")
    {
        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(dni, apellido: apellido), Ct);
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ClienteResponse>(Ct))!;
    }

    private async Task<ClienteResponse> CrearClienteDadoDeBajaAsync(string dni = DniValido)
    {
        var cliente = await CrearClienteAsync(dni);
        (await Http.DeleteAsync($"{Ruta}/{cliente.Id}", Ct)).EnsureSuccessStatusCode();

        return cliente;
    }

    private async Task<List<ClienteResponse>> ListarAsync(bool incluirBajas = false) =>
        (await Http.GetFromJsonAsync<List<ClienteResponse>>($"{Ruta}?incluirBajas={incluirBajas}", Ct))!;

    [Fact]
    public async Task Crear_ConDatosValidos_Devuelve201ConLocationYLoPersiste()
    {
        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(), Ct);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await respuesta.Content.ReadFromJsonAsync<ClienteResponse>(Ct);
        Assert.NotNull(creado);
        Assert.True(creado.Id > 0);
        Assert.Equal($"{Ruta}/{creado.Id}", respuesta.Headers.Location?.AbsolutePath);

        var obtenido = await Http.GetFromJsonAsync<ClienteResponse>(respuesta.Headers.Location, Ct);
        Assert.Equal(creado, obtenido);
    }

    [Fact]
    public async Task Crear_ConCampoDemasiadoLargo_Devuelve400ConElErrorEnEseCampo()
    {
        var request = CrearRequest(nombre: new string('a', Cliente.LargoMaximoTexto + 1));

        var respuesta = await Http.PostAsJsonAsync(Ruta, request, Ct);

        var problema = await LeerProblemaAsync<ValidationProblemDetails>(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains(nameof(CrearClienteRequest.Nombre), problema.Errors.Keys);
    }

    [Fact]
    public async Task Crear_ConDniInvalido_Devuelve400PorReglaDeNegocio()
    {
        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(dni: "30.123.456"), Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains("DNI", problema.Detail);
    }

    [Fact]
    public async Task Crear_ConDniDeUnClienteActivo_Devuelve409()
    {
        await CrearClienteAsync();

        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(), Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("Ya existe un cliente", problema.Detail);
    }

    [Fact]
    public async Task Crear_ConDniDeUnClienteDadoDeBaja_Devuelve409IndicandoReactivarlo()
    {
        var existente = await CrearClienteDadoDeBajaAsync();

        var respuesta = await Http.PostAsJsonAsync(Ruta, CrearRequest(), Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("Reactivalo", problema.Detail);
        Assert.Contains($"Id {existente.Id}", problema.Detail);
    }

    [Fact]
    public async Task ObtenerPorId_ConIdInexistente_Devuelve404()
    {
        var respuesta = await Http.GetAsync($"{Ruta}/{IdInexistente}", Ct);

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObtenerPorId_UnClienteDadoDeBaja_Devuelve404()
    {
        var cliente = await CrearClienteDadoDeBajaAsync();

        var respuesta = await Http.GetAsync($"{Ruta}/{cliente.Id}", Ct);

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listar_PorDefecto_OcultaLasBajasYOrdenaPorApellido()
    {
        await CrearClienteAsync("30000001", apellido: "Zapata");
        await CrearClienteAsync("30000002", apellido: "Alvarez");
        await CrearClienteDadoDeBajaAsync("30000003");

        var lista = await ListarAsync();

        Assert.Equal(["Alvarez", "Zapata"], lista.Select(c => c.Apellido));
    }

    [Fact]
    public async Task Listar_ConIncluirBajas_MuestraLasBajas()
    {
        await CrearClienteAsync("30000001");
        var dadoDeBaja = await CrearClienteDadoDeBajaAsync("30000002");

        var lista = await ListarAsync(incluirBajas: true);

        Assert.Equal(2, lista.Count);
        Assert.False(Assert.Single(lista, c => c.Id == dadoDeBaja.Id).Activo);
    }

    [Fact]
    public async Task DarDeBaja_UnClienteActivo_Devuelve204YRegistraLaFechaDelReloj()
    {
        var cliente = await CrearClienteAsync();

        var respuesta = await Http.DeleteAsync($"{Ruta}/{cliente.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var dadoDeBaja = Assert.Single(await ListarAsync(incluirBajas: true));
        Assert.False(dadoDeBaja.Activo);
        Assert.Equal(Factory.Tiempo.GetUtcNow().UtcDateTime, dadoDeBaja.FechaBaja);
    }

    [Fact]
    public async Task DarDeBaja_ConIdInexistente_Devuelve404()
    {
        var respuesta = await Http.DeleteAsync($"{Ruta}/{IdInexistente}", Ct);

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_ConUnaSolicitudPendiente_Devuelve409YSigueActivo()
    {
        var cliente = await CrearClienteAsync();
        var hoy = DateOnly.FromDateTime(Factory.Tiempo.GetUtcNow().UtcDateTime);
        var solicitud = new CrearSolicitudRequest(cliente.Id, 120_000m, 12, 500_000m, hoy.AddMonths(1));
        (await Http.PostAsJsonAsync("/api/solicitudes", solicitud, Ct)).EnsureSuccessStatusCode();

        var respuesta = await Http.DeleteAsync($"{Ruta}/{cliente.Id}", Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("solicitudes pendientes", problema.Detail);
        Assert.True(Assert.Single(await ListarAsync()).Activo);
    }

    [Fact]
    public async Task DarDeBaja_DosVeces_LaSegundaDevuelve409()
    {
        var cliente = await CrearClienteDadoDeBajaAsync();

        var respuesta = await Http.DeleteAsync($"{Ruta}/{cliente.Id}", Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("ya está dado de baja", problema.Detail);
    }

    [Fact]
    public async Task Reactivar_UnClienteDadoDeBaja_Devuelve200YVuelveAEstarVisible()
    {
        var cliente = await CrearClienteDadoDeBajaAsync();

        var respuesta = await Http.PostAsync($"{Ruta}/{cliente.Id}/reactivar", null, Ct);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var reactivado = await respuesta.Content.ReadFromJsonAsync<ClienteResponse>(Ct);
        Assert.NotNull(reactivado);
        Assert.True(reactivado.Activo);
        Assert.Null(reactivado.FechaBaja);
        Assert.Equal(cliente.Id, Assert.Single(await ListarAsync()).Id);
    }

    [Fact]
    public async Task Reactivar_ConIdInexistente_Devuelve404()
    {
        var respuesta = await Http.PostAsync($"{Ruta}/{IdInexistente}/reactivar", null, Ct);

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reactivar_UnClienteActivo_Devuelve409()
    {
        var cliente = await CrearClienteAsync();

        var respuesta = await Http.PostAsync($"{Ruta}/{cliente.Id}/reactivar", null, Ct);

        var problema = await LeerProblemaAsync(respuesta, HttpStatusCode.Conflict);
        Assert.Contains("ya está activo", problema.Detail);
    }

    // JSON crudo: el DTO compartido no detectaría un cambio de nombres o de formato en el contrato.
    [Fact]
    public async Task Contrato_ElJsonUsaLosNombresYFormatosAcordados()
    {
        await CrearClienteDadoDeBajaAsync();

        var cuerpo = await Http.GetStringAsync($"{Ruta}?incluirBajas=true", Ct);

        using var json = JsonDocument.Parse(cuerpo);
        var cliente = json.RootElement.EnumerateArray().Single();
        Assert.Equal(
            ["id", "nombre", "apellido", "dni", "provincia", "localidad", "telefono", "email", "fechaNacimiento", "ocupacion", "activo", "fechaBaja"],
            cliente.EnumerateObject().Select(p => p.Name));
        Assert.Equal("1990-05-15", cliente.GetProperty("fechaNacimiento").GetString());
        Assert.EndsWith("Z", cliente.GetProperty("fechaBaja").GetString());
    }
}
