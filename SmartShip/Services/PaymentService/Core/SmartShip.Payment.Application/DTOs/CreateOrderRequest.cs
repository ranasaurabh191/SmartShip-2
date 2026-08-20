using SmartShip.Payment.Domain.Entities.Enums;

namespace SmartShip.Payment.Application.DTOs;

/// <summary>
/// Request record for initiating a payment transaction or Razorpay order for a shipment.
/// </summary>

public record CreateOrderRequest(
    int ShipmentId,
    PaymentMethod PaymentMethod
);