# ADR-0010: Contrato HTTP: acciones de negocio, DTOs y códigos de estado

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

El contrato de la API lo va a consumir un frontend en React + TypeScript. Tiene que ser predecible, no exponer datos innecesarios y permitir solo operaciones válidas.

## Decisión

**Endpoints de la Fase 1:**

| Endpoint | Respuestas |
|---|---|
| `POST /api/clientes` | 201 + `Location`, 400, 409 |
| `GET /api/clientes/{id}` | 200, 404 |
| `GET /api/clientes?incluirBajas=` | 200 |
| `DELETE /api/clientes/{id}` | 204, 404, 409 |
| `POST /api/clientes/{id}/reactivar` | 200, 404, 409 |
| `POST /api/solicitudes` | 201 + `Location`, 400, 409 |
| `GET /api/solicitudes/{id}` | 200, 404 |
| `GET /api/solicitudes?estado=&riesgo=&clienteId=&pagina=&tamanioPagina=` | 200, 400 |
| `POST /api/solicitudes/{id}/aprobar` | 200, 404, 409 |
| `POST /api/solicitudes/{id}/rechazar` | 200, 400, 404, 409 |

**Criterio de códigos:**
- **400:** el request está mal armado o viola una regla, incluido un `ClienteId` del body que no existe.
- **404:** solo para el recurso identificado en la URL.
- **409:** el request es válido pero choca con el estado actual (DNI duplicado, cliente dado de baja, solicitud ya resuelta, conflicto de concurrencia).

**DTOs:**
- Los requests no incluyen lo que calcula el sistema (riesgo, estado, fechas de creación).
- `SolicitudResponse` incluye un `ClienteResumen` anidado (Id, nombre, apellido, DNI) en lugar del cliente completo.
- Los enums se serializan como texto con `JsonStringEnumConverter` (`"riesgo": "Bajo"`).
- Los DTOs son `record`. El de filtros del listado usa propiedades `init` con valores por defecto, porque el binder de MVC arma el objeto desde el query string propiedad por propiedad.

**Acciones de negocio como recursos:** aprobar, rechazar y reactivar son `POST /{id}/accion`.

## Alternativas consideradas

- **`PATCH` con `{ "estado": "Aprobada" }`:** el contrato permitiría pedir transiciones inválidas, como volver a Pendiente una rechazada, y una aprobación no es la edición de un campo: registra la fecha y valida el estado.
- **404 para un `ClienteId` inexistente en el body:** confundiría "no existe la URL" con "un dato del formulario es inválido".
- **Exponer las entidades directamente:** acopla el contrato al modelo de datos y expone campos internos (por ejemplo, `RowVersion`).
- **Enums como números:** el contrato perdería legibilidad y el OpenAPI no documentaría los valores posibles por nombre.

## Consecuencias

- `POST` no es idempotente: un doble envío se protege con las restricciones de ADR-0009. `DELETE` sí lo es: repetir la baja no cambia el estado del servidor.
- El frontend puede generar tipos como `"Bajo" | "Medio" | "Alto"` a partir del OpenAPI.
- Los datos personales del cliente (email, teléfono, domicilio) no viajan en los listados de solicitudes.
