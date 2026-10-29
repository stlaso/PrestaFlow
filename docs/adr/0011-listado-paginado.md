# ADR-0011: Listado con filtros opcionales y paginación por offset

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

El backoffice necesita listar solicitudes filtrando por estado, riesgo y cliente, y navegar por páginas sabiendo cuántos resultados hay en total.

## Decisión

- `GET /api/solicitudes` con filtros opcionales y respuesta `PaginaResponse<T>`: `{ items, pagina, tamanioPagina, total }`.
- 20 por página por defecto y 100 como máximo. Un tamaño fuera de rango o una página menor a 1 devuelve 400; no se recorta en silencio.
- Una página fuera de rango devuelve 200 con `items` vacío y el `total` real.
- La consulta se compone con `IQueryable`, agregando un `Where` solo para cada filtro presente:

```csharp
var consulta = db.Solicitudes.IgnoreQueryFilters([ClienteConfiguration.FiltroActivos]);
if (estado is not null) consulta = consulta.Where(s => s.Estado == estado.Value);
var total = await consulta.CountAsync(ct);
var items = await consulta.Include(s => s.Cliente).AsNoTracking()
    .OrderByDescending(s => s.FechaCreacion).ThenByDescending(s => s.Id)
    .Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina)
    .ToListAsync(ct);
```

- El `COUNT` se ejecuta antes del `Include`, así no hace el join con clientes.
- El orden es determinístico (`FechaCreacion` y luego `Id`). Sin el desempate, `OFFSET/FETCH` puede repetir u omitir filas con fechas iguales entre páginas.
- Las consultas de solo lectura usan `AsNoTracking`.
- EF pagina en una subconsulta y hace el join después, así la página cuenta solicitudes y no filas del join.

## Alternativas consideradas

- **`WHERE (@x IS NULL OR Columna = @x)`:** genera un solo plan para todas las combinaciones y es propenso a *parameter sniffing*. Con `IQueryable` compuesto, el filtro ausente no aparece en el SQL.
- **Paginación por keyset** ("las siguientes a la fecha X"): escala mejor con grandes volúmenes porque no recorre las filas salteadas, pero no permite saltar a una página ni calcular el total de páginas.
- **Proyección directa a DTO con `Select`:** traería solo las columnas usadas en lugar del cliente completo, pero obliga al repositorio a conocer los DTOs o a crear un modelo de lectura separado. Con páginas de 20 filas no se justifica.
- **`TotalPaginas` en la respuesta:** es derivable de `total` y `tamanioPagina`; mandarlo agrega un dato que podría quedar inconsistente.

## Consecuencias

- `PaginaResponse<T>` es reutilizable para otros listados.
- Las páginas lejanas son más lentas que las primeras (el `OFFSET` recorre lo salteado).
- Con volumen, el candidato a índice es `(Estado, FechaCreacion DESC)`; se agrega cuando una consulta medida lo justifique.
- El total y la página se obtienen en dos consultas: una solicitud creada entre ambas puede desfasar el total en uno.
