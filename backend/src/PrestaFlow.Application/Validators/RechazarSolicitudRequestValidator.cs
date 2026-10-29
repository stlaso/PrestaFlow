using FluentValidation;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Application.Validators;

// Que el motivo no esté vacío lo exige Solicitud.Rechazar.
public sealed class RechazarSolicitudRequestValidator : AbstractValidator<RechazarSolicitudRequest>
{
    public RechazarSolicitudRequestValidator()
    {
        RuleFor(r => r.Motivo)
            .MaximumLength(Solicitud.LargoMaximoMotivo)
                .WithMessage("No puede superar los {MaxLength} caracteres (tiene {TotalLength}).");
    }
}
