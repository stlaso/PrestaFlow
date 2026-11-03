using Microsoft.Extensions.Time.Testing;
using PrestaFlow.Application.Services;
using PrestaFlow.UnitTests.Builders;

namespace PrestaFlow.UnitTests.Application;

public class RelojNegocioTests
{
    private static (RelojNegocio Reloj, FakeTimeProvider Tiempo) CrearReloj(DateTime instanteUtc)
    {
        var tiempo = new FakeTimeProvider(new DateTimeOffset(instanteUtc));

        return (RelojDePrueba.Crear(tiempo), tiempo);
    }

    private static DateTime Utc(int anio, int mes, int dia, int hora, int minuto, int segundo = 0) =>
        new(anio, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);

    public static TheoryData<DateTime, DateOnly> InstantesYDiaLocal => new()
    {
        { Utc(2026, 3, 11, 1, 30), new DateOnly(2026, 3, 10) },
        { Utc(2026, 3, 11, 2, 59, 59), new DateOnly(2026, 3, 10) },
        { Utc(2026, 3, 11, 3, 0), new DateOnly(2026, 3, 11) },
        { Utc(2027, 1, 1, 2, 30), new DateOnly(2026, 12, 31) },
    };

    [Theory]
    [MemberData(nameof(InstantesYDiaLocal))]
    public void Hoy_DevuelveElDiaDeArgentinaYNoElDeUtc(DateTime instanteUtc, DateOnly esperado)
    {
        var (reloj, _) = CrearReloj(instanteUtc);

        Assert.Equal(esperado, reloj.Hoy());
    }

    [Theory]
    [MemberData(nameof(InstantesYDiaLocal))]
    public void DiaLocal_ConvierteElInstanteRecibidoSinImportarLaHoraDelReloj(DateTime instanteUtc, DateOnly esperado)
    {
        var (reloj, _) = CrearReloj(Utc(2030, 6, 15, 12, 0));

        Assert.Equal(esperado, reloj.DiaLocal(instanteUtc));
    }

    [Fact]
    public void AhoraUtc_DevuelveElInstanteConKindUtc()
    {
        var instante = Utc(2026, 3, 11, 1, 30);
        var (reloj, _) = CrearReloj(instante);

        var ahora = reloj.AhoraUtc();

        Assert.Equal(instante, ahora);
        Assert.Equal(DateTimeKind.Utc, ahora.Kind);
    }

    [Fact]
    public void Hoy_LeeElRelojEnCadaLlamada()
    {
        var (reloj, tiempo) = CrearReloj(Utc(2026, 3, 11, 2, 59, 59));
        Assert.Equal(new DateOnly(2026, 3, 10), reloj.Hoy());

        tiempo.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(new DateOnly(2026, 3, 11), reloj.Hoy());
    }
}
