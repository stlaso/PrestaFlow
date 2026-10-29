using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Application.Services;

namespace PrestaFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var idZona = configuration["Negocio:ZonaHoraria"]
            ?? throw new InvalidOperationException("Falta la configuración 'Negocio:ZonaHoraria'.");
        var zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(idZona);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new RelojNegocio(sp.GetRequiredService<TimeProvider>(), zonaHoraria));
        services.AddValidatorsFromAssemblyContaining<ClienteService>();

        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ISolicitudService, SolicitudService>();

        return services;
    }
}
