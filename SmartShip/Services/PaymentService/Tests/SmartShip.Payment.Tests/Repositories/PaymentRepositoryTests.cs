using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartShip.Payment.Domain.Entities;
using SmartShip.Payment.Domain.Entities.Enums;
using SmartShip.Payment.Infrastructure.Context;
using SmartShip.Payment.Infrastructure.Repositories;
using Xunit;

namespace SmartShip.Payment.Tests.Repositories;

public class PaymentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PaymentDbContext _context;
    private readonly PaymentRepository _repository;

    public PaymentRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PaymentDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new PaymentRepository(_context);
    }

    private ShipmentPayment CreatePayment(
        int id,
        int shipmentId,
        int customerId,
        string trackingNumber,
        string? orderId,
        PaymentStatus status = PaymentStatus.Pending)
    {
        return new ShipmentPayment
        {
            Id = id,
            ShipmentId = shipmentId,
            CustomerId = customerId,
            TrackingNumber = trackingNumber,
            Amount = 1000m,
            PaymentMethod = PaymentMethod.Online,
            PaymentStatus = status,
            RazorpayOrderId = orderId,
            CreatedAt = DateTime.Now
        };
    }

    [Fact]
    public async Task GetByShipmentIdAsync_WhenPaymentExists_ShouldReturnPayment()
    {
        var payment = CreatePayment(
            1,
            100,
            10,
            "TRK100",
            "order100");

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByShipmentIdAsync(100);

        Assert.NotNull(result);
        Assert.Equal(100, result.ShipmentId);
        Assert.Equal("TRK100", result.TrackingNumber);
    }

    [Fact]
    public async Task GetByShipmentIdAsync_WhenPaymentDoesNotExist_ShouldReturnNull()
    {
        var result = await _repository.GetByShipmentIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrderAndShipmentAsync_ShouldReturnMatchingPayment()
    {
        var payment = CreatePayment(
            1,
            100,
            10,
            "TRK100",
            "order100");

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByOrderAndShipmentAsync(
            "order100",
            100);

        Assert.NotNull(result);
        Assert.Equal("order100", result.RazorpayOrderId);
        Assert.Equal(100, result.ShipmentId);
    }

    [Fact]
    public async Task GetByOrderAndShipmentAsync_WhenNoMatch_ShouldReturnNull()
    {
        var result = await _repository.GetByOrderAndShipmentAsync(
            "missing",
            999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrderIdAsync_ShouldReturnPayment()
    {
        var payment = CreatePayment(
            1,
            100,
            10,
            "TRK100",
            "order100");

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByOrderIdAsync("order100");

        Assert.NotNull(result);
        Assert.Equal("order100", result.RazorpayOrderId);
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_ShouldReturnPayment()
    {
        var payment = CreatePayment(
            1,
            100,
            10,
            "TRK100",
            "order100");

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByTrackingNumberAsync("TRK100");

        Assert.NotNull(result);
        Assert.Equal("TRK100", result.TrackingNumber);
    }

    [Fact]
    public async Task AddAsync_ShouldAddPayment()
    {
        var payment = CreatePayment(
            1,
            100,
            10,
            "TRK100",
            "order100");

        await _repository.AddAsync(payment);
        await _context.SaveChangesAsync();

        var result = await _context.Payments.FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal(100, result.ShipmentId);
    }

    [Fact]
    public async Task Update_ShouldUpdatePayment()
    {
        var payment = CreatePayment(
            1,
            100,
            10,
            "TRK100",
            "order100");

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        payment.PaymentStatus = PaymentStatus.Paid;

        _repository.Update(payment);
        await _context.SaveChangesAsync();

        var result = await _context.Payments.FindAsync(1);

        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Paid, result.PaymentStatus);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnOnlyCustomerPayments()
    {
        await _context.Payments.AddRangeAsync(
            CreatePayment(1, 100, 10, "TRK100", "order100"),
            CreatePayment(2, 101, 10, "TRK101", "order101"),
            CreatePayment(3, 102, 20, "TRK102", "order102"));

        await _context.SaveChangesAsync();

        var result = await _repository.GetByCustomerIdAsync(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal(10, x.CustomerId));
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldOrderByCreatedAtDescending()
    {
        var oldPayment = CreatePayment(
            1,
            100,
            10,
            "OLD",
            "order_old");

        oldPayment.CreatedAt = DateTime.Now.AddDays(-2);

        var newPayment = CreatePayment(
            2,
            101,
            10,
            "NEW",
            "order_new");

        newPayment.CreatedAt = DateTime.Now;

        await _context.Payments.AddRangeAsync(
            oldPayment,
            newPayment);

        await _context.SaveChangesAsync();

        var result = await _repository.GetByCustomerIdAsync(10);

        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPayments()
    {
        await _context.Payments.AddRangeAsync(
            CreatePayment(1, 100, 10, "TRK100", "order100"),
            CreatePayment(2, 101, 20, "TRK101", "order101"),
            CreatePayment(3, 102, 30, "TRK102", "order102"));

        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderByCreatedAtDescending()
    {
        var first = CreatePayment(
            1,
            100,
            10,
            "FIRST",
            "order_first");

        first.CreatedAt = DateTime.Now.AddDays(-3);

        var second = CreatePayment(
            2,
            101,
            20,
            "SECOND",
            "order_second");

        second.CreatedAt = DateTime.Now;

        await _context.Payments.AddRangeAsync(first, second);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}