using Microsoft.EntityFrameworkCore;
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

        services.AddDbContext<PrestaFlowDbContext>(options => options.UseSqlServer(connectionString));

        // Reenvía al DbContext ya registrado: con AddScoped<IUnitOfWork, PrestaFlowDbContext>() habría una segunda instancia.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PrestaFlowDbContext>());

        services.AddScoped<IClienteRepository, ClienteRepository>();

        return services;
    }
}
