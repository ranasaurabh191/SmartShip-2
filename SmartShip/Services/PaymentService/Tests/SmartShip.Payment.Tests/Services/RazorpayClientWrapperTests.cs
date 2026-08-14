using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SmartShip.Payment.Application.Services;
using SmartShip.Payment.Domain.Entities;
using Xunit;

namespace SmartShip.Payment.Tests.Services;

public class RazorpayClientWrapperTests
{
    private const string Secret = "TestSecretKey123456789";

    private RazorpayClientWrapper CreateClient()
    {
        var settings = Options.Create(new RazorpaySettings
        {
            KeyId = "test_key_id",
            KeySecret = Secret
        });

        return new RazorpayClientWrapper(settings);
    }

    private static string GenerateSignature(
        string orderId,
        string paymentId,
        string secret)
    {
        var payload = $"{orderId}|{paymentId}";

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(secret));

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(payload));

        return BitConverter
            .ToString(hash)
            .Replace("-", "")
            .ToLower();
    }

    [Fact]
    public void VerifySignature_WhenSignatureIsCorrect_ShouldReturnTrue()
    {
        var orderId = "order_123";
        var paymentId = "pay_123";

        var signature = GenerateSignature(
            orderId,
            paymentId,
            Secret);

        var client = CreateClient();

        var result = client.VerifySignature(
            orderId,
            paymentId,
            signature);

        Assert.True(result);
    }

    [Fact]
    public void VerifySignature_WhenSignatureIsIncorrect_ShouldReturnFalse()
    {
        var client = CreateClient();

        var result = client.VerifySignature(
            "order_123",
            "pay_123",
            "invalid_signature");

        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_WhenPaymentIdChanges_ShouldReturnFalse()
    {
        var signature = GenerateSignature(
            "order_123",
            "pay_123",
            Secret);

        var client = CreateClient();

        var result = client.VerifySignature(
            "order_123",
            "different_payment",
            signature);

        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_WhenOrderIdChanges_ShouldReturnFalse()
    {
        var signature = GenerateSignature(
            "order_123",
            "pay_123",
            Secret);

        var client = CreateClient();

        var result = client.VerifySignature(
            "different_order",
            "pay_123",
            signature);

        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_WhenSignatureIsEmpty_ShouldReturnFalse()
    {
        var client = CreateClient();

        var result = client.VerifySignature(
            "order_123",
            "pay_123",
            "");

        Assert.False(result);
    }
}