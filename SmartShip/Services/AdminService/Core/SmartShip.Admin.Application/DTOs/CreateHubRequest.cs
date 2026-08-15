

namespace SmartShip.Admin.Application.DTOs
{
    public record CreateHubRequest(
        string Name, 
        string City, 
        string State, 
        string Country, 
        string ContactPhone
        );

}
