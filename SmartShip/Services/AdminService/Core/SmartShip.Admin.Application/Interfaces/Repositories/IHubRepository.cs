using SmartShip.Admin.Domain.Entities;
public interface IHubRepository
{
    Task<Hub?> GetByIdAsync(int id);
    Task<Hub> AddAsync(Hub hub);
    Task UpdateAsync(Hub hub);
    Task DeleteAsync(Hub hub);
    Task<List<Hub>> GetAllActiveAsync();
}

