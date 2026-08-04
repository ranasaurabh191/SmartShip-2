using SmartShip.Admin.Domain.Entities;
public interface IDashboardMetricsRepository
{
    Task<DashboardMetrics?> GetFirstAsync();
    Task AddAsync(DashboardMetrics metrics);
    Task UpdateAsync(DashboardMetrics metrics);
}