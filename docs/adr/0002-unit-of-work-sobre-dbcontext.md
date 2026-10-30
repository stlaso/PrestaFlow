# ADR-0002: Unit of Work implementado por el DbContext

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

Un caso de uso puede modificar varias entidades (por ejemplo, un cliente y una solicitud) y los cambios tienen que guardarse juntos o no guardarse.

## Decisión

- Los repositorios solo agregan o traen entidades; **no** llaman a `SaveChanges`.
- El service llama una sola vez a `IUnitOfWork.GuardarCambiosAsync()` al final del caso de uso: un `SaveChanges` es una transacción.
- `IUnitOfWork` se define en Application y lo implementa directamente `PrestaFlowDbContext`.
- Se registra reenviando a la instancia scoped del DbContext:

```csharp
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PrestaFlowDbContext>());
```

## Alternativas consideradas

- **`SaveChanges` dentro de cada repositorio:** dos repositorios en el mismo caso de uso harían dos transacciones; si falla la segunda, la primera ya quedó guardada.
- **`AddScoped<IUnitOfWork, PrestaFlowDbContext>()`:** el contenedor crearía una segunda instancia del DbContext. Los repositorios agregarían en una y el guardado se haría en la otra, sin cambios pendientes.
- **Exponer el DbContext a Application:** acoplaría los casos de uso a EF Core (ver ADR-0001).

## Consecuencias

- Las operaciones de un caso de uso son atómicas sin manejar transacciones a mano.
- `GuardarCambiosAsync` es el lugar natural para traducir errores de persistencia a excepciones de negocio (ver ADR-0005 y ADR-0009).
