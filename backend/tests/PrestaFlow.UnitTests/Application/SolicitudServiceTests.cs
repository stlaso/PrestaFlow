using FluentValidation;
using Moq;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Application.Services;
using PrestaFlow.Application.Validators;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;
using PrestaFlow.Domain.Exceptions;
using PrestaFlow.UnitTests.Builders;

namespace PrestaFlow.UnitTests.Application;

public class SolicitudServiceTests
{
    private const int ClienteId = 1;
    private const int SolicitudId = 10;

    private static readonly DateTime Ahora = new(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly PrimerPago = new(2026, 4, 10);

    // 23:00 del 10/03 en Argentina: en UTC ya es 11/03.
    private static readonly DateTime NocheArgentina = new(2026, 3, 11, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly DiaSiguienteALaNoche = new(2026, 3, 11);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly Mock<ISolicitudRepository> _solicitudes = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SolicitudService _service;

    public SolicitudServiceTests()
    {
        _service = CrearService(Ahora);
    }

    private SolicitudService CrearService(DateTime ahora) => new(
        _solicitudes.Object,
        _clientes.Object,
        _unitOfWork.Object,
        new CrearSolicitudRequestValidator(),
        new ListarSolicitudesRequestValidator(),
        new RechazarSolicitudRequestValidator(),
        RelojDePrueba.Crear(ahora));

    private static CrearSolicitudRequest CrearRequest(
        int clienteId = ClienteId,
        decimal monto = 120_000m,
        DateOnly? primerPago = null) =>
        new(clienteId, monto, CantidadCuotas: 12, IngresoMensual: 500_000m, primerPago ?? PrimerPago);

    private static Solicitud CrearSolicitud() => new(
        cliente: new ClienteBuilder().Build(),
        monto: 120_000m,
        cantidadCuotas: 12,
        ingresoMensual: 500_000m,
        fechaPrimerPago: PrimerPago,
        fechaCreacion: Ahora,
        diaCreacion: DateOnly.FromDateTime(Ahora));

    private void ConfigurarCliente(Cliente? cliente) =>
        _clientes
            .Setup(r => r.ObtenerPorIdIncluyendoBajasAsync(ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

    private void ConfigurarSolicitud(Solicitud? solicitud) =>
        _solicitudes
            .Setup(r => r.ObtenerPorIdAsync(SolicitudId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

    private void ConfigurarListado(int total = 0, params Solicitud[] items) =>
        _solicitudes
            .Setup(r => r.ListarAsync(
                It.IsAny<EstadoSolicitud?>(),
                It.IsAny<NivelRiesgo?>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Solicitud>)items, total));

    private void VerificarGuardado(Times veces) =>
        _unitOfWork.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), veces);

    private void VerificarQueNoAgrego() =>
        _solicitudes.Verify(r => r.Agregar(It.IsAny<Solicitud>()), Times.Never);

    [Fact]
    public async Task CrearAsync_ConDatosValidos_AgregaUnaPendienteConLaFechaDelRelojYGuarda()
    {
        var cliente = new ClienteBuilder().Build();
        ConfigurarCliente(cliente);
        Solicitud? agregada = null;
        _solicitudes
            .Setup(r => r.Agregar(It.IsAny<Solicitud>()))
            .Callback<Solicitud>(s => agregada = s);

        var response = await _service.CrearAsync(CrearRequest(), Ct);

        Assert.NotNull(agregada);
        Assert.Same(cliente, agregada.Cliente);
        Assert.Equal(120_000m, agregada.Monto);
        Assert.Equal(12, agregada.CantidadCuotas);
        Assert.Equal(500_000m, agregada.IngresoMensual);
        Assert.Equal(PrimerPago, agregada.FechaPrimerPago);
        Assert.Equal(Ahora, agregada.FechaCreacion);
        Assert.Equal(EstadoSolicitud.Pendiente, response.Estado);
        Assert.Equal(NivelRiesgo.Bajo, response.Riesgo);
        VerificarGuardado(Times.Once());
    }

    [Fact]
    public async Task CrearAsync_ConClienteInexistente_InformaElErrorSobreClienteIdYNoGuarda()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearAsync(CrearRequest(), Ct));

        var error = Assert.Single(ex.Errors);
        Assert.Equal(nameof(CrearSolicitudRequest.ClienteId), error.PropertyName);
        Assert.Contains("No existe un cliente", error.ErrorMessage);
        VerificarQueNoAgrego();
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_ConClienteIdCero_LanzaValidationExceptionSinConsultarElRepositorio()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearAsync(CrearRequest(clienteId: 0), Ct));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearSolicitudRequest.ClienteId));
        _clientes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CrearAsync_ConMontoDeTresDecimales_LanzaValidationExceptionSinConsultarElRepositorio()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CrearAsync(CrearRequest(monto: 1000.555m), Ct));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearSolicitudRequest.Monto));
        _clientes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CrearAsync_ConUnaSolicitudPendiente_LanzaConflictoYNoGuarda()
    {
        ConfigurarCliente(new ClienteBuilder().Build());
        // El cliente no pasó por la base: su Id es 0, no ClienteId.
        _solicitudes
            .Setup(r => r.ExistenPendientesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(CrearRequest(), Ct));

        Assert.Contains("ya tiene una solicitud pendiente", ex.Message);
        VerificarQueNoAgrego();
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_ConClienteDadoDeBaja_LanzaConflictoYNoGuarda()
    {
        ConfigurarCliente(new ClienteBuilder().DadoDeBaja().Build());

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.CrearAsync(CrearRequest(), Ct));

        Assert.Contains("dado de baja", ex.Message);
        VerificarQueNoAgrego();
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_DeNocheEnArgentina_AceptaElPrimerPagoAlDiaSiguienteLocal()
    {
        ConfigurarCliente(new ClienteBuilder().Build());
        var service = CrearService(NocheArgentina);

        var response = await service.CrearAsync(CrearRequest(primerPago: DiaSiguienteALaNoche), Ct);

        Assert.Equal(DiaSiguienteALaNoche, response.FechaPrimerPago);
        Assert.Equal(NocheArgentina, response.FechaCreacion);
    }

    [Fact]
    public async Task CrearAsync_DeNocheEnArgentina_RechazaAQuienCumple18AlDiaSiguienteLocal()
    {
        var nacimiento = DiaSiguienteALaNoche.AddYears(-18);
        ConfigurarCliente(new ClienteBuilder().ConFechaNacimiento(nacimiento).Build());
        var service = CrearService(NocheArgentina);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CrearAsync(CrearRequest(), Ct));

        Assert.Contains("mayor de 18", ex.Message);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdInexistente_DevuelveNull()
    {
        var response = await _service.ObtenerPorIdAsync(SolicitudId, Ct);

        Assert.Null(response);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdExistente_DevuelveLaSolicitud()
    {
        ConfigurarSolicitud(CrearSolicitud());

        var response = await _service.ObtenerPorIdAsync(SolicitudId, Ct);

        Assert.NotNull(response);
        Assert.Equal(120_000m, response.Monto);
    }

    [Fact]
    public async Task ListarAsync_PasaLosFiltrosAlRepositorioYArmaLaPagina()
    {
        ConfigurarListado(11, CrearSolicitud());
        var request = new ListarSolicitudesRequest
        {
            Estado = EstadoSolicitud.Aprobada,
            Riesgo = NivelRiesgo.Alto,
            ClienteId = 7,
            Pagina = 2,
            TamanioPagina = 5
        };

        var pagina = await _service.ListarAsync(request, Ct);

        Assert.Single(pagina.Items);
        Assert.Equal(2, pagina.Pagina);
        Assert.Equal(5, pagina.TamanioPagina);
        Assert.Equal(11, pagina.Total);
        _solicitudes.Verify(r => r.ListarAsync(
            EstadoSolicitud.Aprobada,
            NivelRiesgo.Alto,
            7,
            2,
            5,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, ListarSolicitudesRequest.TamanioPaginaMaximo)]
    public async Task ListarAsync_ConPaginacionEnElRango_ConsultaElRepositorio(int pagina, int tamanio)
    {
        ConfigurarListado();

        var respuesta = await _service.ListarAsync(new ListarSolicitudesRequest { Pagina = pagina, TamanioPagina = tamanio }, Ct);

        Assert.Empty(respuesta.Items);
    }

    [Theory]
    [InlineData(0, 20, nameof(ListarSolicitudesRequest.Pagina))]
    [InlineData(1, 0, nameof(ListarSolicitudesRequest.TamanioPagina))]
    [InlineData(1, ListarSolicitudesRequest.TamanioPaginaMaximo + 1, nameof(ListarSolicitudesRequest.TamanioPagina))]
    public async Task ListarAsync_ConPaginacionFueraDelRango_LanzaValidationExceptionSinConsultar(int pagina, int tamanio, string campo)
    {
        var request = new ListarSolicitudesRequest { Pagina = pagina, TamanioPagina = tamanio };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ListarAsync(request, Ct));

        Assert.Contains(ex.Errors, e => e.PropertyName == campo);
        _solicitudes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AprobarAsync_ConIdInexistente_DevuelveNullSinGuardar()
    {
        var response = await _service.AprobarAsync(SolicitudId, Ct);

        Assert.Null(response);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task AprobarAsync_UnaPendiente_UsaLaFechaDelRelojYGuarda()
    {
        ConfigurarSolicitud(CrearSolicitud());
        var service = CrearService(Ahora.AddDays(1));

        var response = await service.AprobarAsync(SolicitudId, Ct);

        Assert.NotNull(response);
        Assert.Equal(EstadoSolicitud.Aprobada, response.Estado);
        Assert.Equal(Ahora.AddDays(1), response.FechaResolucion);
        VerificarGuardado(Times.Once());
    }

    [Fact]
    public async Task AprobarAsync_UnaResuelta_LanzaConflictoYNoGuarda()
    {
        var solicitud = CrearSolicitud();
        solicitud.Aprobar(Ahora);
        ConfigurarSolicitud(solicitud);

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _service.AprobarAsync(SolicitudId, Ct));

        Assert.Contains("pendiente", ex.Message);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task RechazarAsync_ConIdInexistente_DevuelveNullSinGuardar()
    {
        var response = await _service.RechazarAsync(SolicitudId, new RechazarSolicitudRequest("Sin ingresos"), Ct);

        Assert.Null(response);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task RechazarAsync_UnaPendiente_GuardaElMotivoConLaFechaDelReloj()
    {
        ConfigurarSolicitud(CrearSolicitud());
        var service = CrearService(Ahora.AddDays(1));

        var response = await service.RechazarAsync(SolicitudId, new RechazarSolicitudRequest("Sin ingresos"), Ct);

        Assert.NotNull(response);
        Assert.Equal(EstadoSolicitud.Rechazada, response.Estado);
        Assert.Equal("Sin ingresos", response.MotivoRechazo);
        Assert.Equal(Ahora.AddDays(1), response.FechaResolucion);
        VerificarGuardado(Times.Once());
    }

    [Fact]
    public async Task RechazarAsync_ConMotivoDemasiadoLargo_LanzaValidationExceptionSinConsultarElRepositorio()
    {
        var request = new RechazarSolicitudRequest(new string('a', Solicitud.LargoMaximoMotivo + 1));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RechazarAsync(SolicitudId, request, Ct));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(RechazarSolicitudRequest.Motivo));
        _solicitudes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RechazarAsync_SinMotivo_LanzaDomainExceptionSinModificarNiGuardar()
    {
        var solicitud = CrearSolicitud();
        ConfigurarSolicitud(solicitud);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.RechazarAsync(SolicitudId, new RechazarSolicitudRequest("   "), Ct));

        Assert.Contains("motivo", ex.Message);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        VerificarGuardado(Times.Never());
    }

    [Fact]
    public async Task RechazarAsync_UnaResuelta_LanzaConflictoYNoGuarda()
    {
        var solicitud = CrearSolicitud();
        solicitud.Aprobar(Ahora);
        ConfigurarSolicitud(solicitud);

        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _service.RechazarAsync(SolicitudId, new RechazarSolicitudRequest("Sin ingresos"), Ct));

        Assert.Contains("pendiente", ex.Message);
        VerificarGuardado(Times.Never());
    }
}
