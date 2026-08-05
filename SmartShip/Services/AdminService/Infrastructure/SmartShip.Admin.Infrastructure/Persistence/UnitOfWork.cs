using SmartShip.Admin.Infrastructure.Context;

namespace SmartShip.Admin.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AdminDbContext _context;// Making it readonly guarantees the reference never changes after construction.
        public UnitOfWork(AdminDbContext context) // Constructor/dependency injection of AdminDbContext
        {
            _context = context;
        }
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) // Asynchronously save all changes to the database
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
