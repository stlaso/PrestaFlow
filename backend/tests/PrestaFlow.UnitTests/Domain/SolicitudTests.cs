using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;
using PrestaFlow.Domain.Exceptions;
using PrestaFlow.UnitTests.Builders;

namespace PrestaFlow.UnitTests.Domain;

public class SolicitudTests
{
    private static readonly DateOnly DiaCreacion = new(2026, 3, 10);
    private static readonly DateTime FechaCreacion = new(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly PrimerPago = new(2026, 4, 10);
    private static readonly DateTime FechaResolucion = new(2026, 3, 11, 13, 0, 0, DateTimeKind.Utc);

    private static Solicitud CrearSolicitud(
        Cliente? cliente = null,
        decimal monto = 120_000m,
        int cuotas = 12,
        decimal ingreso = 500_000m,
        DateOnly? primerPago = null,
        DateOnly? diaCreacion = null) =>
        new(
            cliente: cliente ?? new ClienteBuilder().Build(),
            monto: monto,
            cantidadCuotas: cuotas,
            ingresoMensual: ingreso,
            fechaPrimerPago: primerPago ?? PrimerPago,
            fechaCreacion: FechaCreacion,
            diaCreacion: diaCreacion ?? DiaCreacion);

    private static Solicitud CrearResuelta(EstadoSolicitud estado)
    {
        var solicitud = CrearSolicitud();

        if (estado == EstadoSolicitud.Aprobada)
            solicitud.Aprobar(FechaResolucion);
        else
            solicitud.Rechazar("Ingresos insuficientes", FechaResolucion);

        return solicitud;
    }

    [Fact]
    public void Constructor_ConDatosValidos_QuedaPendienteConElRiesgoCalculado()
    {
        var solicitud = CrearSolicitud(monto: 120_000m, cuotas: 12, ingreso: 500_000m);

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Equal(10_000m, solicitud.Cuota);
        Assert.Equal(NivelRiesgo.Bajo, solicitud.Riesgo);
        Assert.Equal(FechaCreacion, solicitud.FechaCreacion);
        Assert.Null(solicitud.FechaResolucion);
        Assert.Null(solicitud.MotivoRechazo);
    }

    [Fact]
    public void Constructor_SinCliente_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Solicitud(
            cliente: null!,
            monto: 1000m,
            cantidadCuotas: 1,
            ingresoMensual: 1000m,
            fechaPrimerPago: PrimerPago,
            fechaCreacion: FechaCreacion,
            diaCreacion: DiaCreacion));
    }

    [Fact]
    public void Constructor_ConClienteDadoDeBaja_LanzaConflictoException()
    {
        var cliente = new ClienteBuilder().DadoDeBaja().Build();

        var ex = Assert.Throws<ConflictoException>(() => CrearSolicitud(cliente: cliente));

        Assert.Contains("dado de baja", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ConMontoNoPositivo_LanzaDomainException(int monto)
    {
        var ex = Assert.Throws<DomainException>(() => CrearSolicitud(monto: monto));

        Assert.Contains("monto", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void Constructor_ConCuotasEnElRango_SeCrea(int cuotas)
    {
        var solicitud = CrearSolicitud(cuotas: cuotas);

        Assert.Equal(cuotas, solicitud.CantidadCuotas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Constructor_ConCuotasFueraDelRango_LanzaDomainException(int cuotas)
    {
        var ex = Assert.Throws<DomainException>(() => CrearSolicitud(cuotas: cuotas));

        Assert.Contains("cuotas", ex.Message);
    }

    [Fact]
    public void Constructor_ConPrimerPagoElMismoDiaDeCreacion_LanzaDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => CrearSolicitud(primerPago: DiaCreacion));

        Assert.Contains("primer pago", ex.Message);
    }

    [Fact]
    public void Constructor_ConPrimerPagoAlDiaSiguiente_SeCrea()
    {
        var solicitud = CrearSolicitud(primerPago: DiaCreacion.AddDays(1));

        Assert.Equal(DiaCreacion.AddDays(1), solicitud.FechaPrimerPago);
    }

    [Fact]
    public void Constructor_SiCumple18ElDiaDeCreacion_SeCrea()
    {
        var cliente = new ClienteBuilder().ConFechaNacimiento(DiaCreacion.AddYears(-18)).Build();

        var solicitud = CrearSolicitud(cliente: cliente);

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
    }

    [Fact]
    public void Constructor_SiCumple18AlDiaSiguiente_LanzaDomainException()
    {
        var cliente = new ClienteBuilder().ConFechaNacimiento(DiaCreacion.AddYears(-18).AddDays(1)).Build();

        var ex = Assert.Throws<DomainException>(() => CrearSolicitud(cliente: cliente));

        Assert.Contains("mayor de 18", ex.Message);
    }

    // AddYears en un año no bisiesto lleva el 29/02 al 28/02.
    [Fact]
    public void Constructor_NacidoUn29DeFebrero_Cumple18El28DeFebrero()
    {
        var cliente = new ClienteBuilder().ConFechaNacimiento(new DateOnly(2008, 2, 29)).Build();
        var dia = new DateOnly(2026, 2, 28);

        var solicitud = CrearSolicitud(cliente: cliente, diaCreacion: dia, primerPago: dia.AddMonths(1));

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(60)]
    public void Constructor_SiCumple65ElDiaDelUltimoPago_LanzaDomainException(int cuotas)
    {
        var ultimoPago = PrimerPago.AddMonths(cuotas - 1);
        var cliente = new ClienteBuilder().ConFechaNacimiento(ultimoPago.AddYears(-65)).Build();

        var ex = Assert.Throws<DomainException>(() => CrearSolicitud(cliente: cliente, cuotas: cuotas));

        Assert.Contains("cumpla 65", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(60)]
    public void Constructor_SiCumple65ElDiaDespuesDelUltimoPago_SeCrea(int cuotas)
    {
        var ultimoPago = PrimerPago.AddMonths(cuotas - 1);
        var cliente = new ClienteBuilder().ConFechaNacimiento(ultimoPago.AddYears(-65).AddDays(1)).Build();

        var solicitud = CrearSolicitud(cliente: cliente, cuotas: cuotas);

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
    }

    public static TheoryData<decimal, int, decimal> MontosYCuotas => new()
    {
        { 1000m, 3, 333.33m },
        { 2000m, 3, 666.67m },
        { 1000.01m, 2, 500.01m },
    };

    [Theory]
    [MemberData(nameof(MontosYCuotas))]
    public void Cuota_SeRedondeaADosDecimalesAlejandoseDelCero(decimal monto, int cuotas, decimal esperada)
    {
        var solicitud = CrearSolicitud(monto: monto, cuotas: cuotas);

        Assert.Equal(esperada, solicitud.Cuota);
    }

    // 600,01 / 3 = 200,0033...: sin redondear sería 20,0003% del ingreso (Medio).
    [Fact]
    public void Constructor_CalculaElRiesgoConLaCuotaRedondeada()
    {
        var solicitud = CrearSolicitud(monto: 600.01m, cuotas: 3, ingreso: 1000m);

        Assert.Equal(200.00m, solicitud.Cuota);
        Assert.Equal(NivelRiesgo.Bajo, solicitud.Riesgo);
    }

    [Fact]
    public void Aprobar_UnaPendiente_QuedaAprobadaConFechaDeResolucion()
    {
        var solicitud = CrearSolicitud();

        solicitud.Aprobar(FechaResolucion);

        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucion);
    }

    [Fact]
    public void Rechazar_UnaPendiente_QuedaRechazadaConMotivoSinEspacios()
    {
        var solicitud = CrearSolicitud();

        solicitud.Rechazar("  Ingresos insuficientes  ", FechaResolucion);

        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal("Ingresos insuficientes", solicitud.MotivoRechazo);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechazar_SinMotivo_LanzaDomainExceptionYSigueSiendoPendiente(string motivo)
    {
        var solicitud = CrearSolicitud();

        var ex = Assert.Throws<DomainException>(() => solicitud.Rechazar(motivo, FechaResolucion));

        Assert.Contains("motivo", ex.Message);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.FechaResolucion);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Aprobada)]
    [InlineData(EstadoSolicitud.Rechazada)]
    public void Aprobar_UnaResuelta_LanzaConflictoException(EstadoSolicitud estado)
    {
        var solicitud = CrearResuelta(estado);

        var ex = Assert.Throws<ConflictoException>(() => solicitud.Aprobar(FechaResolucion.AddDays(1)));

        Assert.Contains("pendiente", ex.Message);
        Assert.Equal(estado, solicitud.Estado);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Aprobada)]
    [InlineData(EstadoSolicitud.Rechazada)]
    public void Rechazar_UnaResuelta_LanzaConflictoException(EstadoSolicitud estado)
    {
        var solicitud = CrearResuelta(estado);

        var ex = Assert.Throws<ConflictoException>(() => solicitud.Rechazar("Otro motivo", FechaResolucion.AddDays(1)));

        Assert.Contains("pendiente", ex.Message);
        Assert.Equal(estado, solicitud.Estado);
    }
}
