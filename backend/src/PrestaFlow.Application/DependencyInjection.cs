using Microsoft.Extensions.DependencyInjection;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Application.Services;

namespace PrestaFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClienteService, ClienteService>();

        return services;
    }
}
