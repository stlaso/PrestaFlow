using Microsoft.EntityFrameworkCore;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;
using PrestaFlow.Infrastructure.Persistence;
using PrestaFlow.Infrastructure.Persistence.Configurations;

namespace PrestaFlow.Infrastructure.Repositories;

public class SolicitudRepository(PrestaFlowDbContext db) : ISolicitudRepository
{
    // Sin ignorar el filtro, el INNER JOIN contra clientes activos haría desaparecer las solicitudes de clientes dados de baja.
    public Task<Solicitud?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Solicitudes
            .IgnoreQueryFilters([ClienteConfiguration.FiltroActivos])
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Solicitud> Items, int Total)> ListarAsync(
        EstadoSolicitud? estado,
        NivelRiesgo? riesgo,
        int? clienteId,
        int pagina,
        int tamanioPagina,
        CancellationToken cancellationToken = default)
    {
        var consulta = db.Solicitudes.IgnoreQueryFilters([ClienteConfiguration.FiltroActivos]);

        if (estado is not null)
            consulta = consulta.Where(s => s.Estado == estado.Value);

        if (riesgo is not null)
            consulta = consulta.Where(s => s.Riesgo == riesgo.Value);

        if (clienteId is not null)
            consulta = consulta.Where(s => s.ClienteId == clienteId.Value);

        // Antes del Include, para que el COUNT no haga el join con Clientes.
        var total = await consulta.CountAsync(cancellationToken);

        // Orden determinístico: sin el Id como desempate, OFFSET puede repetir o saltear filas entre páginas.
        var items = await consulta
            .Include(s => s.Cliente)
            .AsNoTracking()
            .OrderByDescending(s => s.FechaCreacion)
            .ThenByDescending(s => s.Id)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> ExistenPendientesAsync(int clienteId, CancellationToken cancellationToken = default) =>
        db.Solicitudes.AnyAsync(s => s.ClienteId == clienteId && s.Estado == EstadoSolicitud.Pendiente, cancellationToken);

    public void Agregar(Solicitud solicitud) => db.Solicitudes.Add(solicitud);
}
