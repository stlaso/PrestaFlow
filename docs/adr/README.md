# Registro de decisiones de arquitectura (ADR)

Cada archivo documenta una decisión técnica: el contexto que la motivó, qué se decidió, qué alternativas se descartaron y por qué, y qué consecuencias tiene.

Las decisiones no se editan para cambiarlas: si una se reemplaza, se escribe un ADR nuevo y el anterior pasa a estado *Reemplazada por ADR-XXXX*.

| ADR | Decisión | Estado |
|---|---|---|
| [0001](0001-arquitectura-en-capas.md) | Arquitectura en capas con inversión de dependencias | Aceptada |
| [0002](0002-unit-of-work-sobre-dbcontext.md) | Unit of Work implementado por el DbContext | Aceptada |
| [0003](0003-entidades-ricas.md) | Entidades ricas y dos tipos de excepción de negocio | Aceptada |
| [0004](0004-sql-server-en-docker-y-secretos.md) | SQL Server en Docker y secretos fuera del repositorio | Aceptada |
| [0005](0005-errores-con-problem-details.md) | Traducción centralizada de errores a Problem Details | Aceptada |
| [0006](0006-validacion-en-dos-niveles.md) | Validación en dos niveles: forma del dato y reglas de negocio | Aceptada |
| [0007](0007-baja-logica-de-clientes.md) | Baja lógica de clientes con filtro global | Aceptada |
| [0008](0008-fechas-utc-y-dia-del-negocio.md) | Instantes en UTC y reglas de calendario en el día del negocio | Aceptada |
| [0009](0009-concurrencia.md) | Concurrencia optimista y restricciones únicas como respaldo | Aceptada |
| [0010](0010-contrato-http.md) | Contrato HTTP: acciones de negocio, DTOs y códigos de estado | Aceptada |
| [0011](0011-listado-paginado.md) | Listado con filtros opcionales y paginación por offset | Aceptada |

## Formato

```markdown
# ADR-XXXX: Título

- **Estado:** Propuesta | Aceptada | Reemplazada por ADR-YYYY
- **Fecha:** AAAA-MM

## Contexto
## Decisión
## Alternativas consideradas
## Consecuencias
```
