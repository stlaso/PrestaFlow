namespace PrestaFlow.Application.Services;

// Los instantes se guardan en UTC; las reglas de días ("posterior a hoy", edades) usan el día de la zona del negocio.
public sealed class RelojNegocio(TimeProvider timeProvider, TimeZoneInfo zonaHoraria)
{
    public DateTime AhoraUtc() => timeProvider.GetUtcNow().UtcDateTime;

    public DateOnly DiaLocal(DateTime instanteUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instanteUtc, zonaHoraria));

    public DateOnly Hoy() => DiaLocal(AhoraUtc());
}
