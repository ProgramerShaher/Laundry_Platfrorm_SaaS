namespace laundry_SaaS.PickupDelivery;

public enum PickupTaskStatus
{
    Assigned = 0,
    Accepted = 1,
    OutForPickup = 2,
    Arrived = 3,
    PickedUp = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7
}
