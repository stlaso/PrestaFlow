using System.Linq.Expressions;
using FluentValidation;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Application.Validators;

public sealed class CrearClienteRequestValidator : AbstractValidator<CrearClienteRequest>
{
    public const int EdadMaximaRazonable = 120;

    public CrearClienteRequestValidator(TimeProvider timeProvider)
    {
        LargoMaximo(r => r.Nombre, Cliente.LargoMaximoTexto);
        LargoMaximo(r => r.Apellido, Cliente.LargoMaximoTexto);
        LargoMaximo(r => r.Provincia, Cliente.LargoMaximoTexto);
        LargoMaximo(r => r.Localidad, Cliente.LargoMaximoTexto);
        LargoMaximo(r => r.Ocupacion, Cliente.LargoMaximoTexto);
        LargoMaximo(r => r.Email, Cliente.LargoMaximoEmail);
        LargoMaximo(r => r.Telefono, Cliente.LargoMaximoTelefono);

        RuleFor(r => r.FechaNacimiento)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("La fecha de nacimiento es obligatoria.")
            .LessThanOrEqualTo(_ => Hoy(timeProvider))
                .WithMessage("La fecha de nacimiento no puede ser futura.")
            .GreaterThan(_ => Hoy(timeProvider).AddYears(-EdadMaximaRazonable))
                .WithMessage($"La fecha de nacimiento no puede ser de hace más de {EdadMaximaRazonable} años.");
    }

    private void LargoMaximo(Expression<Func<CrearClienteRequest, string>> campo, int largo) =>
        RuleFor(campo)
            .MaximumLength(largo)
            .WithMessage("No puede superar los {MaxLength} caracteres (tiene {TotalLength}).");

    private static DateOnly Hoy(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
