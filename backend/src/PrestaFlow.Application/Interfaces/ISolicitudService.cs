using PrestaFlow.Application.Dtos;

namespace PrestaFlow.Application.Interfaces;

public interface ISolicitudService
{
    Task<SolicitudResponse> CrearAsync(CrearSolicitudRequest request, CancellationToken cancellationToken = default);

    Task<SolicitudResponse?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PaginaResponse<SolicitudResponse>> ListarAsync(ListarSolicitudesRequest request, CancellationToken cancellationToken = default);

    Task<SolicitudResponse?> AprobarAsync(int id, CancellationToken cancellationToken = default);

    Task<SolicitudResponse?> RechazarAsync(int id, RechazarSolicitudRequest request, CancellationToken cancellationToken = default);
}
