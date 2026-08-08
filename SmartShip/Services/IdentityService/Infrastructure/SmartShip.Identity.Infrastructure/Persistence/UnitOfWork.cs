using SmartShip.Identity.Application.Interfaces.Persistence;
using SmartShip.Identity.Infrastructure.Data;

namespace SmartShip.Identity.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

}