using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.Api.Errores;

public sealed class ManejadorExcepciones(IProblemDetailsService problemDetailsService, ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validacion)
            return await EscribirValidacionAsync(httpContext, validacion);

        var (status, titulo, detalle) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Regla de negocio incumplida", exception.Message),
            ConflictoException => (StatusCodes.Status409Conflict, "Conflicto con el estado actual", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("{Status} en {Metodo} {Ruta}: {Mensaje}", status, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        return await EscribirAsync(httpContext, new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle
        });
    }

    private async ValueTask<bool> EscribirValidacionAsync(HttpContext httpContext, ValidationException validacion)
    {
        var errores = validacion.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        logger.LogInformation("400 en {Metodo} {Ruta}: datos inválidos en {Campos}", httpContext.Request.Method, httpContext.Request.Path, errores.Keys);

        return await EscribirAsync(httpContext, new ValidationProblemDetails(errores)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o más datos son inválidos."
        });
    }

    private ValueTask<bool> EscribirAsync(HttpContext httpContext, ProblemDetails problemDetails)
    {
        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}
