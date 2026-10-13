using PrestaFlow.Domain.Enums;
using PrestaFlow.Domain.Exceptions;
using PrestaFlow.Domain.Services;

namespace PrestaFlow.Domain.Entities;

public class Solicitud
{
    private const int CuotasMinimas = 1;
    private const int CuotasMaximas = 60;
    private const int EdadMinima = 18;

    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public int CantidadCuotas { get; private set; }
    public decimal IngresoMensual { get; private set; }
    public NivelRiesgo Riesgo { get; private set; }
    public EstadoSolicitud Estado { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public DateOnly FechaPrimerPago { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaResolucion { get; private set; }

    public decimal Cuota => Math.Round(Monto / CantidadCuotas, 2, MidpointRounding.AwayFromZero);

    public Solicitud(
        Cliente cliente,
        decimal monto,
        int cantidadCuotas,
        decimal ingresoMensual,
        DateOnly fechaPrimerPago,
        DateTime fechaCreacion)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        if (monto <= 0)
            throw new DomainException("El monto debe ser mayor a cero.");

        if (cantidadCuotas is < CuotasMinimas or > CuotasMaximas)
            throw new DomainException($"La cantidad de cuotas debe estar entre {CuotasMinimas} y {CuotasMaximas}.");

        var diaCreacion = DateOnly.FromDateTime(fechaCreacion);

        if (fechaPrimerPago <= diaCreacion)
            throw new DomainException("La fecha del primer pago debe ser posterior a la fecha de creación.");

        if (cliente.FechaNacimiento.AddYears(EdadMinima) > diaCreacion)
            throw new DomainException($"El cliente debe ser mayor de {EdadMinima} años.");

        Cliente = cliente;
        ClienteId = cliente.Id;
        Monto = monto;
        CantidadCuotas = cantidadCuotas;
        IngresoMensual = ingresoMensual;
        FechaPrimerPago = fechaPrimerPago;
        FechaCreacion = fechaCreacion;
        Riesgo = CalculadoraRiesgo.Calcular(Cuota, ingresoMensual);
        Estado = EstadoSolicitud.Pendiente;
    }

    private Solicitud() { }

    public void Aprobar(DateTime fecha)
    {
        ValidarPendiente();
        Estado = EstadoSolicitud.Aprobada;
        FechaResolucion = fecha;
    }

    public void Rechazar(string motivo, DateTime fecha)
    {
        ValidarPendiente();

        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("Rechazar una solicitud exige un motivo.");

        Estado = EstadoSolicitud.Rechazada;
        MotivoRechazo = motivo.Trim();
        FechaResolucion = fecha;
    }

    private void ValidarPendiente()
    {
        if (Estado != EstadoSolicitud.Pendiente)
            throw new DomainException($"Solo se puede resolver una solicitud pendiente. Estado actual: {Estado}.");
    }
}
