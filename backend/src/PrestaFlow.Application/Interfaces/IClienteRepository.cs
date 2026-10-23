using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Application.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    // Incluye dados de baja: el índice único de DNI abarca todas las filas.
    Task<Cliente?> ObtenerPorDniIncluyendoBajasAsync(string dni, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cliente>> ListarAsync(bool incluirBajas, CancellationToken cancellationToken = default);

    void Agregar(Cliente cliente);
}
