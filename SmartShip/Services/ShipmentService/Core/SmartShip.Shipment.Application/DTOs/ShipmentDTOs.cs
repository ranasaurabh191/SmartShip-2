using SmartShip.Shipment.Domain.Enums;

namespace SmartShip.Shipment.Core.DTOs;

public record AddressDto(
    string FullName, 
    string Phone, 
    string Street, 
    string City, 
    string State, 
    string PostalCode, 
    string Country
    );
public record PackageDto(
    double WeightKg, 
    double LengthCm, 
    double WidthCm, 
    double HeightCm, 
    string Description
    );

public record CreateShipmentRequest(
    AddressDto SenderAddress,
    AddressDto ReceiverAddress,
    PackageDto Package,
    ShipmentType ShipmentType,
    string? Notes,
    bool IsFragile = false
);

public record ShipmentResponse(
    int Id, 
    string TrackingNumber, 
    int CustomerId,
    string ShipmentType, 
    string Status, 
    string PaymentStatus, 
    decimal ShippingRate,
    string CreatedAt, 
    string? PickupScheduledAt, 
    string? DeliveredAt,
    AddressDto SenderAddress, 
    AddressDto ReceiverAddress, 
    PackageDto Package, 
    string? Notes,
    bool IsFragile
);
