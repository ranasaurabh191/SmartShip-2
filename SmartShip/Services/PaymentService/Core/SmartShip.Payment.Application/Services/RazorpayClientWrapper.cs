using Microsoft.Extensions.Options;
using Razorpay.Api;
using SmartShip.Payment.Application.Interfaces.Services;
using SmartShip.Payment.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SmartShip.Payment.Application.Services;

public class RazorpayClientWrapper : IRazorpayClient
{
    private readonly RazorpaySettings _settings;

    public RazorpayClientWrapper(IOptions<RazorpaySettings> options)
    {
        _settings = options.Value;
    }

    public string CreateOrder(decimal amount, int shipmentId)
    {
        var client = new RazorpayClient(
            _settings.KeyId,
            _settings.KeySecret);

        var options = new Dictionary<string, object>
        {
            { "amount", (int)(amount * 100) },
            { "currency", "INR" },
            { "receipt", $"receipt_shipment_{shipmentId}" },
            { "payment_capture", 1 }
        };

        var order = client.Order.Create(options);

        return order["id"].ToString();
    }

    public bool VerifySignature(
        string orderId,
        string paymentId,
        string signature)
    {
        var payload = $"{orderId}|{paymentId}";

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_settings.KeySecret));

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(payload));

        var generated = Convert.ToHexString(hash).ToLower();

        return generated == signature;
    }

    public string GenerateDemoPaymentId()
    {
        return $"pay_demo_{Guid.NewGuid():N}";
    }

    public string GenerateDemoSignature(
        string orderId,
        string paymentId)
    {
        var payload = $"{orderId}|{paymentId}";

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_settings.KeySecret));

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash).ToLower();
    }
}