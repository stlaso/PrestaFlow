using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Infrastructure.Persistence.Configurations;

public class SolicitudConfiguration : IEntityTypeConfiguration<Solicitud>
{
    public void Configure(EntityTypeBuilder<Solicitud> builder)
    {
        builder.Property(s => s.Monto).HasPrecision(18, 2);
        builder.Property(s => s.IngresoMensual).HasPrecision(18, 2);
        builder.Property(s => s.MotivoRechazo).HasMaxLength(500);

        builder.Ignore(s => s.Cuota);

        // Concurrencia optimista: si otro request la modificó entre la lectura y el guardado, falla el UPDATE.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(s => s.Cliente)
            .WithMany()
            .HasForeignKey(s => s.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
