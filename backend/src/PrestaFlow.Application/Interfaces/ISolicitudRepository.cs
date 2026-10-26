namespace PrestaFlow.Application.Interfaces;

public interface ISolicitudRepository
{
    Task<bool> ExistenPendientesAsync(int clienteId, CancellationToken cancellationToken = default);
}
