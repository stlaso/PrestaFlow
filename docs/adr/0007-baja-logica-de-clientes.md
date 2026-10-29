# ADR-0007: Baja lógica de clientes con filtro global

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

Un cliente con historial de solicitudes no puede borrarse físicamente: se perdería el historial y la FK lo impide. Aun así, un cliente dado de baja no debe aparecer en las consultas habituales ni recibir solicitudes nuevas.

## Decisión

- `Cliente.FechaBaja` (nula si está activo), con los métodos `DarDeBaja(fecha)` y `Reactivar()`.
- Filtro global con nombre en EF Core: `HasQueryFilter("ClientesActivos", c => c.FechaBaja == null)`. Las consultas que necesitan ver bajas lo ignoran explícitamente con `IgnoreQueryFilters([FiltroActivos])`.
- Endpoints `DELETE /api/clientes/{id}` (baja) y `POST /api/clientes/{id}/reactivar`.
- Reglas:
  - No se da de baja un cliente con solicitudes pendientes (409). El chequeo se hace antes de modificar la entidad, para no dejar cambios trackeados que no se van a guardar.
  - Dar de alta un DNI que pertenece a un cliente dado de baja devuelve 409 e indica reactivarlo.
  - Una solicitud para un cliente dado de baja es un conflicto (409), distinto de un cliente inexistente (400).
- Las consultas de solicitudes ignoran el filtro de clientes. Como la navegación es obligatoria, EF genera un `INNER JOIN` contra los clientes activos, y la solicitud de un cliente dado de baja desaparecería. La advertencia de EF 10622, que avisa esta situación, se silencia con un comentario que lo explica.

## Alternativas consideradas

- **Borrado físico:** pierde el historial.
- **Columna booleana `Activo`:** no registra cuándo ocurrió la baja.
- **Filtrar a mano en cada consulta:** es fácil olvidarse de un `Where` y mostrar clientes dados de baja.

## Consecuencias

- El DNI sigue siendo único en toda la tabla, incluyendo bajas.
- **Riesgo aceptado:** si una baja y el alta de una solicitud del mismo cliente ocurren al mismo tiempo, podría quedar un cliente dado de baja con una solicitud pendiente. Las operaciones tocan filas distintas, así que ni `RowVersion` ni los índices lo evitan. Se acepta por el volumen esperado; la solución sería una transacción con bloqueo.
