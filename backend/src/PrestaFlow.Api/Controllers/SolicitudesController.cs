using Microsoft.AspNetCore.Mvc;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Interfaces;

namespace PrestaFlow.Api.Controllers;

[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController(ISolicitudService solicitudService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SolicitudResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolicitudResponse>> Crear(CrearSolicitudRequest request, CancellationToken cancellationToken)
    {
        var solicitud = await solicitudService.CrearAsync(request, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = solicitud.Id }, solicitud);
    }

    [HttpGet]
    [ProducesResponseType<PaginaResponse<SolicitudResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaResponse<SolicitudResponse>>> Listar(
        [FromQuery] ListarSolicitudesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await solicitudService.ListarAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<SolicitudResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SolicitudResponse>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var solicitud = await solicitudService.ObtenerPorIdAsync(id, cancellationToken);

        return solicitud is null ? NotFound() : Ok(solicitud);
    }

    [HttpPost("{id:int}/aprobar")]
    [ProducesResponseType<SolicitudResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolicitudResponse>> Aprobar(int id, CancellationToken cancellationToken)
    {
        var solicitud = await solicitudService.AprobarAsync(id, cancellationToken);

        return solicitud is null ? NotFound() : Ok(solicitud);
    }

    [HttpPost("{id:int}/rechazar")]
    [ProducesResponseType<SolicitudResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolicitudResponse>> Rechazar(int id, RechazarSolicitudRequest request, CancellationToken cancellationToken)
    {
        var solicitud = await solicitudService.RechazarAsync(id, request, cancellationToken);

        return solicitud is null ? NotFound() : Ok(solicitud);
    }
}
