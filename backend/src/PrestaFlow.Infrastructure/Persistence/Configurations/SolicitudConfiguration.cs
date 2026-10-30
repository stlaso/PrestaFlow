using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;

namespace PrestaFlow.Infrastructure.Persistence.Configurations;

public class SolicitudConfiguration : IEntityTypeConfiguration<Solicitud>
{
    public void Configure(EntityTypeBuilder<Solicitud> builder)
    {
        builder.Property(s => s.Monto).HasPrecision(18, 2);
        builder.Property(s => s.IngresoMensual).HasPrecision(18, 2);
        builder.Property(s => s.MotivoRechazo).HasMaxLength(Solicitud.LargoMaximoMotivo);

        builder.Ignore(s => s.Cuota);

        // Concurrencia optimista: si otro request la modificó entre la lectura y el guardado, falla el UPDATE.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(s => s.Cliente)
            .WithMany()
            .HasForeignKey(s => s.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // El índice de la FK se declara explícito para que el filtrado de abajo no lo reemplace.
        builder.HasIndex(s => s.ClienteId);

        // Respaldo en la base de la regla "una solicitud pendiente por cliente" ante requests simultáneos.
        builder.HasIndex(s => s.ClienteId, "UX_Solicitudes_ClienteId_Pendiente")
            .IsUnique()
            .HasFilter($"[Estado] = {(int)EstadoSolicitud.Pendiente}");
    }
}
