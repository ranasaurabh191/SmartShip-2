
// Defines request and response DTOs used by the Identity module for authentication,
// user registration, profile management, and administrative user operations.
// API controls exactly what the client receives.
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
