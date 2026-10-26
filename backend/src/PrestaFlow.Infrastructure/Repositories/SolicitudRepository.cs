using Microsoft.EntityFrameworkCore;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Enums;
using PrestaFlow.Infrastructure.Persistence;

namespace PrestaFlow.Infrastructure.Repositories;

public class SolicitudRepository(PrestaFlowDbContext db) : ISolicitudRepository
{
    public Task<bool> ExistenPendientesAsync(int clienteId, CancellationToken cancellationToken = default) =>
        db.Solicitudes.AnyAsync(s => s.ClienteId == clienteId && s.Estado == EstadoSolicitud.Pendiente, cancellationToken);
}
