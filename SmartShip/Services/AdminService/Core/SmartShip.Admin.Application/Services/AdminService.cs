
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartShip.Admin.Application.DTOs;
using SmartShip.Admin.Domain.Entities;
using SmartShip.Admin.Domain.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace SmartShip.Admin.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IHubRepository _hubRepository;
        private readonly IReportRepository _reportRepository;
        private readonly IDashboardMetricsRepository _dashboardMetricsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminService(
        IHubRepository hubRepository,
        IReportRepository reportRepository,
        IDashboardMetricsRepository dashboardMetricsRepository,
        IUnitOfWork unitOfWork,
        ILogger<AdminService> logger,
        IHttpContextAccessor httpContextAccessor)
        {
            _hubRepository = hubRepository;
            _reportRepository = reportRepository;
            _dashboardMetricsRepository = dashboardMetricsRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal GetCurrentUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return user;
        }
        private string GetCurrentUserName()
        {
            var user = GetCurrentUser();

            return user.FindFirstValue(ClaimTypes.Name)
                   ?? user.FindFirstValue(ClaimTypes.Email)
                   ?? "Admin";
        }
        private void EnsureAdminAccess()
        {
            var user = GetCurrentUser();

            var roleClaims = user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
            // getting all role claims of the user and converting them to a list of strings
            var hasAdminRole = roleClaims.Any(r => r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));

            if (!hasAdminRole) throw new UnauthorizedAccessException("Unauthorized.");
        }

        
        public async Task<DashboardMetricsDTO> GetDashboardAsync()
        {
            EnsureAdminAccess(); //security check to ensure the user has admin access
            _logger.LogInformation("Fetching Dashboard Metrics");

            var metrics = await _dashboardMetricsRepository.GetFirstAsync();
            if (metrics == null)
            {
                _logger.LogWarning("No dashboard metrics found.");
                metrics = new DashboardMetrics
                {
                    TotalShipments = 0,
                    ActiveShipments = 0,
                    DeliveredToday = 0,
                    TotalCustomers = 0,
                    LastUpdatedAt = DateTime.Now
                };
                await _dashboardMetricsRepository.AddAsync(metrics);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Default metrics row created.");
            }
            metrics.LastUpdatedAt = DateTime.Now;
            await _dashboardMetricsRepository.UpdateAsync(metrics);
            await _unitOfWork.SaveChangesAsync();

            return new DashboardMetricsDTO
            {
                TotalShipments = metrics.TotalShipments,
                ActiveShipments = metrics.ActiveShipments,
                DeliveredToday = metrics.DeliveredToday,
                TotalCustomers = metrics.TotalCustomers,
                LastUpdatedAt = metrics.LastUpdatedAt.HasValue
                ? DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt")
                : null
            };
        }
        public async Task<HubDTO> GetHubByIdAsync(int id)
        {
            EnsureAdminAccess();

            _logger.LogInformation("Fetching hub by ID: {HubId}", id);

            var h = await _hubRepository.GetByIdAsync(id);

            if (h == null)
            {
                _logger.LogWarning("Hub not found: ID {HubId}", id);
                throw new KeyNotFoundException($"Hub {id} not found.");
            }

            _logger.LogInformation("Hub found: {HubName} | City: {City}", h.Name, h.City);

            return new HubDTO(h.Id, h.Name, h.City, h.State, h.Country, h.ContactPhone, h.IsActive);
        }

        public async Task<HubDTO> CreateHubAsync(CreateHubRequest req)
        {
            EnsureAdminAccess();

            _logger.LogInformation("Creating hub: {HubName} | City: {City} | State: {State}",
                req.Name, req.City, req.State);

            var hub = new Hub
            {
                Name = req.Name,
                City = req.City,
                State = req.State,
                Country = req.Country,
                ContactPhone = req.ContactPhone
            };

            await _hubRepository.AddAsync(hub);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Hub created: ID {HubId} | {HubName} | {City}", hub.Id, hub.Name, hub.City);

            return new HubDTO(hub.Id, hub.Name, hub.City, hub.State, hub.Country, hub.ContactPhone, hub.IsActive);
        }

        public async Task UpdateHubAsync(int id, UpdateHubRequest req)
        {
            EnsureAdminAccess();

            _logger.LogInformation("Updating hub ID: {HubId} | Name: {HubName}", id, req.Name);

            var h = await _hubRepository.GetByIdAsync(id);
            if (h == null)
            {
                _logger.LogWarning("Hub not found for update: ID {HubId}", id);
                throw new KeyNotFoundException($"Hub {id} not found.");
            }

            h.Name = req.Name;
            h.City = req.City;
            h.State = req.State;
            h.Country = req.Country;
            h.ContactPhone = req.ContactPhone;
            h.IsActive = req.IsActive;

            await _hubRepository.UpdateAsync(h);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Hub updated: ID {HubId} | {HubName} | IsActive: {IsActive}",
                id, h.Name, h.IsActive);
        }
        public async Task DeleteHubAsync(int id)
        {
            EnsureAdminAccess();

            _logger.LogInformation("Deleting hub ID: {HubId}", id);

            var h = await _hubRepository.GetByIdAsync(id);
            if (h == null)
            {
                _logger.LogWarning("Hub not found for deletion: ID {HubId}", id);
                throw new KeyNotFoundException($"Hub {id} not found.");
            }

            await _hubRepository.DeleteAsync(h);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Hub deleted: ID {HubId} | {HubName}", id, h.Name);
        }
        public async Task<IEnumerable<HubDTO>> GetAllActiveHubsAsync()
        {
            var hubs = await _hubRepository.GetAllActiveAsync();
            return hubs.Select(h => new HubDTO(h.Id, h.Name, h.City, h.State, h.Country, h.ContactPhone, h.IsActive));
        }

        public async Task<ReportDTO> GenerateReportAsync(ReportRequest req)
        {
            EnsureAdminAccess();

            _logger.LogInformation("Generating {ReportType} report | From: {From} | To: {To}",
                req.ReportType, req.FromDate, req.ToDate);

            Enum.TryParse<ReportType>(req.ReportType, true, out var rt);

            var metrics = await _dashboardMetricsRepository.GetFirstAsync();
            var currentUserName = GetCurrentUserName();

            var data = new
            {
                TotalShipments = metrics?.TotalShipments ?? 0,
                Delivered = (metrics?.TotalShipments ?? 0) - (metrics?.ActiveShipments ?? 0),
                ActiveShipments = metrics?.ActiveShipments ?? 0,
                GeneratedFrom = req.FromDate,
                GeneratedTo = req.ToDate
            };

            var report = new Report
            {
                Title = $"{req.ReportType} Report ({req.FromDate:d} - {req.ToDate:d})",
                ReportType = rt,
                GeneratedBy = currentUserName,
                FromDate = req.FromDate,
                ToDate = req.ToDate,
                DataJson = JsonSerializer.Serialize(data) // serialize the data object to JSON for storage
            };

            await _reportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Report generated: ID {ReportId} | {Title}", report.Id, report.Title);

            return new ReportDTO(
                report.Id,
                report.Title,
                report.ReportType.ToString(),
                report.FromDate,
                report.ToDate,
                report.GeneratedAt,
                data);
        }
    }
}
