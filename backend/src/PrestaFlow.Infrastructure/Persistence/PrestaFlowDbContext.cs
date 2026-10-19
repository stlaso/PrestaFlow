using Microsoft.EntityFrameworkCore;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Infrastructure.Persistence;

public class PrestaFlowDbContext(DbContextOptions<PrestaFlowDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrestaFlowDbContext).Assembly);
    }
}
