using SmartShip.Shipment.Domain.Entities;
using SmartShip.Shipment.Domain.Enums;
using Xunit;

namespace SmartShip.Shipment.Tests.Entities;

public class ShipmentsEntityTests
{
    [Fact]
    public void Shipments_Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var customerId = 1;
        var trackingNumber = "SHIP123ABC";
        var shippingRate = 500.00m;

        // Act
        var shipment = new Shipments
        {
            CustomerId = customerId,
            TrackingNumber = trackingNumber,
            ShipmentType = ShipmentType.Domestic,
            Status = ShipmentStatus.Draft,
            PaymentMethod = PaymentMethod.Online,
            ShippingRate = shippingRate
        };

        // Assert
        Assert.Equal(customerId, shipment.CustomerId);
        Assert.Equal(trackingNumber, shipment.TrackingNumber);
        Assert.Equal(ShipmentType.Domestic, shipment.ShipmentType);
        Assert.Equal(ShipmentStatus.Draft, shipment.Status);
        Assert.Equal(shippingRate, shipment.ShippingRate);
    }

    [Fact]
    public void Shipments_DefaultStatus_ShouldBeDraft()
    {
        // Arrange & Act
        var shipment = new Shipments();

        // Assert
        Assert.Equal(ShipmentStatus.Draft, shipment.Status);
    }

    [Fact]
    public void Shipments_DefaultPaymentMethod_ShouldBeOnline()
    {
        // Arrange & Act
        var shipment = new Shipments();

        // Assert
        Assert.Equal(PaymentMethod.Online, shipment.PaymentMethod);
    }

    [Fact]
    public void Shipments_MarkAsPickedUp_ShouldUpdateStatus()
    {
        // Arrange
        var shipment = new Shipments { Status = ShipmentStatus.Draft };
        var scheduledAt = DateTime.Now.AddHours(2);

        // Act
        shipment.Status = ShipmentStatus.PickedUp;
        shipment.PickupScheduledAt = scheduledAt;

        // Assert
        Assert.Equal(ShipmentStatus.PickedUp, shipment.Status);
        Assert.Equal(scheduledAt, shipment.PickupScheduledAt);
    }

    [Fact]
    public void Shipments_MarkAsDelivered_ShouldUpdateStatusAndDeliveredAt()
    {
        // Arrange
        var shipment = new Shipments { Status = ShipmentStatus.InTransit };
        var deliveredAt = DateTime.Now;

        // Act
        shipment.Status = ShipmentStatus.Delivered;
        shipment.DeliveredAt = deliveredAt;

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(deliveredAt, shipment.DeliveredAt);
    }

    [Fact]
    public void Shipments_FragilePackage_ShouldBeMarked()
    {
        // Arrange & Act
        var shipment = new Shipments { IsFragile = true };

        // Assert
        Assert.True(shipment.IsFragile);
    }

    [Fact]
    public void Shipments_WithNotes_ShouldStoreNotes()
    {
        // Arrange
        var notes = "Handle with care - contains electronics";

        // Act
        var shipment = new Shipments { Notes = notes };

        // Assert
        Assert.Equal(notes, shipment.Notes);
    }

    [Theory]
    [InlineData(10.5)]
    [InlineData(100.0)]
    [InlineData(500.75)]
    public void Shipments_WithDistance_ShouldAcceptValidDistanceValues(double distance)
    {
        // Arrange & Act
        var shipment = new Shipments { DistanceKm = distance };

        // Assert
        Assert.Equal(distance, shipment.DistanceKm);
    }

    [Theory]
    [InlineData(ShipmentType.Domestic)]
    [InlineData(ShipmentType.International)]
    public void Shipments_WithShipmentTypes_ShouldHandleAllTypes(ShipmentType type)
    {
        // Arrange & Act
        var shipment = new Shipments { ShipmentType = type };

        // Assert
        Assert.Equal(type, shipment.ShipmentType);
    }
}

public class AddressEntityTests
{
    [Fact]
    public void Address_Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var fullName = "John Doe";
        var phone = "9876543210";
        var city = "New York";
        var postalCode = "10001";

        // Act
        var address = new Address
        {
            FullName = fullName,
            Phone = phone,
            Street = "123 Main St",
            City = city,
            State = "NY",
            PostalCode = postalCode,
            Country = "USA"
        };

        // Assert
        Assert.Equal(fullName, address.FullName);
        Assert.Equal(phone, address.Phone);
        Assert.Equal(city, address.City);
        Assert.Equal(postalCode, address.PostalCode);
    }

    [Fact]
    public void Address_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var address = new Address();

        // Assert
        Assert.Equal(string.Empty, address.FullName);
        Assert.Equal(string.Empty, address.Phone);
        Assert.Equal(string.Empty, address.City);
    }

    [Fact]
    public void Address_WithCompleteInfo_ShouldStoreAll()
    {
        // Arrange
        var address = new Address
        {
            FullName = "Jane Smith",
            Phone = "9123456789",
            Street = "456 Oak Ave",
            City = "Los Angeles",
            State = "CA",
            PostalCode = "90001",
            Country = "USA"
        };

        // Act
        var isValid = !string.IsNullOrEmpty(address.FullName) 
                   && !string.IsNullOrEmpty(address.Phone)
                   && !string.IsNullOrEmpty(address.City);

        // Assert
        Assert.True(isValid);
        Assert.NotEmpty(address.FullName);
        Assert.NotEmpty(address.Phone);
    }

    [Theory]
    [InlineData("USA")]
    [InlineData("Canada")]
    [InlineData("Mexico")]
    public void Address_WithDifferentCountries_ShouldBeAccepted(string country)
    {
        // Arrange & Act
        var address = new Address { Country = country };

        // Assert
        Assert.Equal(country, address.Country);
    }
}
