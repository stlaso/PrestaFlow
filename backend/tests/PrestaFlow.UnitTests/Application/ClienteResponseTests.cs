using PrestaFlow.Application.Dtos;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.UnitTests.Application;

public class ClienteResponseTests
{
    private static Cliente CrearConValoresDistintos() => new(
        nombre: "Ana",
        apellido: "Pérez",
        dni: "30123456",
        provincia: "Mendoza",
        localidad: "Godoy Cruz",
        telefono: "2614567890",
        email: "ana.perez@example.com",
        fechaNacimiento: new DateOnly(1990, 5, 15),
        ocupacion: "Docente");

    // El Id lo asigna la base: se cubre en los tests de integración.
    [Fact]
    public void Desde_MapeaCadaCampoAlSuyo()
    {
        var response = ClienteResponse.Desde(CrearConValoresDistintos());

        Assert.Equal("Ana", response.Nombre);
        Assert.Equal("Pérez", response.Apellido);
        Assert.Equal("30123456", response.Dni);
        Assert.Equal("Mendoza", response.Provincia);
        Assert.Equal("Godoy Cruz", response.Localidad);
        Assert.Equal("2614567890", response.Telefono);
        Assert.Equal("ana.perez@example.com", response.Email);
        Assert.Equal(new DateOnly(1990, 5, 15), response.FechaNacimiento);
        Assert.Equal("Docente", response.Ocupacion);
        Assert.True(response.Activo);
        Assert.Null(response.FechaBaja);
    }

    [Fact]
    public void Desde_UnClienteDadoDeBaja_InformaLaBaja()
    {
        var fechaBaja = new DateTime(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);
        var cliente = CrearConValoresDistintos();
        cliente.DarDeBaja(fechaBaja);

        var response = ClienteResponse.Desde(cliente);

        Assert.False(response.Activo);
        Assert.Equal(fechaBaja, response.FechaBaja);
    }
}
