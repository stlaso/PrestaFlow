using Microsoft.EntityFrameworkCore;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Infrastructure.Persistence;

public class PrestaFlowDbContext(DbContextOptions<PrestaFlowDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrestaFlowDbContext).Assembly);
    }
}
