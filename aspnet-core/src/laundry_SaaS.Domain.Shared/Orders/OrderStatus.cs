namespace laundry_SaaS.Orders;

public enum OrderStatus
{
    Draft = 0,
    PendingPickup = 1,
    PickupAssigned = 2,
    OutForPickup = 3,
    PickedUp = 4,
    ReceivedAtLaundry = 5,
    Inspection = 6,
    WaitingForAdjustmentApproval = 7,
    Processing = 8,
    ReadyForDelivery = 9,
    OutForDelivery = 10,
    Delivered = 11,
    Completed = 12,
    Cancelled = 13,
    OnHold = 14,
    PickupFailed = 15,
    DeliveryFailed = 16
}
