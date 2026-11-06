# ADR-0012: Estrategia de testing en dos niveles contra SQL Server real

- **Estado:** Aceptada
- **Fecha:** 2026-11

## Contexto

Parte de las reglas vive en el código (riesgo, transiciones de estado, casos de uso) y parte solo existe en la base o en la configuración de la Api: índices únicos, `RowVersion`, filtro global de bajas, traducción a Problem Details y formato del JSON. Un solo tipo de test no cubre las dos cosas: los mocks devuelven lo que se les programa y no pueden detectar un error de una consulta o de un índice.

## Decisión

Dos proyectos, según lo que cada test necesita para fallar:

| Proyecto | Prueba | Dependencias | Herramientas |
|---|---|---|---|
| `PrestaFlow.UnitTests` | Dominio, `RelojNegocio`, services y mapeo a DTOs | Domain y Application, sin base | xUnit v3, Moq, `FakeTimeProvider` |
| `PrestaFlow.IntegrationTests` | Endpoints, consultas, migraciones, índices y JSON | Api completa y SQL Server real | Testcontainers, `WebApplicationFactory`, Respawn |

- **Moq solo para las dependencias de I/O:** en los unitarios se simulan los repositorios e `IUnitOfWork`. Las entidades y `RelojNegocio` son reales, y el tiempo se controla con `FakeTimeProvider`, que es una implementación falsa y no un mock.
- **Base real y descartable:** Testcontainers levanta un contenedor de SQL Server por corrida y se aplican las migraciones reales.
- **Limpieza con Respawn:** los tests de integración comparten el contenedor, corren en serie (`ICollectionFixture`) y cada uno arranca con las tablas vacías. Es más rápido que recrear la base.
- **Entorno `Testing`:** con `Development` se cargarían los User Secrets, donde está la base local. Además, `PrestaFlowApiFactory` frena antes de migrar si la Api no apunta al contenedor.
- **Tiempo controlado:** `FakeTimeProvider` reemplaza a `TimeProvider`, tanto en unitarios como en la Api. Permite probar el cambio de día en Argentina y empates exactos de fecha.
- **Contrato con JSON crudo:** cada recurso tiene un test que lee la respuesta con `JsonDocument`. Los demás deserializan a los DTOs de C# y no detectan cambios en nombres, formatos o enums que romperían al frontend.
- **Carreras con dos scopes de DI:** cada scope equivale a un request con su propio `DbContext`. Los dos leen antes de que el otro guarde y el test fija el orden, así que el resultado es determinista. Se llega hasta `ConflictoException`; que esa excepción da 409 lo cubren los tests de endpoints.
- **Mutaciones manuales:** al cerrar cada grupo de tests se introdujeron bugs a propósito para confirmar que algún test falla. Es una verificación puntual: no queda automatizada.

## Alternativas consideradas

- **EF Core InMemory:** no es una base relacional: no aplica índices únicos filtrados ni traduce a SQL, justo lo que hay que probar.
- **SQLite en memoria:** es relacional, pero no es el motor de producción: no tiene `rowversion` ni los mismos errores 2601.
- **LocalDB o la base de desarrollo:** LocalDB solo funciona en Windows y una base compartida acumula estado entre corridas. Además, Respawn podría borrar datos de desarrollo.
- **Recrear la base en cada test o usar transacciones con rollback:** recrearla es lenta; el rollback no sirve porque la Api guarda con su propio `DbContext` y su propia conexión.
- **Muchos `POST` en paralelo para las carreras:** no es determinista. En una prueba manual, ningún request llegó a pasar el chequeo del service: el test daría verde sin ejercitar el respaldo de la base.
- **Mockear también el dominio o el reloj:** el test verificaría las respuestas programadas en el mock y no la lógica real, y se rompería ante cualquier refactor interno.
- **Stryker (mutation testing automatizado):** da una medida continua, pero suma tiempo de ejecución y otra herramienta a mantener. Se puede evaluar más adelante.

## Consecuencias

- Los tests de integración requieren Docker, también en el pipeline de la Fase 5. Si Docker está apagado, fallan al iniciar el contenedor.
- La suite es más lenta que una solo de unitarios, porque descarga la imagen la primera vez y levanta el contenedor en cada corrida.
- Sin un `ThenBy` de desempate, SQL Server no garantiza el orden de los empates: el test de orden es confiable con el código correcto, pero podría no detectar siempre la falta del desempate.
- La rama del error 2627 de `GuardarCambiosAsync` no tiene test: se traduce por prevención, y hoy el modelo solo tiene índices únicos (2601).
- La carrera entre una baja de cliente y el alta de una solicitud sigue sin test: es el riesgo aceptado en el ADR-0007, porque esas operaciones tocan filas distintas y ni `RowVersion` ni los índices la frenan.
