
namespace SmartShip.Admin.Application.DTOs
{
    public record UpdateHubRequest(
        string Name, 
        string City, 
        string State, 
        string Country, 
        string ContactPhone, 
        bool IsActive
        );
}
