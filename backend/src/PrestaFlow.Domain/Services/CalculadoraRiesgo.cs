using PrestaFlow.Domain.Enums;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.Domain.Services;

public static class CalculadoraRiesgo
{
    private const decimal LimiteBajo = 0.20m;
    private const decimal LimiteMedio = 0.30m;

    public static NivelRiesgo Calcular(decimal cuota, decimal ingresoMensual)
    {
        if (cuota <= 0)
            throw new DomainException("La cuota debe ser mayor a cero.");

        if (ingresoMensual <= 0)
            throw new DomainException("El ingreso mensual debe ser mayor a cero.");

        var proporcion = cuota / ingresoMensual;

        return proporcion switch
        {
            <= LimiteBajo => NivelRiesgo.Bajo,
            <= LimiteMedio => NivelRiesgo.Medio,
            _ => NivelRiesgo.Alto
        };
    }
}
