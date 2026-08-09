using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Core.Interfaces.Services;


namespace SmartShip.Shipment.API.Controllers;

[ApiController]
[Route("api/admin/shipments")]
[Authorize(Roles = "ADMIN")]
public class AdminShipmentsController : ControllerBase
{
    private readonly IShipmentService _service;
    public AdminShipmentsController(IShipmentService service) => _service = service;


    [HttpPut("status/{id}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        await _service.UpdateStatusAsync(id, request);      
        return Ok(new { message = "Status updated successfully." });
    }

}
