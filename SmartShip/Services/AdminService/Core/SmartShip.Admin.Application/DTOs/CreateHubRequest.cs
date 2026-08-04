

namespace SmartShip.Admin.Application.DTOs
{
    public record CreateHubRequest(
        string Name, 
        string City, 
        string State, 
        string Country, 
        double Latitude, 
        double Longitude, 
        string ContactPhone
        );

}
