# ADR-0009: Concurrencia optimista y restricciones únicas como respaldo

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

Cada request trabaja con su propio DbContext y su propia copia de las entidades. Un chequeo hecho en memoria (en el service o en la entidad) no ve lo que hace otro request al mismo tiempo: dos requests pueden leer el mismo estado y pasar los dos el chequeo.

## Decisión

Cada regla tiene una primera defensa en el código, con un mensaje específico, y un respaldo en la base para el caso concurrente:

| Regla | Primera defensa | Respaldo en la base | Respuesta |
|---|---|---|---|
| DNI único | Búsqueda en el service | Índice único en `Dni` (error 2601) | 409 |
| Una solicitud pendiente por cliente | `ExistenPendientesAsync` en el service | Índice único filtrado `UX_Solicitudes_ClienteId_Pendiente` (`WHERE [Estado] = 1`) | 409 |
| Resolver una solicitud una sola vez | Chequeo de estado en la entidad | `RowVersion` (concurrencia optimista) | 409 |

- `RowVersion` es una columna `rowversion` que SQL Server cambia en cada `UPDATE`. EF la agrega al `WHERE` y lee el valor nuevo con `OUTPUT`:

```sql
UPDATE [Solicitudes] SET [Estado] = @p0, [FechaResolucion] = @p1
OUTPUT INSERTED.[RowVersion]
WHERE [Id] = @p2 AND [RowVersion] = @p3;
```

  Si otro request ya modificó la fila, el `UPDATE` afecta 0 filas y EF lanza `DbUpdateConcurrencyException`, que se traduce a 409.
- El índice filtrado permite historial (varias aprobadas o rechazadas por cliente) y exige unicidad solo entre las pendientes. Se declara también el índice común de la FK, para que EF no considere la FK cubierta por el filtrado.
- El filtro del índice se genera desde el enum (`(int)EstadoSolicitud.Pendiente`) y no con un número escrito a mano.

## Alternativas consideradas

- **Concurrencia pesimista** (`UPDLOCK` dentro de una transacción): bloquea a los demás mientras dura la operación. En una API web no se puede mantener un bloqueo entre requests, y bloquear dentro de cada request agrega contención sin beneficio para este volumen.
- **Solo el chequeo en el service:** no cubre requests simultáneos.
- **Solo la restricción en la base:** funciona, pero el mensaje sería genérico en todos los casos.
- **`UNIQUE` común sobre `ClienteId`:** impediría tener historial de solicitudes resueltas.

## Consecuencias

- En el caso concurrente el mensaje es genérico ("El registro fue modificado por otro usuario" o "Ya existe un registro con esos datos"), pero el resultado es correcto.
- Las carreras no se reproducen de forma confiable por HTTP; se verifican con tests de integración que abren dos DbContext (Fase 2).
- Escribir en `Solicitudes` desde scripts requiere `QUOTED_IDENTIFIER ON` por el índice filtrado (`sqlcmd -I`).
- Aplicar la migración del índice falla si ya existen duplicados; habría que limpiarlos antes.
