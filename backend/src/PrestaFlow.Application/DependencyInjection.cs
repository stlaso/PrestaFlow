using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Application.Services;

namespace PrestaFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<ClienteService>();

        services.AddScoped<IClienteService, ClienteService>();

        return services;
    }
}
