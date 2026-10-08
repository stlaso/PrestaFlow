using System.Net.Mail;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.Domain.Entities;

public class Cliente
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string Dni { get; private set; } = string.Empty;
    public string Provincia { get; private set; } = string.Empty;
    public string Localidad { get; private set; } = string.Empty;
    public string Telefono { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateOnly FechaNacimiento { get; private set; }
    public string Ocupacion { get; private set; } = string.Empty;

    public Cliente(
        string nombre,
        string apellido,
        string dni,
        string provincia,
        string localidad,
        string telefono,
        string email,
        DateOnly fechaNacimiento,
        string ocupacion)
    {
        Nombre = Requerido(nombre, "nombre");
        Apellido = Requerido(apellido, "apellido");
        Dni = ValidarDni(dni);
        Provincia = Requerido(provincia, "provincia");
        Localidad = Requerido(localidad, "localidad");
        Ocupacion = Requerido(ocupacion, "ocupación");
        FechaNacimiento = fechaNacimiento;
        ActualizarContacto(telefono, email);
    }

    // Para EF Core.
    private Cliente() { }

    // El DNI no tiene método de cambio: es inmutable.
    public void ActualizarContacto(string telefono, string email)
    {
        Telefono = Requerido(telefono, "teléfono");
        Email = ValidarEmail(email);
    }

    private static string Requerido(string valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException($"El campo {campo} es obligatorio.");

        return valor.Trim();
    }

    private static string ValidarDni(string dni)
    {
        dni = Requerido(dni, "DNI");

        if (dni.Length is not (7 or 8) || !dni.All(char.IsDigit))
            throw new DomainException("El DNI debe tener 7 u 8 dígitos, sin puntos.");

        return dni;
    }

    private static string ValidarEmail(string email)
    {
        email = Requerido(email, "email");

        if (!MailAddress.TryCreate(email, out _))
            throw new DomainException("El email no tiene un formato válido.");

        return email.ToLowerInvariant();
    }
}
