
namespace SmartShip.Admin.Application.DTOs
{
    public record HubDTO(
        int Id,
        string Name, 
        string City, 
        string State, 
        string Country, 
        string ContactPhone, 
        bool IsActive);

}
