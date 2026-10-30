using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;

namespace PrestaFlow.Application.Dtos;

public sealed record SolicitudResponse(
    int Id,
    ClienteResumen Cliente,
    decimal Monto,
    int CantidadCuotas,
    decimal Cuota,
    decimal IngresoMensual,
    NivelRiesgo Riesgo,
    EstadoSolicitud Estado,
    string? MotivoRechazo,
    DateOnly FechaPrimerPago,
    DateTime FechaCreacion,
    DateTime? FechaResolucion)
{
    // Requiere la navegación Cliente cargada.
    public static SolicitudResponse Desde(Solicitud solicitud) => new(
        Id: solicitud.Id,
        Cliente: ClienteResumen.Desde(solicitud.Cliente),
        Monto: solicitud.Monto,
        CantidadCuotas: solicitud.CantidadCuotas,
        Cuota: solicitud.Cuota,
        IngresoMensual: solicitud.IngresoMensual,
        Riesgo: solicitud.Riesgo,
        Estado: solicitud.Estado,
        MotivoRechazo: solicitud.MotivoRechazo,
        FechaPrimerPago: solicitud.FechaPrimerPago,
        FechaCreacion: solicitud.FechaCreacion,
        FechaResolucion: solicitud.FechaResolucion);
}
