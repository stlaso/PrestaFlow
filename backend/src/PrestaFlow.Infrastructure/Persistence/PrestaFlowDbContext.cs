using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.Infrastructure.Persistence;

public class PrestaFlowDbContext(DbContextOptions<PrestaFlowDbContext> options) : DbContext(options), IUnitOfWork
{
    private const int ErrorIndiceUnico = 2601;
    private const int ErrorRestriccionUnica = 2627;

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictoException("El registro fue modificado por otro usuario. Volvé a cargarlo.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: ErrorIndiceUnico or ErrorRestriccionUnica })
        {
            throw new ConflictoException("Ya existe un registro con esos datos.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrestaFlowDbContext).Assembly);
    }
}
