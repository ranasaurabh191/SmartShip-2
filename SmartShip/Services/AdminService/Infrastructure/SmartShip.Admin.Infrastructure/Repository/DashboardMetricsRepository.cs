using Microsoft.EntityFrameworkCore;
using SmartShip.Admin.Domain.Entities;
using SmartShip.Admin.Infrastructure.Context;

namespace SmartShip.Admin.Infrastructure.Repositories;

public class DashboardMetricsRepository : IDashboardMetricsRepository
{
    private readonly AdminDbContext _context;

    public DashboardMetricsRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetrics?> GetFirstAsync() => await _context.DashboardMetrics.FirstOrDefaultAsync();

    public async Task AddAsync(DashboardMetrics metrics) => await _context.DashboardMetrics.AddAsync(metrics);

    public Task UpdateAsync(DashboardMetrics metrics)
    {
        _context.DashboardMetrics.Update(metrics);
        return Task.CompletedTask;
    }
}