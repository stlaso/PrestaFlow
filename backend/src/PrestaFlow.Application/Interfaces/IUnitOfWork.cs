namespace PrestaFlow.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
