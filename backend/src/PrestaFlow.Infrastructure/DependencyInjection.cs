using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Infrastructure.Persistence;
using PrestaFlow.Infrastructure.Repositories;

namespace PrestaFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PrestaFlow")
            ?? throw new InvalidOperationException("Falta la connection string 'PrestaFlow'.");

        // 10622 avisa que el filtro de Cliente puede ocultar solicitudes; los repositorios lo ignoran explícitamente al incluirlo.
        services.AddDbContext<PrestaFlowDbContext>(options => options
            .UseSqlServer(connectionString)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        // Reenvía al DbContext ya registrado: con AddScoped<IUnitOfWork, PrestaFlowDbContext>() habría una segunda instancia.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PrestaFlowDbContext>());

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ISolicitudRepository, SolicitudRepository>();

        return services;
    }
}
