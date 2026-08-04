
namespace SmartShip.Admin.Application.DTOs
{
    public record HubDto(
        int Id,
        string Name, 
        string City, 
        string State, 
        string Country, 
        string ContactPhone, 
        bool IsActive);

}
