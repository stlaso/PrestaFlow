using Microsoft.EntityFrameworkCore;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Infrastructure.Persistence;
using PrestaFlow.Infrastructure.Persistence.Configurations;

namespace PrestaFlow.Infrastructure.Repositories;

public class ClienteRepository(PrestaFlowDbContext db) : IClienteRepository
{
    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Cliente?> ObtenerPorDniIncluyendoBajasAsync(string dni, CancellationToken cancellationToken = default) =>
        db.Clientes
            .IgnoreQueryFilters([ClienteConfiguration.FiltroActivos])
            .FirstOrDefaultAsync(c => c.Dni == dni, cancellationToken);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(bool incluirBajas, CancellationToken cancellationToken = default)
    {
        var consulta = db.Clientes.AsNoTracking();

        if (incluirBajas)
            consulta = consulta.IgnoreQueryFilters([ClienteConfiguration.FiltroActivos]);

        return await consulta
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync(cancellationToken);
    }

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
}
