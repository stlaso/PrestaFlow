using System.Linq.Expressions;
using FluentValidation;
using PrestaFlow.Application.Dtos;

namespace PrestaFlow.Application.Validators;

// Solo forma del dato; las reglas de negocio (monto > 0, cuotas, fechas, edades) las valida Solicitud.
public sealed class CrearSolicitudRequestValidator : AbstractValidator<CrearSolicitudRequest>
{
    private const int Precision = 18;
    private const int Decimales = 2;

    public CrearSolicitudRequestValidator()
    {
        RuleFor(r => r.ClienteId)
            .GreaterThan(0)
                .WithMessage("El cliente es obligatorio.");

        RuleFor(r => r.FechaPrimerPago)
            .NotEmpty()
                .WithMessage("La fecha del primer pago es obligatoria.");

        Importe(r => r.Monto);
        Importe(r => r.IngresoMensual);
    }

    private void Importe(Expression<Func<CrearSolicitudRequest, decimal>> campo) =>
        RuleFor(campo)
            .PrecisionScale(Precision, Decimales, ignoreTrailingZeros: true)
                .WithMessage($"Admite hasta {Decimales} decimales y {Precision - Decimales} dígitos enteros.");
}
