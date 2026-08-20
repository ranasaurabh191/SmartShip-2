namespace SmartShip.Payment.Domain.Entities
{
    /// <summary>
    /// Configuration options entity containing API keys for connecting to Razorpay payment gateway.
    /// </summary>
    public class RazorpaySettings
    {

        public string KeyId { get; set; } = string.Empty;
        public string KeySecret { get; set; } = string.Empty;
    }
}
