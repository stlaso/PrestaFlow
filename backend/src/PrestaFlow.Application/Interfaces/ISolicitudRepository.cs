using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;

namespace PrestaFlow.Application.Interfaces;

public interface ISolicitudRepository
{
    // Trae la navegación Cliente aunque esté dado de baja; queda trackeada para poder resolverla.
    Task<Solicitud?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    // Solo lectura, de la más nueva a la más vieja; Total cuenta todas las que cumplen los filtros.
    Task<(IReadOnlyList<Solicitud> Items, int Total)> ListarAsync(
        EstadoSolicitud? estado,
        NivelRiesgo? riesgo,
        int? clienteId,
        int pagina,
        int tamanioPagina,
        CancellationToken cancellationToken = default);

    Task<bool> ExistenPendientesAsync(int clienteId, CancellationToken cancellationToken = default);
    void Agregar(Solicitud solicitud);
}
