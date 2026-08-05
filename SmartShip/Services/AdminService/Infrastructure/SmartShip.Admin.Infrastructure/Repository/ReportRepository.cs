using SmartShip.Admin.Domain.Entities;
using SmartShip.Admin.Infrastructure.Context;

namespace SmartShip.Admin.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AdminDbContext _context;

    public ReportRepository(AdminDbContext context)
    {
        _context = context;// Store injected DbContext into private field for later DB operations
    }

    public async Task<Report> AddAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
        return report;
    }
}