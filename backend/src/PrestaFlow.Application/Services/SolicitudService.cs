using FluentValidation;
using FluentValidation.Results;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.Application.Services;

public class SolicitudService(
    ISolicitudRepository solicitudes,
    IClienteRepository clientes,
    IUnitOfWork unitOfWork,
    IValidator<CrearSolicitudRequest> validadorAlta,
    IValidator<ListarSolicitudesRequest> validadorListado,
    IValidator<RechazarSolicitudRequest> validadorRechazo,
    RelojNegocio reloj) : ISolicitudService
{
    public async Task<SolicitudResponse> CrearAsync(CrearSolicitudRequest request, CancellationToken cancellationToken = default)
    {
        await validadorAlta.ValidateAndThrowAsync(request, cancellationToken);

        // Incluye bajas: si está dado de baja, el constructor de Solicitud responde 409 en lugar de "no existe".
        var cliente = await clientes.ObtenerPorIdIncluyendoBajasAsync(request.ClienteId, cancellationToken)
            ?? throw new ValidationException(
                [new ValidationFailure(nameof(request.ClienteId), $"No existe un cliente con el Id {request.ClienteId}.")]);

        if (await solicitudes.ExistenPendientesAsync(cliente.Id, cancellationToken))
            throw new ConflictoException("El cliente ya tiene una solicitud pendiente.");

        var ahora = reloj.AhoraUtc();

        var solicitud = new Solicitud(
            cliente: cliente,
            monto: request.Monto,
            cantidadCuotas: request.CantidadCuotas,
            ingresoMensual: request.IngresoMensual,
            fechaPrimerPago: request.FechaPrimerPago,
            fechaCreacion: ahora,
            diaCreacion: reloj.DiaLocal(ahora));

        solicitudes.Agregar(solicitud);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return SolicitudResponse.Desde(solicitud);
    }

    public async Task<SolicitudResponse?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var solicitud = await solicitudes.ObtenerPorIdAsync(id, cancellationToken);

        return solicitud is null ? null : SolicitudResponse.Desde(solicitud);
    }

    public async Task<PaginaResponse<SolicitudResponse>> ListarAsync(ListarSolicitudesRequest request, CancellationToken cancellationToken = default)
    {
        await validadorListado.ValidateAndThrowAsync(request, cancellationToken);

        var (items, total) = await solicitudes.ListarAsync(
            request.Estado,
            request.Riesgo,
            request.ClienteId,
            request.Pagina,
            request.TamanioPagina,
            cancellationToken);

        return new PaginaResponse<SolicitudResponse>(
            items.Select(SolicitudResponse.Desde).ToList(),
            request.Pagina,
            request.TamanioPagina,
            total);
    }

    public async Task<SolicitudResponse?> AprobarAsync(int id, CancellationToken cancellationToken = default)
    {
        var solicitud = await solicitudes.ObtenerPorIdAsync(id, cancellationToken);

        if (solicitud is null)
            return null;

        solicitud.Aprobar(reloj.AhoraUtc());
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return SolicitudResponse.Desde(solicitud);
    }

    public async Task<SolicitudResponse?> RechazarAsync(int id, RechazarSolicitudRequest request, CancellationToken cancellationToken = default)
    {
        await validadorRechazo.ValidateAndThrowAsync(request, cancellationToken);

        var solicitud = await solicitudes.ObtenerPorIdAsync(id, cancellationToken);

        if (solicitud is null)
            return null;

        solicitud.Rechazar(request.Motivo, reloj.AhoraUtc());
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return SolicitudResponse.Desde(solicitud);
    }
}
