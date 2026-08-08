
using SmartShip.Identity.Domain.Entities;

namespace SmartShip.Identity.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsActiveByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailForAdminAsync(string email);
    Task AddAsync(User user);
    void Update(User user);
    void Delete(User user);
}