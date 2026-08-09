using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartShip.Shared.Events;
using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Core.Interfaces.Persistence;
using SmartShip.Shipment.Core.Interfaces.Repositories;
using SmartShip.Shipment.Core.Interfaces.Services;
using SmartShip.Shipment.Domain.Entities;
using SmartShip.Shipment.Domain.Enums;
using SmartShip.Shipment.Shared.Helpers;
using System.Net.Http.Json;

namespace SmartShip.ShipmentService.Core.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShipmentService> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;

    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IAddressRepository addressRepository,
        IPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        ILogger<ShipmentService> logger,
        IPublishEndpoint publisher,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration config)
    {
        _shipmentRepository = shipmentRepository;
        _addressRepository = addressRepository;
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }


    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId)
    {
        _logger.LogInformation("Creating shipment for Customer {CustomerId} | Type: {Type} | Weight: {Weight}kg",
            customerId, req.ShipmentType, req.Package.WeightKg);

        try
        {
            var customerExists = await ConsumerHelper.ValidateCustomerExistsAsync(
                _httpClientFactory, _logger, customerId, _config);

            if (!customerExists)
            {
                _logger.LogWarning("Shipment creation rejected — Customer {CustomerId} not found or inactive.", customerId);
                throw new KeyNotFoundException($"Customer {customerId} does not exist or is inactive.");
            }

            _logger.LogInformation("Customer {CustomerId} validated. Proceeding with shipment creation.", customerId);

            var sender = MapAddress(req.SenderAddress);
            var receiver = MapAddress(req.ReceiverAddress);
            var package = MapPackage(req.Package);

            var rate = await CalculateRateAsync(req.Package.WeightKg, req.ShipmentType, 0);
            _logger.LogInformation("Calculated shipping rate: {Rate} for Type: {Type} ", rate, req.ShipmentType);

            await _addressRepository.AddRangeAsync(sender, receiver);
            await _packageRepository.AddAsync(package);
            await _unitOfWork.SaveChangesAsync();

            var shipment = new Shipments
            {
                TrackingNumber = GenerateTrackingNumber(),
                CustomerId = customerId,
                ShipmentType = req.ShipmentType,
                Status = ShipmentStatus.Draft,
                ShippingRate = rate,
                SenderAddressId = sender.Id,
                ReceiverAddressId = receiver.Id,
                PackageId = package.Id,
                PickupScheduledAt = req.PickupScheduledAt,
                Notes = req.Notes,
                IsFragile = req.IsFragile,
            };

            shipment.SenderAddress = sender;
            shipment.ReceiverAddress = receiver;
            shipment.Package = package;

            var correlationId = NewId.NextSequentialGuid();

            await _shipmentRepository.AddAsync(shipment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Shipment created: {TrackingNumber} | Rate: {Rate} | Customer: {CustomerId}",
                shipment.TrackingNumber, rate, customerId);

            await _publisher.Publish(new ShipmentCreatedEvent
            {
                ShipmentId = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                CustomerId = shipment.CustomerId,
                SenderCity = sender.City,
                CreatedAt = shipment.CreatedAt,
                Amount = shipment.ShippingRate,
                CorrelationId = correlationId,
                IsFragile = shipment.IsFragile
            });
            _logger.LogInformation("Shipment created Event Published.");

            return MapToResponse(shipment, sender, receiver, package, "Pending");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shipment for Customer {CustomerId}", customerId);
            throw;
        }
    }
    public async Task<ShipmentResponse> GetByIdAsync(int id)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Shipment {id} not found.");

        return MapToResponse(
            shipment,
            shipment.SenderAddress!,
            shipment.ReceiverAddress!,
            shipment.Package!
        );
    }
    public Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type, double distanceKm = 0)
    {
        decimal rate = type switch
        {
            ShipmentType.Express => (decimal)(weightKg * 150),
            ShipmentType.International => (decimal)(weightKg * 300),
            ShipmentType.Freight => (decimal)(weightKg * 50),
            ShipmentType.Domestic => (decimal)(weightKg * 80),
            _ => (decimal)(weightKg * 80)
        };

        const decimal baseDistance = 2000m;
        const decimal flatDistanceSurcharge = 200m;

        if (distanceKm > (double)baseDistance)
        {
            rate += flatDistanceSurcharge;

            _logger.LogInformation("Added flat distance surcharge: {Charge} for shipment over {BaseKm}km",
                flatDistanceSurcharge,
                baseDistance
            );
        }

        var finalRate = Math.Max(rate, 99);

        _logger.LogInformation("Rate calculated: {Rate} | Type: {Type} | Weight: {Weight}kg ",
            finalRate, type, weightKg);

        return Task.FromResult(finalRate);
    }
    public async Task CancelByCustomerAsync(int shipmentId, int customerId, string reason)
    {
        var shipment = await _shipmentRepository.GetByIdAndCustomerAsync(shipmentId, customerId);

        if (shipment == null)
            throw new KeyNotFoundException("Shipment not found.");

        if (shipment.Status != ShipmentStatus.Draft && shipment.Status != ShipmentStatus.Booked)
        {
            throw new InvalidOperationException(
                $"Shipment cannot be cancelled. Current status: {shipment.Status}. Only Draft or Booked shipments can be cancelled.");
        }

        bool wasPaid = shipment.Status == ShipmentStatus.Booked;


        shipment.Status = ShipmentStatus.Cancelled;
        shipment.Notes = $"Cancelled by customer: {reason}";
        shipment.UpdatedAt = DateTime.Now;

        _shipmentRepository.Update(shipment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Shipment {TrackingNumber} cancelled by Customer {CustomerId} | WasPaid: {WasPaid}",
            shipment.TrackingNumber, customerId, wasPaid);

        await _publisher.Publish(new ShipmentCancelledEvent
        {
            ShipmentId = shipment.Id,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = customerId,
            CancelledAt = DateTime.Now
        });

        _logger.LogInformation("ShipmentCancelledByCustomerEvent published for {TrackingNumber}", shipment.TrackingNumber);
    }


    public async Task UpdateStatusAsync(int id, UpdateStatusRequest request)
    {
        _logger.LogInformation("Updating status for Shipment {ShipmentId} -> {Status}", id, request.Status);

        try
        {
            var s = await _shipmentRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Shipment {id} not found.");

            if (!Enum.TryParse<ShipmentStatus>(request.Status, true, out var st))
            {
                _logger.LogWarning("Invalid status value: {Status}", request.Status);
                throw new ArgumentException($"Invalid status: {request.Status}");
            }

            if (st == ShipmentStatus.Cancelled && s.Status == ShipmentStatus.Delivered)
                throw new InvalidOperationException("Cannot cancel a delivered shipment.");

            if (st == ShipmentStatus.InTransit)
                throw new InvalidOperationException("In Transit status is managed automatically when you advance to a hub.");

            if (st == ShipmentStatus.OutForDelivery)
                throw new InvalidOperationException("Out For Delivery status is managed automatically when the final hub is reached.");

            if (st == s.Status)
                return;


            if (st == ShipmentStatus.PickedUp && s.Status != ShipmentStatus.Booked)
                throw new InvalidOperationException($"Shipment must be Booked before PickedUp. Current: {s.Status}");

            if (st == ShipmentStatus.Delivered && s.Status != ShipmentStatus.OutForDelivery)
                throw new InvalidOperationException($"Shipment must be OutForDelivery before Delivered. Current: {s.Status}");

            if (st == ShipmentStatus.Booked && s.PickupScheduledAt == null)
                throw new InvalidOperationException("Cannot book shipment without scheduling pickup first.");

            var oldStatus = s.Status;
            s.Status = st;
            s.UpdatedAt = DateTime.Now;
            if (st == ShipmentStatus.Delivered)
                s.DeliveredAt = DateTime.Now;

            _shipmentRepository.Update(s);
            await _unitOfWork.SaveChangesAsync();


            _logger.LogInformation("Shipment {TrackingNumber} status: {OldStatus} → {NewStatus}",
                s.TrackingNumber, oldStatus, st);

            if (st is not (ShipmentStatus.Booked or ShipmentStatus.Delivered))
            {
                await _publisher.Publish(new ShipmentStatusUpdatedEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = s.Status.ToString(),
                    Location = request.Location ?? "Unknown Hub",
                    UpdatedBy = "Agent-" + DateTime.Now.ToString("hhmm"),
                    UpdatedAt = DateTime.Now,
                    CustomerId = s.CustomerId
                });
            }

            if (s.Status == ShipmentStatus.Delivered)
            {
                _logger.LogInformation("Publishing ShipmentDeliveredEvent for {TrackingNumber}", s.TrackingNumber);

                await _publisher.Publish(new ShipmentDeliveredEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    Location = request.Location ?? "Customer Address",
                    CustomerId = s.CustomerId,
                    DeliveredAt = DateTime.Now
                });
            }

            if (s.Status == ShipmentStatus.Cancelled)
            {
                _logger.LogInformation("Publishing ShipmentCancelledEvent for {TrackingNumber}", s.TrackingNumber);

                await _publisher.Publish(new ShipmentCancelledEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    CancelledAt = DateTime.Now,
                    CustomerId = s.CustomerId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public async Task SchedulePickupAsync(int id, int customerId, SchedulePickupRequest request)
    {
        _logger.LogInformation("Scheduling pickup for Shipment {ShipmentId} | Customer {CustomerId}", id, customerId);

        try
        {
            var s = await _shipmentRepository.GetByIdAndCustomerAsync(id, customerId);

            if (s == null)
            {
                _logger.LogWarning("Shipment {ShipmentId} not found or does not belong to Customer {CustomerId}",
                    id, customerId);
                throw new KeyNotFoundException("Shipment not found or you are not authorized to schedule pickup for it.");
            }

            if (s.Status != ShipmentStatus.Draft && s.Status != ShipmentStatus.PaymentFailed)
                throw new InvalidOperationException(
                    $"Pickup can only be scheduled for Draft or PaymentFailed shipments. Current status: {s.Status}.");

            s.PickupScheduledAt = request.PickupTime;
            s.Status = ShipmentStatus.Booked;
            s.UpdatedAt = request.PickupTime;

            _shipmentRepository.Update(s);
            await _unitOfWork.SaveChangesAsync();

            await _publisher.Publish(new ShipmentStatusUpdatedEvent
            {
                ShipmentId = s.Id,
                TrackingNumber = s.TrackingNumber,
                OldStatus = "Draft",
                NewStatus = "Booked",
                Location = s.SenderAddress?.City ?? "Warehouse",
                UpdatedBy = "system",
                UpdatedAt = DateTime.Now,
                CustomerId = s.CustomerId
            });

            _logger.LogInformation("Pickup scheduled for {TrackingNumber} at {PickupTime} | Customer {CustomerId}",
                s.TrackingNumber, request.PickupTime, customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule pickup for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public async Task<ShipmentResponse?> GetByTrackingNumberAsync(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return null;

        var shipment = await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber);

        if (shipment == null)
            return null;

        return MapToResponse(
            shipment,
            shipment.SenderAddress!,
            shipment.ReceiverAddress!,
            shipment.Package!
        );
    }

    private Address MapAddress(AddressDto dto)
    {
        return new Address
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            Street = dto.Street,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country
        };
    }

    private Package MapPackage(PackageDto dto)
    {
        return new Package
        {
            WeightKg = dto.WeightKg,
            LengthCm = dto.LengthCm,
            WidthCm = dto.WidthCm,
            HeightCm = dto.HeightCm,
            Description = dto.Description,
            DeclaredValue = dto.DeclaredValue
        };
    }

    private string GenerateTrackingNumber()
    {
        const string prefix = "SHP";
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{timestamp}-{random}";
    }

    private ShipmentResponse MapToResponse(Shipments shipment, Address sender, Address receiver, Package package, string? paymentStatus = null)
    {
        return new ShipmentResponse(
            Id: shipment.Id,
            TrackingNumber: shipment.TrackingNumber,
            CustomerId: shipment.CustomerId,
            ShipmentType: shipment.ShipmentType.ToString(),
            Status: shipment.Status.ToString(),
            PaymentStatus: paymentStatus ?? "Pending",
            ShippingRate: shipment.ShippingRate,
            CreatedAt: shipment.CreatedAt.ToString("O"),
            PickupScheduledAt: shipment.PickupScheduledAt?.ToString("O"),
            DeliveredAt: shipment.DeliveredAt?.ToString("O"),
            SenderAddress: new AddressDto(
                sender.FullName, sender.Phone, sender.Street, sender.City,
                sender.State, sender.PostalCode, sender.Country
            ),
            ReceiverAddress: new AddressDto(
                receiver.FullName, receiver.Phone, receiver.Street, receiver.City,
                receiver.State, receiver.PostalCode, receiver.Country
            ),
            Package: new PackageDto(
                package.WeightKg, package.LengthCm, package.WidthCm, package.HeightCm,
                package.Description, package.DeclaredValue
            ),
            Notes: shipment.Notes,
            IsFragile: shipment.IsFragile,
            DistanceKm: 0
        );
    }

}