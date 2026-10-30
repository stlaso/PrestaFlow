namespace PrestaFlow.Application.Dtos;

public sealed record CrearSolicitudRequest(
    int ClienteId,
    decimal Monto,
    int CantidadCuotas,
    decimal IngresoMensual,
    DateOnly FechaPrimerPago);
