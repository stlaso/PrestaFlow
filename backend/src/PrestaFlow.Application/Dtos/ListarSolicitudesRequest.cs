using PrestaFlow.Domain.Enums;

namespace PrestaFlow.Application.Dtos;

// Los filtros en null no filtran.
public sealed record ListarSolicitudesRequest
{
    public const int TamanioPaginaPorDefecto = 20;
    public const int TamanioPaginaMaximo = 100;

    public EstadoSolicitud? Estado { get; init; }
    public NivelRiesgo? Riesgo { get; init; }
    public int? ClienteId { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanioPagina { get; init; } = TamanioPaginaPorDefecto;
}
