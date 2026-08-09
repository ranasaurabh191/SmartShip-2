using SmartShip.Shipment.Domain.Entities;

namespace SmartShip.Shipment.Core.Interfaces.Repositories;

public interface IAddressRepository
{
    Task AddRangeAsync(params Address[] addresses);
}