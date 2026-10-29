# ADR-0001: Arquitectura en capas con inversión de dependencias

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

La API tiene reglas de negocio propias (riesgo, edades, estados de una solicitud) que tienen que poder testearse sin base de datos y sobrevivir a cambios de tecnología de persistencia o de presentación.

## Decisión

Cuatro proyectos con dependencias en una sola dirección:

| Capa | Contiene | Depende de |
|---|---|---|
| Domain | Entidades, enums, reglas de negocio, excepciones de negocio | Nada |
| Application | Services (casos de uso), DTOs, validadores, interfaces de repositorios y de Unit of Work | Domain |
| Infrastructure | DbContext, configuraciones de EF Core, migraciones, repositorios | Application, Domain |
| Api | Controllers, manejo de errores, configuración | Application, Infrastructure |

- Las interfaces de los repositorios se definen en **Application**, que es quien las necesita, y se implementan en Infrastructure (inversión de dependencias).
- La Api referencia Infrastructure solo como *composition root*: `Program.cs` registra las implementaciones con `AddInfrastructure()`. Ningún controller usa tipos de Infrastructure.
- Los controllers no tienen lógica: llaman a un service y devuelven DTOs, nunca entidades.
- Se usan controllers y no Minimal APIs, por la organización por recurso y los atributos de documentación.

## Alternativas consideradas

- **Interfaces de repositorio en Infrastructure:** Application tendría que referenciar Infrastructure y quedaría atada a EF Core. Además, Infrastructure ya referencia Application, así que habría una referencia circular.
- **Un solo proyecto con carpetas:** más simple al principio, pero nada impide que un controller use el DbContext o que una entidad dependa de EF. Las reglas de dependencia se cumplirían por disciplina y no por el compilador.
- **Clean Architecture con MediatR / CQRS:** agrega indirección (handlers, pipelines) que el tamaño del dominio no justifica.

## Consecuencias

- El Domain se testea con `new Solicitud(...)`, sin mocks ni base.
- Cambiar la persistencia (por ejemplo, de EF Core a Dapper) no toca Application ni Domain.
- Hay más proyectos y más archivos (interfaz + implementación) que en un diseño en un solo proyecto.
