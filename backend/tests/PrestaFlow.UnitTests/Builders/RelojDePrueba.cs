using Microsoft.Extensions.Time.Testing;
using PrestaFlow.Application.Services;

namespace PrestaFlow.UnitTests.Builders;

public static class RelojDePrueba
{
    private static readonly TimeZoneInfo ZonaArgentina =
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    public static RelojNegocio Crear(DateTime instanteUtc) =>
        Crear(new FakeTimeProvider(new DateTimeOffset(instanteUtc)));

    public static RelojNegocio Crear(TimeProvider tiempo) => new(tiempo, ZonaArgentina);
}
