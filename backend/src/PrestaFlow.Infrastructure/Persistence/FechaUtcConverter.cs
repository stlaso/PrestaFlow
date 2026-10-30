using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PrestaFlow.Infrastructure.Persistence;

// Todos los instantes del sistema se guardan en UTC (RelojNegocio.AhoraUtc), así que al leerlos se marcan como tales.
public sealed class FechaUtcConverter() : ValueConverter<DateTime, DateTime>(
    valor => valor,
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
