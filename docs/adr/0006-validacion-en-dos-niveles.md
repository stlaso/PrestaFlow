# ADR-0006: Validación en dos niveles: forma del dato y reglas de negocio

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

Hay validaciones que dependen del formato del request (largos, decimales, paginación) y otras que son reglas del negocio. Repetirlas en varios lugares lleva a que con el tiempo queden distintas.

## Decisión

| Nivel | Valida | Ejemplos |
|---|---|---|
| FluentValidation, en Application | La **forma** del dato y lo que la base no puede guardar | Largos máximos, como máximo 2 decimales, fecha obligatoria, página ≥ 1, tamaño de página entre 1 y 100 |
| Entidad, en Domain | Las **reglas de negocio** | Monto mayor a cero, cantidad de cuotas, fechas, edades, transiciones de estado, motivo de rechazo obligatorio |

- Las reglas no se duplican entre niveles.
- Los validadores se invocan explícitamente desde el service con `ValidateAndThrowAsync`, y se registran con `AddValidatorsFromAssemblyContaining`.
- Los importes usan `PrecisionScale(18, 2, ignoreTrailingZeros: true)`, alineado con `decimal(18,2)`, para que la base no redondee en silencio (`1000.555` se rechaza; `100.500` se acepta).
- Los mensajes están en español y usan los marcadores de FluentValidation (`{MaxLength}`, `{From}`, `{To}`) para no repetir números.
- Las reglas que dependen de "hoy" usan una lambda (`_ => reloj.Hoy()`), que se evalúa en cada validación y no queda congelada según el ciclo de vida del validador.
- Un `ClienteId` inexistente se informa como `ValidationException` sobre el campo `ClienteId` (400), con el mismo formato que un error de formulario.

## Alternativas consideradas

- **Auto-validación de `FluentValidation.AspNetCore`:** está deprecada y oculta cuándo se valida.
- **Data annotations en los DTOs:** menos expresivas para reglas condicionales y mensajes compuestos.
- **Duplicar las reglas de negocio en el validador:** daría todos los errores en una sola respuesta, pero la regla quedaría en dos lugares.

## Consecuencias

- Un request con errores de forma y de negocio los informa en dos vueltas: primero los de forma; los de negocio aparecen cuando la entidad se construye. El frontend compensa validando en el formulario.
- Cambiar una regla de negocio se hace solo en la entidad.
