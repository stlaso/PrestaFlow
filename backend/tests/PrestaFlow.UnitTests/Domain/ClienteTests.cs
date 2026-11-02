using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.UnitTests.Domain;

public class ClienteTests
{
    private static readonly DateTime FechaBaja = new(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);

    private static Cliente CrearCliente(
        string nombre = "Ana",
        string apellido = "Pérez",
        string dni = "30123456",
        string provincia = "Buenos Aires",
        string localidad = "La Plata",
        string telefono = "2214567890",
        string email = "ana.perez@example.com",
        string ocupacion = "Docente") =>
        new(
            nombre: nombre,
            apellido: apellido,
            dni: dni,
            provincia: provincia,
            localidad: localidad,
            telefono: telefono,
            email: email,
            fechaNacimiento: new DateOnly(1990, 5, 15),
            ocupacion: ocupacion);

    private static Cliente CrearConCampoVacio(string campo, string valor) => campo switch
    {
        "nombre" => CrearCliente(nombre: valor),
        "apellido" => CrearCliente(apellido: valor),
        "dni" => CrearCliente(dni: valor),
        "provincia" => CrearCliente(provincia: valor),
        "localidad" => CrearCliente(localidad: valor),
        "telefono" => CrearCliente(telefono: valor),
        "email" => CrearCliente(email: valor),
        "ocupacion" => CrearCliente(ocupacion: valor),
        _ => throw new ArgumentOutOfRangeException(nameof(campo), campo, null)
    };

    [Fact]
    public void Constructor_ConDatosValidos_QuedaActivo()
    {
        var cliente = CrearCliente();

        Assert.True(cliente.EstaActivo);
        Assert.Null(cliente.FechaBaja);
    }

    [Theory]
    [InlineData("nombre", "", "nombre")]
    [InlineData("nombre", "   ", "nombre")]
    [InlineData("apellido", "", "apellido")]
    [InlineData("dni", "", "DNI")]
    [InlineData("provincia", "", "provincia")]
    [InlineData("localidad", "", "localidad")]
    [InlineData("telefono", "", "teléfono")]
    [InlineData("email", "", "email")]
    [InlineData("ocupacion", "   ", "ocupación")]
    public void Constructor_ConCampoObligatorioVacio_LanzaDomainException(string campo, string valor, string nombreEnMensaje)
    {
        var ex = Assert.Throws<DomainException>(() => CrearConCampoVacio(campo, valor));

        Assert.Contains($"campo {nombreEnMensaje} es obligatorio", ex.Message);
    }

    [Fact]
    public void Constructor_RecortaEspaciosYPasaElEmailAMinusculas()
    {
        var cliente = CrearCliente(
            nombre: "  Ana ",
            apellido: " Pérez  ",
            dni: " 30123456 ",
            provincia: " Buenos Aires ",
            localidad: " La Plata ",
            telefono: " 2214567890 ",
            email: "  Ana.Perez@Example.COM ",
            ocupacion: " Docente ");

        Assert.Equal("Ana", cliente.Nombre);
        Assert.Equal("Pérez", cliente.Apellido);
        Assert.Equal("30123456", cliente.Dni);
        Assert.Equal("Buenos Aires", cliente.Provincia);
        Assert.Equal("La Plata", cliente.Localidad);
        Assert.Equal("2214567890", cliente.Telefono);
        Assert.Equal("ana.perez@example.com", cliente.Email);
        Assert.Equal("Docente", cliente.Ocupacion);
    }

    [Theory]
    [InlineData("3012345")]
    [InlineData("30123456")]
    public void Constructor_ConDniDe7u8Digitos_SeCrea(string dni)
    {
        var cliente = CrearCliente(dni: dni);

        Assert.Equal(dni, cliente.Dni);
    }

    [Theory]
    [InlineData("301234")]
    [InlineData("301234567")]
    [InlineData("30.123.456")]
    [InlineData("3012345A")]
    [InlineData("30 123456")]
    [InlineData("٣٠١٢٣٤٥٦")]
    public void Constructor_ConDniInvalido_LanzaDomainException(string dni)
    {
        var ex = Assert.Throws<DomainException>(() => CrearCliente(dni: dni));

        Assert.Contains("DNI", ex.Message);
    }

    [Theory]
    [InlineData("ana.perez")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    public void Constructor_ConEmailInvalido_LanzaDomainException(string email)
    {
        var ex = Assert.Throws<DomainException>(() => CrearCliente(email: email));

        Assert.Contains("email", ex.Message);
    }

    [Fact]
    public void DarDeBaja_UnActivo_GuardaLaFechaYQuedaInactivo()
    {
        var cliente = CrearCliente();

        cliente.DarDeBaja(FechaBaja);

        Assert.False(cliente.EstaActivo);
        Assert.Equal(FechaBaja, cliente.FechaBaja);
    }

    [Fact]
    public void DarDeBaja_UnoYaDadoDeBaja_LanzaConflictoExceptionYConservaLaFechaOriginal()
    {
        var cliente = CrearCliente();
        cliente.DarDeBaja(FechaBaja);

        var ex = Assert.Throws<ConflictoException>(() => cliente.DarDeBaja(FechaBaja.AddDays(1)));

        Assert.Contains("ya está dado de baja", ex.Message);
        Assert.Equal(FechaBaja, cliente.FechaBaja);
    }

    [Fact]
    public void Reactivar_UnoDadoDeBaja_QuedaActivoSinFechaDeBaja()
    {
        var cliente = CrearCliente();
        cliente.DarDeBaja(FechaBaja);

        cliente.Reactivar();

        Assert.True(cliente.EstaActivo);
        Assert.Null(cliente.FechaBaja);
    }

    [Fact]
    public void Reactivar_UnActivo_LanzaConflictoException()
    {
        var cliente = CrearCliente();

        var ex = Assert.Throws<ConflictoException>(cliente.Reactivar);

        Assert.Contains("ya está activo", ex.Message);
    }
}
