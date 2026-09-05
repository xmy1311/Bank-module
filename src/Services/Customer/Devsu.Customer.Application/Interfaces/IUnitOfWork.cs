namespace Devsu.Customer.Application.Interfaces;


public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
