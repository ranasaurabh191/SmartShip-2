using SmartShip.Admin.Application.DTOs;
public interface IAdminService
{
    Task<DashboardMetricsDto> GetDashboardAsync();
    Task<HubDto> GetHubByIdAsync(int id);
    Task<HubDto> CreateHubAsync(CreateHubRequest req);
    Task UpdateHubAsync(int id, UpdateHubRequest req);
    Task DeleteHubAsync(int id);
    Task<IEnumerable<HubDto>> GetAllActiveHubsAsync();
    Task<ReportDto> GenerateReportAsync(ReportRequest req);
}