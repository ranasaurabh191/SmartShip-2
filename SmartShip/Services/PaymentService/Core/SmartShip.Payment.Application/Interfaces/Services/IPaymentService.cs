using SmartShip.Payment.Application.DTOs;

namespace SmartShip.Payment.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<PaymentResponse> VerifyPaymentAsync(VerifyPaymentRequest request);
    Task<PaymentResponse> GetByShipmentIdAsync(int shipmentId);
    Task<PaymentResponse> PaymentStatusAsync(PaymentStatusRequest request);
    Task<List<PaymentResponse>> GetMyPaymentsAsync();
    Task<List<PaymentResponse>> GetAllPaymentsAsync();
}