namespace SmartShip.Identity.Application.Interfaces.Persistence;

/// Defines the Unit of Work contract for managing database transactions and committing changes in IdentityService.
public interface IUnitOfWork
{

    /// <param name="cancellationToken">Cancellation token to observe while waiting for the task to complete.</param>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}