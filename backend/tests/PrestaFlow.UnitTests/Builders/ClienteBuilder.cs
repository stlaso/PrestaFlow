using PrestaFlow.Domain.Entities;

namespace PrestaFlow.UnitTests.Builders;

public class ClienteBuilder
{
    private string _dni = "30123456";
    private DateOnly _fechaNacimiento = new(1990, 5, 15);
    private DateTime? _fechaBaja;

    public ClienteBuilder ConDni(string dni)
    {
        _dni = dni;
        return this;
    }

    public ClienteBuilder ConFechaNacimiento(DateOnly fechaNacimiento)
    {
        _fechaNacimiento = fechaNacimiento;
        return this;
    }

    public ClienteBuilder DadoDeBaja(DateTime? fecha = null)
    {
        _fechaBaja = fecha ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        return this;
    }

    public Cliente Build()
    {
        var cliente = new Cliente(
            nombre: "Ana",
            apellido: "Pérez",
            dni: _dni,
            provincia: "Buenos Aires",
            localidad: "La Plata",
            telefono: "2214567890",
            email: "ana.perez@example.com",
            fechaNacimiento: _fechaNacimiento,
            ocupacion: "Docente");

        if (_fechaBaja is not null)
            cliente.DarDeBaja(_fechaBaja.Value);

        return cliente;
    }
}
