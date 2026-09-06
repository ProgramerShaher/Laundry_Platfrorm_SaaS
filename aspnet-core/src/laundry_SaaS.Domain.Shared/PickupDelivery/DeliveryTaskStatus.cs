namespace laundry_SaaS.PickupDelivery;

public enum DeliveryTaskStatus
{
    Assigned = 0,
    Accepted = 1,
    PickedUpFromLaundry = 2,
    OutForDelivery = 3,
    Arrived = 4,
    Delivered = 5,
    Failed = 6,
    Cancelled = 7
}
