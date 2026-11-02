using PrestaFlow.Domain.Enums;
using PrestaFlow.Domain.Exceptions;
using PrestaFlow.Domain.Services;

namespace PrestaFlow.UnitTests.Domain;

public class CalculadoraRiesgoTests
{
    private const decimal Ingreso = 1000m;

    public static TheoryData<decimal, NivelRiesgo> CuotasYRiesgo => new()
    {
        { 1m, NivelRiesgo.Bajo },
        { 200m, NivelRiesgo.Bajo },
        { 200.01m, NivelRiesgo.Medio },
        { 300m, NivelRiesgo.Medio },
        { 300.01m, NivelRiesgo.Alto },
        { 2000m, NivelRiesgo.Alto },
    };

    public static TheoryData<decimal> ValoresNoPositivos => [0m, -0.01m, -100m];

    [Theory]
    [MemberData(nameof(CuotasYRiesgo))]
    public void Calcular_SegunProporcionCuotaIngreso_DevuelveElNivel(decimal cuota, NivelRiesgo esperado)
    {
        var riesgo = CalculadoraRiesgo.Calcular(cuota, Ingreso);

        Assert.Equal(esperado, riesgo);
    }

    [Theory]
    [MemberData(nameof(ValoresNoPositivos))]
    public void Calcular_ConCuotaNoPositiva_LanzaDomainException(decimal cuota)
    {
        var ex = Assert.Throws<DomainException>(() => CalculadoraRiesgo.Calcular(cuota, Ingreso));

        Assert.Contains("cuota", ex.Message);
    }

    [Theory]
    [MemberData(nameof(ValoresNoPositivos))]
    public void Calcular_ConIngresoNoPositivo_LanzaDomainException(decimal ingreso)
    {
        var ex = Assert.Throws<DomainException>(() => CalculadoraRiesgo.Calcular(100m, ingreso));

        Assert.Contains("ingreso", ex.Message);
    }
}
