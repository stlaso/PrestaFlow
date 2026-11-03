using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Application.Services;
using PrestaFlow.Application.Validators;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Exceptions;
using PrestaFlow.UnitTests.Builders;

namespace PrestaFlow.UnitTests.Application;

public class ClienteServiceTests
{
    private const string DniValido = "30123456";

    private static readonly DateTime Ahora = new(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<ISolicitudRepository> _solicitudes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        var reloj = new RelojNegocio(
            new FakeTimeProvider(new DateTimeOffset(Ahora)),
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"));

        _service = new ClienteService(
            _clientes.Object,
            _solicitudes.Object,
            _unitOfWork.Object,
            new CrearClienteRequestValidator(reloj),
            reloj);
    }

    private static CrearClienteRequest CrearRequest(string dni = DniValido, string nombre = "Ana") => new(
        Nombre: nombre,
        Apellido: "Pérez",
        Dni: dni,
        Provincia: "Mendoza",
        Localidad: "Godoy Cruz",
        Telefono: "2614567890",
        Email: "ana.perez@example.com",
        FechaNacimiento: new DateOnly(1990, 5, 15),
        Ocupacion: "Docente");

    private void ConfigurarBusquedaPorId(int id, Cliente? cliente) =>
        _clientes
            .Setup(r => r.ObtenerPorIdIncluyendoBajasAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

    private void ConfigurarBusquedaPorDni(string dni, Cliente? cliente) =>
        _clientes
            .Setup(r => r.ObtenerPorDniIncluyendoBajasAsync(dni, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

    private void VerificarGuardado(Times veces) =>
        _unitOfWork.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), veces);

    private void VerificarQueNoAgrego() =>
        _clientes.Verify(r => r.Agregar(It.IsAny<Cliente>()), Times.Never);

    [Fact]
    public async Task CrearAsync_ConDatosValidos_AgregaUnClienteConCadaCampoEnSuLugarYGuarda()
    {
        Cliente? agregado = null;
        _clientes
            .Setup(r => r.Agregar(It.IsAny<Cliente>()))
            .Callback<Cliente>(c => agregado = c);

        var response = await _service.CrearAsync(CrearRequest(), Ct);

        Assert.NotNull(agregado);
        Assert.Equal("Ana", agregado.Nombre);
        Assert.Equal("Pérez", agregado.Apellido);
        Assert.Equal(DniValido, agregado.Dni);
        Assert.Equal("Mendoza", agregado.Provincia);
        Assert.Equal("Godoy Cruz", agregado.Localidad);
        Assert.Equal("2614567890", agregado.Telefono);
        Assert.Equal("ana.perez@example.com", agregado.Email);
        Assert.Equal(new DateOnly(1990, 5, 15), agregado.FechaNacimiento);
        Assert.Equal("Docente", agregado.Ocupacion);
        Assert.Equal(DniValido, response.Dni);
        VerificarGuardado(Times.Once());
    }

    [Fact]
    public async Task CrearAsync_BuscaElDuplicadoConElDniNormalizado()
    {
        await _service.CrearAsync(CrearRequest(dni: $" {DniValido} "), Ct);

        _clientes.Verify(r => r.ObtenerPorDniIncluyendoBajasAsync(DniValido, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_ConDniDeUnClienteActivo_LanzaConflictoYNoGuarda()
    {
        ConfigurarBusquedaPorDni(DniValido, new ClienteBuilder().ConDni(DniValido).Build());

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(CrearRequest(), Ct));

        Assert.Contains("Ya existe un cliente", ex.Message);
        VerificarQueNoAgrego();
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_ConDniDeUnClienteDadoDeBaja_IndicaReactivarloYNoGuarda()
    {
        ConfigurarBusquedaPorDni(DniValido, new ClienteBuilder().ConDni(DniValido).DadoDeBaja().Build());

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(CrearRequest(), Ct));

        Assert.Contains("Reactivalo", ex.Message);
        VerificarQueNoAgrego();
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_ConRequestInvalido_LanzaValidationExceptionSinConsultarElRepositorio()
    {
        var request = CrearRequest(nombre: new string('a', Cliente.LargoMaximoTexto + 1));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearAsync(request, Ct));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearClienteRequest.Nombre));
        _clientes.VerifyNoOtherCalls();
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdInexistente_DevuelveNull()
    {
        var response = await _service.ObtenerPorIdAsync(1, Ct);

        Assert.Null(response);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdExistente_DevuelveElCliente()
    {
        _clientes
            .Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteBuilder().ConDni(DniValido).Build());

        var response = await _service.ObtenerPorIdAsync(1, Ct);

        Assert.NotNull(response);
        Assert.Equal(DniValido, response.Dni);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ListarAsync_PasaElFiltroDeBajasAlRepositorioYMapeaLaLista(bool incluirBajas)
    {
        _clientes
            .Setup(r => r.ListarAsync(incluirBajas, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClienteBuilder().ConDni(DniValido).Build()]);

        var lista = await _service.ListarAsync(incluirBajas, Ct);

        var cliente = Assert.Single(lista);
        Assert.Equal(DniValido, cliente.Dni);
    }

    [Fact]
    public async Task DarDeBajaAsync_ConIdInexistente_DevuelveFalseSinGuardar()
    {
        var resultado = await _service.DarDeBajaAsync(1, Ct);

        Assert.False(resultado);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task DarDeBajaAsync_SinPendientes_RegistraLaFechaDelRelojYGuarda()
    {
        var cliente = new ClienteBuilder().Build();
        ConfigurarBusquedaPorId(1, cliente);

        var resultado = await _service.DarDeBajaAsync(1, Ct);

        Assert.True(resultado);
        Assert.Equal(Ahora, cliente.FechaBaja);
        VerificarGuardado(Times.Once());
    }

    [Fact]
    public async Task DarDeBajaAsync_ConPendientes_LanzaConflictoSinTocarElClienteNiGuardar()
    {
        var cliente = new ClienteBuilder().Build();
        ConfigurarBusquedaPorId(1, cliente);
        _solicitudes
            .Setup(r => r.ExistenPendientesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.DarDeBajaAsync(1, Ct));

        Assert.Contains("solicitudes pendientes", ex.Message);
        Assert.True(cliente.EstaActivo);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task DarDeBajaAsync_UnoYaDadoDeBaja_LanzaConflictoYNoGuarda()
    {
        ConfigurarBusquedaPorId(1, new ClienteBuilder().DadoDeBaja().Build());

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.DarDeBajaAsync(1, Ct));

        Assert.Contains("ya está dado de baja", ex.Message);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task ReactivarAsync_ConIdInexistente_DevuelveNullSinGuardar()
    {
        var response = await _service.ReactivarAsync(1, Ct);

        Assert.Null(response);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task ReactivarAsync_UnoDadoDeBaja_LoDejaActivoYGuarda()
    {
        ConfigurarBusquedaPorId(1, new ClienteBuilder().DadoDeBaja().Build());

        var response = await _service.ReactivarAsync(1, Ct);

        Assert.NotNull(response);
        Assert.True(response.Activo);
        Assert.Null(response.FechaBaja);
        VerificarGuardado(Times.Once());
    }

    [Fact]
    public async Task ReactivarAsync_UnoActivo_LanzaConflictoYNoGuarda()
    {
        ConfigurarBusquedaPorId(1, new ClienteBuilder().Build());

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.ReactivarAsync(1, Ct));

        Assert.Contains("ya está activo", ex.Message);
        VerificarGuardado(Times.Never());
    }
}
