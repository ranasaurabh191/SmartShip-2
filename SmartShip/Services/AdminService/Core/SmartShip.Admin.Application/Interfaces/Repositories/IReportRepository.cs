using SmartShip.Admin.Domain.Entities;
public interface IReportRepository
{
    Task<Report> AddAsync(Report report);
}