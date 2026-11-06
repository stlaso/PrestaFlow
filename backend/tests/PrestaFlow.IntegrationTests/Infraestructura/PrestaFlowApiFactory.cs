using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PrestaFlow.Infrastructure.Persistence;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace PrestaFlow.IntegrationTests.Infraestructura;

public sealed class PrestaFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ImagenSqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string BaseDeDatos = "PrestaFlowTests";

    public static readonly DateTimeOffset InstanteInicial = new(2026, 3, 10, 15, 0, 0, TimeSpan.Zero);

    private readonly MsSqlContainer _contenedor = new MsSqlBuilder(ImagenSqlServer).Build();
    private string _connectionString = string.Empty;
    private Respawner _respawner = null!;

    public FakeTimeProvider Tiempo { get; } = new(InstanteInicial);

    public async ValueTask InitializeAsync()
    {
        await _contenedor.StartAsync();

        _connectionString = new SqlConnectionStringBuilder(_contenedor.GetConnectionString())
        {
            InitialCatalog = BaseDeDatos
        }.ConnectionString;

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestaFlowDbContext>();

        VerificarBaseDelContenedor(db);
        await db.Database.MigrateAsync();

        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();

        _respawner = await Respawner.CreateAsync(conexion, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public async Task LimpiarAsync()
    {
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        await _respawner.ResetAsync(conexion);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _contenedor.DisposeAsync();
    }

    // Frena antes de migrar si la Api apunta a otra base, como la de desarrollo.
    // Compara servidor y base porque EF agrega otras claves a la connection string.
    private void VerificarBaseDelContenedor(PrestaFlowDbContext db)
    {
        var usada = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        var esperada = new SqlConnectionStringBuilder(_connectionString);

        if (usada.DataSource != esperada.DataSource || usada.InitialCatalog != esperada.InitialCatalog)
            throw new InvalidOperationException("La Api de prueba no está usando la base del contenedor.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Con Development se cargarían los User Secrets, donde está la base local.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PrestaFlow", _connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Tiempo);
        });
    }
}
