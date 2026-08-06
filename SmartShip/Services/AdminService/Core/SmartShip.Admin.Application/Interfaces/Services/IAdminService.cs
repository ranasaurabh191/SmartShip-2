using SmartShip.Admin.Application.DTOs;
public interface IAdminService
{
    Task<DashboardMetricsDTO> GetDashboardAsync();
    Task<HubDTO> GetHubByIdAsync(int id);
    Task<HubDTO> CreateHubAsync(CreateHubRequest req);
    Task UpdateHubAsync(int id, UpdateHubRequest req);
    Task DeleteHubAsync(int id);
    Task<IEnumerable<HubDTO>> GetAllActiveHubsAsync();
    Task<ReportDTO> GenerateReportAsync(ReportRequest req);
}