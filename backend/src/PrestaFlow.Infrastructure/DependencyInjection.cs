using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Infrastructure.Persistence;

namespace PrestaFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PrestaFlow")
            ?? throw new InvalidOperationException("Falta la connection string 'PrestaFlow'.");

        services.AddDbContext<PrestaFlowDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
