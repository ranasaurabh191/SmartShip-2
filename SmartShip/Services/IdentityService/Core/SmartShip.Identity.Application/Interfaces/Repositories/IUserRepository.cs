using SmartShip.Identity.Domain.Entities;

namespace SmartShip.Identity.Application.Interfaces.Repositories;

public interface IUserRepository
{
    /// Checks whether a user account exists with the specified email address.
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// Checks whether an active user account exists with the specified user ID.
    Task<bool> ExistsActiveByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// Retrieves an active user record by email address.
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// Retrieves a user record by primary key identifier.
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// Retrieves a user record by email address regardless of active status (for Administrative operations).
    Task<User?> GetByEmailForAdminAsync(string email, CancellationToken cancellationToken = default);

    /// Adds a new user entity to the persistence context.
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// Marks an existing user entity as modified in the persistence context.
    void Update(User user);

    /// Marks an existing user entity for deletion from the persistence context.
    void Delete(User user);
}