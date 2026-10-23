namespace PrestaFlow.Domain.Exceptions;

// Pedido válido que choca con el estado actual (ej.: DNI ya registrado, aprobar una solicitud ya resuelta).
public sealed class ConflictoException : Exception
{
    public ConflictoException(string message) : base(message)
    {
    }
}
