using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace PrestaFlow.IntegrationTests.Infraestructura;

// xUnit crea una instancia por test: InitializeAsync deja la base vacía antes de cada uno.
[Collection(ColeccionIntegracion.Nombre)]
public abstract class PruebaDeIntegracion(PrestaFlowApiFactory factory) : IAsyncLifetime
{
    // Replica la configuración de Program.cs: la Api manda los enums como texto.
    protected static JsonSerializerOptions JsonApi { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected PrestaFlowApiFactory Factory => factory;
    protected HttpClient Http { get; } = factory.CreateClient();

    public async ValueTask InitializeAsync() => await factory.LimpiarAsync();

    public ValueTask DisposeAsync()
    {
        Http.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected static async Task<T> LeerProblemaAsync<T>(HttpResponseMessage respuesta, HttpStatusCode esperado)
        where T : ProblemDetails
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<T>(Ct);

        Assert.NotNull(problema);
        Assert.True(problema.Extensions.ContainsKey("traceId"), "El Problem Details no incluye traceId.");

        return problema;
    }

    protected static Task<ProblemDetails> LeerProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode esperado) =>
        LeerProblemaAsync<ProblemDetails>(respuesta, esperado);
}
