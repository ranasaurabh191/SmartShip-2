namespace SmartShip.Shipment.Domain.Enums
{
    public enum ShipmentStatus
    {
        Draft, Booked, PickedUp, InTransit, OutForDelivery, Delivered,
        Delayed, Failed, Returned, Cancelled, PaymentFailed
    }
}
