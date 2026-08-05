using Microsoft.EntityFrameworkCore;
using SmartShip.Admin.Domain.Entities;
using SmartShip.Admin.Infrastructure.Context;

namespace SmartShip.Admin.Infrastructure.Repositories;

public class HubRepository : IHubRepository
{
    private readonly AdminDbContext _context;

    public HubRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<Hub?> GetByIdAsync(int id)  => await _context.Hubs.FindAsync(id);

    public async Task<Hub> AddAsync(Hub hub)
    {
        await _context.Hubs.AddAsync(hub);
        return hub;
    }

    public Task UpdateAsync(Hub hub)
    {
        _context.Hubs.Update(hub);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Hub hub)
    {
        _context.Hubs.Remove(hub);
        return Task.CompletedTask;
    }

    public async Task<List<Hub>> GetAllActiveAsync() => 
        await _context.Hubs
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();
}