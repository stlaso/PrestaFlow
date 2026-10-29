using FluentValidation;
using PrestaFlow.Application.Dtos;

namespace PrestaFlow.Application.Validators;

public sealed class ListarSolicitudesRequestValidator : AbstractValidator<ListarSolicitudesRequest>
{
    public ListarSolicitudesRequestValidator()
    {
        RuleFor(r => r.Pagina)
            .GreaterThanOrEqualTo(1)
                .WithMessage("La página debe ser 1 o mayor.");

        RuleFor(r => r.TamanioPagina)
            .InclusiveBetween(1, ListarSolicitudesRequest.TamanioPaginaMaximo)
                .WithMessage("El tamaño de página debe estar entre {From} y {To}.");

        RuleFor(r => r.ClienteId)
            .GreaterThan(0)
                .When(r => r.ClienteId is not null)
                .WithMessage("El cliente debe ser un Id válido.");
    }
}
