using SmartShip.Payment.Domain.Entities;
using SmartShip.Payment.Domain.Entities.Enums;
using Xunit;

namespace SmartShip.Payment.Tests.Entities;

public class ShipmentPaymentEntityTests
{
    [Fact]
    public void ShipmentPayment_Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var shipmentId = 1;
        var customerId = 100;
        var amount = 1500.50m;
        var trackingNumber = "TRACK123ABC";

        // Act
        var payment = new ShipmentPayment
        {
            ShipmentId = shipmentId,
            CustomerId = customerId,
            Amount = amount,
            TrackingNumber = trackingNumber,
            PaymentMethod = PaymentMethod.Online,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.Now
        };

        // Assert
        Assert.Equal(shipmentId, payment.ShipmentId);
        Assert.Equal(customerId, payment.CustomerId);
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(trackingNumber, payment.TrackingNumber);
        Assert.Equal(PaymentMethod.Online, payment.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, payment.PaymentStatus);
    }

    [Fact]
    public void ShipmentPayment_DefaultStatus_ShouldBePending()
    {
        // Arrange & Act
        var payment = new ShipmentPayment();

        // Assert
        Assert.Equal(PaymentStatus.Pending, payment.PaymentStatus);
    }

    [Fact]
    public void ShipmentPayment_DefaultTrackingNumber_ShouldBeEmpty()
    {
        // Arrange & Act
        var payment = new ShipmentPayment();

        // Assert
        Assert.Equal("", payment.TrackingNumber);
    }

    [Fact]
    public void ShipmentPayment_MarkAsPaid_ShouldUpdateStatusAndPaidAt()
    {
        // Arrange
        var payment = new ShipmentPayment
        {
            Id = 1,
            ShipmentId = 1,
            CustomerId = 1,
            Amount = 500.00m,
            PaymentStatus = PaymentStatus.Pending
        };

        var paidAt = DateTime.Now;

        // Act
        payment.PaymentStatus = PaymentStatus.Paid;
        payment.PaidAt = paidAt;

        // Assert
        Assert.Equal(PaymentStatus.Paid, payment.PaymentStatus);
        Assert.Equal(paidAt, payment.PaidAt);
    }

    [Fact]
    public void ShipmentPayment_MarkAsRefunded_ShouldUpdateStatusAndRefundedAt()
    {
        // Arrange
        var payment = new ShipmentPayment
        {
            Id = 1,
            PaymentStatus = PaymentStatus.Paid,
            PaidAt = DateTime.Now.AddDays(-1)
        };

        var refundedAt = DateTime.Now;

        // Act
        payment.PaymentStatus = PaymentStatus.Refunded;
        payment.RefundedAt = refundedAt;

        // Assert
        Assert.Equal(PaymentStatus.Refunded, payment.PaymentStatus);
        Assert.Equal(refundedAt, payment.RefundedAt);
    }

    [Fact]
    public void ShipmentPayment_WithRazorpayDetails_ShouldStoreProperlyWithoutNull()
    {
        // Arrange
        var orderId = "order_1A2B3C";
        var paymentId = "pay_1A2B3C4D";
        var signature = "sig_1A2B3C4D5E6F";

        // Act
        var payment = new ShipmentPayment
        {
            ShipmentId = 1,
            Amount = 500.00m,
            RazorpayOrderId = orderId,
            RazorpayPaymentId = paymentId,
            RazorpaySignature = signature
        };

        // Assert
        Assert.NotNull(payment.RazorpayOrderId);
        Assert.Equal(orderId, payment.RazorpayOrderId);
        Assert.Equal(paymentId, payment.RazorpayPaymentId);
        Assert.Equal(signature, payment.RazorpaySignature);
    }

    [Fact]
    public void ShipmentPayment_CODPayment_ShouldHaveOnlyPaymentMethod()
    {
        // Arrange & Act
        var payment = new ShipmentPayment
        {
            ShipmentId = 1,
            Amount = 500.00m,
            PaymentMethod = PaymentMethod.COD,
            PaymentStatus = PaymentStatus.Pending
        };

        // Assert
        Assert.Equal(PaymentMethod.COD, payment.PaymentMethod);
        Assert.Null(payment.RazorpayOrderId);
        Assert.Null(payment.RazorpayPaymentId);
    }

    [Theory]
    [InlineData(100.00)]
    [InlineData(500.50)]
    [InlineData(10000.00)]
    public void ShipmentPayment_WithValidAmounts_ShouldAcceptAllDecimalValues(decimal amount)
    {
        // Arrange & Act
        var payment = new ShipmentPayment
        {
            ShipmentId = 1,
            Amount = amount
        };

        // Assert
        Assert.Equal(amount, payment.Amount);
    }

    [Fact]
    public void ShipmentPayment_Multiple_ShouldBeDifferentInstances()
    {
        // Arrange & Act
        var payment1 = new ShipmentPayment { Id = 1, ShipmentId = 1 };
        var payment2 = new ShipmentPayment { Id = 2, ShipmentId = 2 };

        // Assert
        Assert.NotEqual(payment1.Id, payment2.Id);
        Assert.NotEqual(payment1.ShipmentId, payment2.ShipmentId);
        Assert.NotSame(payment1, payment2);
    }
}
