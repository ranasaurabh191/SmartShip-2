using SmartShip.Shipment.Domain.Entities;

namespace SmartShip.Shipment.Core.Interfaces.Repositories;

public interface IPackageRepository
{
    Task AddAsync(Package package);
}