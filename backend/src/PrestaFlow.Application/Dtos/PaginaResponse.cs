namespace PrestaFlow.Application.Dtos;

public sealed record PaginaResponse<T>(IReadOnlyList<T> Items, int Pagina, int TamanioPagina, int Total);
