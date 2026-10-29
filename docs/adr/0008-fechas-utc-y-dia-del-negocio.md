# ADR-0008: Instantes en UTC y reglas de calendario en el día del negocio

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

El servidor en Azure corre en UTC y el negocio opera en Argentina (UTC-3). A las 22:30 en Argentina ya es el día siguiente en UTC: una regla como "el primer pago debe ser posterior a hoy" o "el cliente debe ser mayor de 18" daría resultados distintos según qué "hoy" se use. Además, una regla que lee la hora del sistema no se puede testear.

## Decisión

- El Domain no lee la hora: recibe las fechas por parámetro.
- Se distinguen dos conceptos:
  - **Instantes** (creación, resolución, baja): `DateTime` en UTC.
  - **Días del calendario** (fecha de nacimiento, primer pago, día de creación): `DateOnly`, sin hora ni zona.
- `RelojNegocio` en Application, sobre `TimeProvider`:

```csharp
public DateTime AhoraUtc() => timeProvider.GetUtcNow().UtcDateTime;
public DateOnly DiaLocal(DateTime instanteUtc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instanteUtc, zonaHoraria));
public DateOnly Hoy() => DiaLocal(AhoraUtc());
```

- La zona se configura en `appsettings.json` (`Negocio:ZonaHoraria`, id IANA `America/Argentina/Buenos_Aires`) y no se toma de la máquina.
- El reloj se lee una sola vez por operación, y de esa lectura salen el instante y el día. `Solicitud` recibe `fechaCreacion` (UTC) y `diaCreacion` (local).
- Las fechas que se leen de la base se marcan como UTC con un `ValueConverter` global: `datetime2` no guarda la zona y, sin esto, EF devuelve `Kind = Unspecified` y el JSON sale sin la `Z`.

```csharp
configurationBuilder.Properties<DateTime>().HaveConversion<FechaUtcConverter>();
// al leer: DateTime.SpecifyKind(valor, DateTimeKind.Utc)
```

## Alternativas consideradas

- **`DateTime.Now` / `TimeZoneInfo.Local`:** el resultado depende de la configuración del servidor y no se puede testear.
- **Interfaz propia `IReloj`:** innecesaria; `TimeProvider` ya es la abstracción y se reemplaza por `FakeTimeProvider` en los tests.
- **`ToUniversalTime()` en el converter:** sobre un valor `Unspecified` asume la hora local del servidor; funcionaría en Azure y correría las fechas 3 horas en una máquina en Argentina.
- **`DateTimeOffset` en las columnas:** guarda la zona en el dato, pero obliga a cambiar el Domain, los DTOs y migrar datos; el sistema ya tiene la regla "todo instante es UTC".

## Consecuencias

- Las reglas de calendario dan el mismo resultado en cualquier servidor.
- Los tests pueden fijar y adelantar la hora.
- Si el negocio opera en varias zonas, la zona dejaría de ser global y pasaría a depender de la sucursal o del cliente; el Domain no cambiaría porque recibe el día ya resuelto.
- Todo `DateTime` nuevo que se persista tiene que ser UTC, porque el converter lo etiqueta así al leerlo.
