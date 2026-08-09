using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Core.Interfaces.Services;
using SmartShip.Shipment.Domain.Enums;
using SmartShip.Shipment.Infrastructure.Context;
using System.Security.Claims;

namespace SmartShip.ShipmentService.API.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _service;
    private readonly ShipmentDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<ShipmentsController> _logger;

    public ShipmentsController(IShipmentService service, ShipmentDbContext context, IConfiguration config, ILogger<ShipmentsController> logger)
    {
        _service = service;
        _context = context; 
        _config = config;
        _logger = logger;
    }
    
    private int GetUserId() 
    {
        var claim = User.FindFirst("userId")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(claim ?? "0");
    }

    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request)
    {
        var result = await _service.CreateAsync(request, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id) =>  Ok(await _service.GetByIdAsync(id));

    [HttpPost("{id}/schedule-pickup")]
    [Authorize(Roles = "CUSTOMER")]  
    public async Task<IActionResult> SchedulePickup(int id, [FromBody] SchedulePickupRequest request)
    {
        var customerIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            _logger.LogWarning("JWT userId claim missing or invalid on SchedulePickup.");
            return Unauthorized(new { message = "Invalid or missing user token." });
        }

        await _service.SchedulePickupAsync(id, customerId, request);  
        return Ok(new { message = "Pickup scheduled successfully." });
    }

    [HttpGet("rate")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetRate([FromQuery] double weight, [FromQuery] string type)
    {
        if (!Enum.TryParse<ShipmentType>(type, true, out var shipType)) return BadRequest("Invalid type");
        var rate = await _service.CalculateRateAsync(weight, shipType);
        return Ok(new { rate });
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> CancelShipment(int id, [FromBody] CancelShipmentRequest request)
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var customerId))
            return Unauthorized("Invalid token.");

        await _service.CancelByCustomerAsync(id, customerId, request.Reason);
        return Ok(new { message = "Shipment cancelled successfully." });

    }
    [HttpGet("by-tracking/{trackingNumber}")]
    [Authorize]
    public async Task<IActionResult> GetByTrackingNumber(string trackingNumber)
    {
        var shipment = await _service.GetByTrackingNumberAsync(trackingNumber);
        if (shipment == null) return NotFound();
        return Ok(shipment);
    }
   
}