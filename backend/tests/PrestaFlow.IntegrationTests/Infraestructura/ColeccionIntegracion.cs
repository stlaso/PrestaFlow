namespace PrestaFlow.IntegrationTests.Infraestructura;

// Un solo contenedor para todos los tests; dentro de una colección corren en serie.
[CollectionDefinition(Nombre)]
public sealed class ColeccionIntegracion : ICollectionFixture<PrestaFlowApiFactory>
{
    public const string Nombre = "Integracion";
}
