using PrestaFlow.Application.Dtos;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Enums;
using PrestaFlow.UnitTests.Builders;

namespace PrestaFlow.UnitTests.Application;

public class SolicitudResponseTests
{
    private static readonly DateOnly PrimerPago = new(2026, 4, 10);
    private static readonly DateTime FechaCreacion = new(2026, 3, 10, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaResolucion = new(2026, 3, 11, 13, 0, 0, DateTimeKind.Utc);

    private static Solicitud CrearConValoresDistintos() => new(
        cliente: new ClienteBuilder().ConDni("30123456").Build(),
        monto: 30_000m,
        cantidadCuotas: 6,
        ingresoMensual: 20_000m,
        fechaPrimerPago: PrimerPago,
        fechaCreacion: FechaCreacion,
        diaCreacion: new DateOnly(2026, 3, 10));

    [Fact]
    public void Desde_UnaPendiente_MapeaCadaCampoAlSuyo()
    {
        var response = SolicitudResponse.Desde(CrearConValoresDistintos());

        Assert.Equal("Ana", response.Cliente.Nombre);
        Assert.Equal("Pérez", response.Cliente.Apellido);
        Assert.Equal("30123456", response.Cliente.Dni);
        Assert.Equal(30_000m, response.Monto);
        Assert.Equal(6, response.CantidadCuotas);
        Assert.Equal(5_000m, response.Cuota);
        Assert.Equal(20_000m, response.IngresoMensual);
        Assert.Equal(NivelRiesgo.Medio, response.Riesgo);
        Assert.Equal(EstadoSolicitud.Pendiente, response.Estado);
        Assert.Equal(PrimerPago, response.FechaPrimerPago);
        Assert.Equal(FechaCreacion, response.FechaCreacion);
        Assert.Null(response.MotivoRechazo);
        Assert.Null(response.FechaResolucion);
    }

    [Fact]
    public void Desde_UnaRechazada_InformaElMotivoYLaResolucion()
    {
        var solicitud = CrearConValoresDistintos();
        solicitud.Rechazar("Ingresos insuficientes", FechaResolucion);

        var response = SolicitudResponse.Desde(solicitud);

        Assert.Equal(EstadoSolicitud.Rechazada, response.Estado);
        Assert.Equal("Ingresos insuficientes", response.MotivoRechazo);
        Assert.Equal(FechaResolucion, response.FechaResolucion);
    }
}
