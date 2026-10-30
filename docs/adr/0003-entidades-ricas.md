# ADR-0003: Entidades ricas y dos tipos de excepción de negocio

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

Las reglas de una solicitud (solo se aprueba una pendiente, rechazar exige motivo, el riesgo depende de la cuota y el ingreso) tienen que cumplirse siempre, sin importar desde qué caso de uso se modifique la entidad.

## Decisión

- Entidades con propiedades `private set`, un constructor que valida, métodos de negocio (`Aprobar`, `Rechazar`, `DarDeBaja`, `Reactivar`) y un constructor privado sin parámetros para EF Core.
- Los services aplican *tell, don't ask*: llaman a `solicitud.Aprobar(fecha)` en lugar de consultar el estado y decidir afuera.
- Reglas que viven en el Domain:
  - Cuota = Monto / CantidadCuotas, sin interés, redondeada a 2 decimales.
  - Riesgo según cuota / ingreso: Bajo hasta 20% inclusive, Medio hasta 30% inclusive, Alto más de 30%. Se calcula al crear la solicitud y se guarda, porque es una foto del momento.
  - Monto mayor a cero, 1 a 60 cuotas, primer pago posterior al día de creación.
  - Mayor de 18 años al crear la solicitud y menor de 65 al último pago (política de crédito, no característica del cliente).
- Dos excepciones de negocio, sin dependencias de HTTP:
  - `DomainException`: el pedido viola una regla.
  - `ConflictoException`: el pedido es válido pero choca con el estado actual (aprobar una ya aprobada, crear una solicitud para un cliente dado de baja).
- La plata se maneja con `decimal`.
- Constantes públicas en las entidades (`Cliente.LargoMaximoTexto`, `Solicitud.LargoMaximoMotivo`) que reutilizan la configuración de EF y los validadores.

## Alternativas consideradas

- **Modelo anémico** (propiedades públicas y lógica en los services): cualquier service puede hacer `Estado = Aprobada` y saltearse la regla, y las validaciones terminan repetidas.
- **Una sola excepción de negocio:** no permitiría distinguir un pedido inválido (400) de un conflicto con el estado (409).
- **`double` para importes:** es binario y no representa exactamente valores como 0.1; genera diferencias de centavos.

## Consecuencias

- Cada regla está en un solo lugar y se testea sin infraestructura.
- Cambiar una regla (por ejemplo, un nuevo estado) no obliga a revisar los services.
- EF Core necesita el constructor privado y el mapeo de propiedades con setter privado.
