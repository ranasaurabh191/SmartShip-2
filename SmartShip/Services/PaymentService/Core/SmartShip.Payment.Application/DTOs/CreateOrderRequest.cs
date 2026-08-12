using SmartShip.Payment.Domain.Entities.Enums;

namespace SmartShip.Payment.Application.DTOs;

public record CreateOrderRequest(
    int ShipmentId,
    PaymentMethod PaymentMethod
);