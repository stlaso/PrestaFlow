using PrestaFlow.Application.Dtos;

namespace PrestaFlow.Application.Interfaces;

public interface IClienteService
{
    Task<ClienteResponse> CrearAsync(CrearClienteRequest request, CancellationToken cancellationToken = default);

    Task<ClienteResponse?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool incluirBajas, CancellationToken cancellationToken = default);

    Task<bool> DarDeBajaAsync(int id, CancellationToken cancellationToken = default);

    Task<ClienteResponse?> ReactivarAsync(int id, CancellationToken cancellationToken = default);
}
