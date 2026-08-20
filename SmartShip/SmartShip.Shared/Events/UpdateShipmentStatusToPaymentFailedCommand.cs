using System;

namespace SmartShip.Shared.Events
{
    /// <summary>
    /// Compensating command sent from PaymentService to ShipmentService when payment authorization or capture fails,
    /// instructing ShipmentService to transition the shipment status to PAYMENT_FAILED.
    /// </summary>
    public class UpdateShipmentStatusToPaymentFailedCommand
    {
        /// <summary>
        /// Gets or sets the unique database identifier of the affected shipment.
        /// </summary>
        public int ShipmentId { get; set; }

        /// <summary>
        /// Gets or sets the tracking number of the shipment.
        /// </summary>
        public string TrackingNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for the payment failure.
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
