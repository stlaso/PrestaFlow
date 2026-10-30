# ADR-0005: Traducción centralizada de errores a Problem Details

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

Los errores se originan en capas que no conocen HTTP (Domain, Application, Infrastructure) y el cliente de la API necesita un formato de error uniforme, sin detalles internos.

## Decisión

- Un único `ManejadorExcepciones : IExceptionHandler`, registrado con `AddProblemDetails()` y `UseExceptionHandler()` como primer middleware del pipeline.
- Traducción por tipo de excepción:

| Excepción | Respuesta | Nivel de log |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 `ValidationProblemDetails` con errores por campo | Information |
| `DomainException` | 400 | Information |
| `ConflictoException` | 409 | Information |
| Cualquier otra | 500 con mensaje genérico | Error, con la excepción completa |

- Todas las respuestas incluyen el `traceId`, que coincide con el del log.
- `UseStatusCodePages()` hace que los 404 de rutas inexistentes también salgan como Problem Details.
- Los errores de persistencia se traducen **en Infrastructure**, dentro de `GuardarCambiosAsync`:
  - `DbUpdateConcurrencyException` → `ConflictoException`.
  - `DbUpdateException` con `SqlException` 2601 o 2627 (índice o restricción única) → `ConflictoException`.
- Cualquier otro error de la base (por ejemplo, el 547 de FK) no se traduce: si ocurre es un bug, y el 500 con log de Error lo hace visible.

## Alternativas consideradas

- **`try/catch` en cada controller:** repite el mapeo y deja formatos inconsistentes.
- **Middleware propio:** posible, pero `IExceptionHandler` (.NET 8+) se integra con `IProblemDetailsService` y no duplica el log de excepciones manejadas.
- **Traducir `SqlException` en la Api:** obligaría a la capa de presentación a conocer el proveedor de base de datos.
- **Atributo `[Produces("application/json")]` en los controllers:** se descartó porque forzaba ese content type también en los 400 automáticos, que deben ser `application/problem+json`.

## Consecuencias

- Los controllers quedan sin manejo de errores.
- Un error reportado por un usuario se busca en los logs por el `traceId` de la respuesta.
- Los 400 automáticos de `[ApiController]` (binder de MVC) todavía salen con mensajes en inglés; se unifican en la Fase 3.
