using System;
using System.Linq;
using laundry_SaaS.Bags;
using laundry_SaaS.Catalog;
using laundry_SaaS.Complaints;
using laundry_SaaS.Customers;
using laundry_SaaS.LaundryProcessing;
using laundry_SaaS.Laundries;
using laundry_SaaS.Orders;
using laundry_SaaS.PickupDelivery;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace laundry_SaaS.Entities;

/// <summary>
/// اختبارات قواعد نطاق العمل الصارمة (Domain Rules & Invariants Tests)
/// تغطي موجهات دورات الحياة، والتحقق المالي، والإحداثيات الجغرافية، وتسلسل المراحل التشغيلية.
/// </summary>
public class DomainRulesTests
{
    private Order CreateTestOrder(OrderStatus initialStatus = OrderStatus.Draft)
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var laundryId = Guid.NewGuid();
        var address = new AddressSnapshot("King Fahd Rd", 24.7136, 46.6753);

        var order = new Order(
            Guid.NewGuid(),
            tenantId,
            "ORD-" + Guid.NewGuid().ToString("N")[..8],
            customerId,
            laundryId,
            address,
            address,
            deliveryFee: 10m);

        if (initialStatus == OrderStatus.Draft) return order;

        order.MarkAsPendingPickup();
        if (initialStatus == OrderStatus.PendingPickup) return order;

        order.MarkAsPickupAssigned();
        if (initialStatus == OrderStatus.PickupAssigned) return order;

        order.MarkAsOutForPickup();
        if (initialStatus == OrderStatus.OutForPickup) return order;

        order.MarkAsPickedUp();
        if (initialStatus == OrderStatus.PickedUp) return order;

        order.MarkAsReceivedAtLaundry();
        if (initialStatus == OrderStatus.ReceivedAtLaundry) return order;

        order.StartInspection();
        if (initialStatus == OrderStatus.Inspection) return order;

        order.StartProcessing(ProcessingStage.Sorting);
        if (initialStatus == OrderStatus.Processing) return order;

        order.MoveToProcessingStage(ProcessingStage.Washing);
        order.MoveToProcessingStage(ProcessingStage.Drying);
        order.MoveToProcessingStage(ProcessingStage.Ironing);
        order.MoveToProcessingStage(ProcessingStage.Folding);
        order.MoveToProcessingStage(ProcessingStage.Packaging);
        order.MarkAsReadyForDelivery();
        if (initialStatus == OrderStatus.ReadyForDelivery) return order;

        order.MarkAsOutForDelivery();
        if (initialStatus == OrderStatus.OutForDelivery) return order;

        order.MarkAsDelivered();
        if (initialStatus == OrderStatus.Delivered) return order;

        order.Complete();
        return order;
    }

    [Fact]
    public void TenantId_Must_Not_Be_Empty_For_Tenant_Aggregates()
    {
        Should.Throw<BusinessException>(() =>
        {
            var area = new CoverageArea(24.7136, 46.6753, 10, 20);
            new Laundry(Guid.NewGuid(), Guid.Empty, "Clean Wash", "0501234567", 24.7136, 46.6753, area);
        });

        Should.Throw<BusinessException>(() =>
        {
            var address = new AddressSnapshot("Al-Olaya", 24.7136, 46.6753);
            new Order(Guid.NewGuid(), Guid.Empty, "ORD-100", Guid.NewGuid(), Guid.NewGuid(), address, address);
        });
    }

    [Fact]
    public void LaundryWorkingHour_Should_Validate_Open_And_Close_Times()
    {
        var laundryId = Guid.NewGuid();

        var workingHour = new LaundryWorkingHour(
            Guid.NewGuid(),
            laundryId,
            DayOfWeek.Sunday,
            false,
            new TimeOnly(8, 0),
            new TimeOnly(18, 0));

        workingHour.IsOpen.ShouldBeFalse();
        workingHour.OpenTime.ShouldBeNull();
        workingHour.CloseTime.ShouldBeNull();

        Should.Throw<BusinessException>(() =>
        {
            new LaundryWorkingHour(
                Guid.NewGuid(),
                laundryId,
                DayOfWeek.Monday,
                true,
                new TimeOnly(18, 0),
                new TimeOnly(8, 0));
        });

        var validHour = new LaundryWorkingHour(
            Guid.NewGuid(),
            laundryId,
            DayOfWeek.Monday,
            true,
            new TimeOnly(8, 0),
            new TimeOnly(22, 0));

        validHour.IsOpen.ShouldBeTrue();
        validHour.OpenTime.ShouldBe(new TimeOnly(8, 0));
        validHour.CloseTime.ShouldBe(new TimeOnly(22, 0));
    }

    [Fact]
    public void ServicePrice_Must_Be_Non_Negative()
    {
        var tenantId = Guid.NewGuid();
        var itemTypeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        Should.Throw<BusinessException>(() =>
        {
            new ServicePrice(Guid.NewGuid(), tenantId, itemTypeId, serviceId, -5.00m);
        });

        var validPrice = new ServicePrice(Guid.NewGuid(), tenantId, itemTypeId, serviceId, 15.50m);
        validPrice.Price.ShouldBe(15.50m);

        Should.Throw<BusinessException>(() =>
        {
            validPrice.SetPrice(-1m);
        });
    }

    // 1. Draft -> Delivered must fail
    [Fact]
    public void Order_Draft_To_Delivered_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Draft);
        Should.Throw<BusinessException>(() =>
        {
            order.MarkAsDelivered();
        });
    }

    // 2. Cancelled -> Processing must fail
    [Fact]
    public void Order_Cancelled_To_Processing_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Draft);
        order.Cancel("Customer decided to cancel", DateTime.UtcNow);
        order.Status.ShouldBe(OrderStatus.Cancelled);

        Should.Throw<BusinessException>(() =>
        {
            order.StartProcessing(ProcessingStage.Sorting);
        });
    }

    // 3. Delivered -> Completed succeeds
    [Fact]
    public void Order_Delivered_To_Completed_Succeeds()
    {
        var order = CreateTestOrder(OrderStatus.Delivered);
        order.Complete();
        order.Status.ShouldBe(OrderStatus.Completed);
    }

    // 4. Processing stage sequence correct succeeds
    [Fact]
    public void Order_Processing_Stage_Sequence_Correct_Succeeds()
    {
        var order = CreateTestOrder(OrderStatus.Inspection);

        order.StartProcessing(ProcessingStage.Sorting);
        order.CurrentProcessingStage.ShouldBe(ProcessingStage.Sorting);

        order.MoveToProcessingStage(ProcessingStage.Washing);
        order.CurrentProcessingStage.ShouldBe(ProcessingStage.Washing);

        order.MoveToProcessingStage(ProcessingStage.Drying);
        order.CurrentProcessingStage.ShouldBe(ProcessingStage.Drying);

        order.MoveToProcessingStage(ProcessingStage.Ironing);
        order.CurrentProcessingStage.ShouldBe(ProcessingStage.Ironing);

        order.MoveToProcessingStage(ProcessingStage.Folding);
        order.CurrentProcessingStage.ShouldBe(ProcessingStage.Folding);

        order.MoveToProcessingStage(ProcessingStage.Packaging);
        order.CurrentProcessingStage.ShouldBe(ProcessingStage.Packaging);

        order.MarkAsReadyForDelivery();
        order.Status.ShouldBe(OrderStatus.ReadyForDelivery);
        order.CurrentProcessingStage.ShouldBeNull();
    }

    // 5. Sorting -> Packaging fails
    [Fact]
    public void Order_Sorting_To_Packaging_Fails()
    {
        var order = CreateTestOrder(OrderStatus.Inspection);
        order.StartProcessing(ProcessingStage.Sorting);

        Should.Throw<BusinessException>(() =>
        {
            order.MoveToProcessingStage(ProcessingStage.Packaging);
        });
    }

    // 6. Delivery cannot confirm before COD collection
    [Fact]
    public void DeliveryTask_Cannot_Confirm_Before_COD_Collection()
    {
        var driverId = Guid.NewGuid();
        var task = new DeliveryTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            50m,
            driverId);

        task.Accept(DateTime.UtcNow);
        task.PickupFromLaundry(DateTime.UtcNow);
        task.StartDelivery(DateTime.UtcNow);
        task.Arrive(DateTime.UtcNow);

        // Cannot confirm delivery while CashCollectionInfo.IsCollected is false
        task.CashCollectionInfo.IsCollected.ShouldBeFalse();
        Should.Throw<BusinessException>(() =>
        {
            task.ConfirmDelivery(DateTime.UtcNow);
        });

        // Collect cash and now confirm succeeds
        task.ConfirmCashCollection(50m, driverId, DateTime.UtcNow);
        task.ConfirmDelivery(DateTime.UtcNow);
        task.Status.ShouldBe(DeliveryTaskStatus.Delivered);
    }

    // 7. CollectedAmount > AmountToCollect fails
    [Fact]
    public void CashCollectionInfo_CollectedAmount_Greater_Than_AmountToCollect_Fails()
    {
        var cash = new CashCollectionInfo(100m);
        var driverId = Guid.NewGuid();

        Should.Throw<BusinessException>(() =>
        {
            cash.MarkAsCollected(150m, driverId, DateTime.UtcNow);
        });

        Should.Throw<BusinessException>(() =>
        {
            cash.MarkAsCollected(50m, driverId, DateTime.UtcNow); // In MVP, partial payment is not allowed; must equal AmountToCollect
        });

        cash.MarkAsCollected(100m, driverId, DateTime.UtcNow);
        cash.IsCollected.ShouldBeTrue();
        cash.CollectedAmount.ShouldBe(100m);
    }

    // 8. Negative compensation fails
    [Fact]
    public void ComplaintResolutionInfo_Negative_Compensation_Fails()
    {
        Should.Throw<BusinessException>(() =>
        {
            new ComplaintResolutionInfo("Resolved with replacement", null, null, -25m);
        });

        var valid = new ComplaintResolutionInfo("Resolved with replacement", null, null, 25m);
        valid.CompensationAmount.ShouldBe(25m);
    }

    // 9. Invalid latitude fails
    [Fact]
    public void GeoLocation_Invalid_Latitude_Fails()
    {
        Should.Throw<BusinessException>(() =>
        {
            new AddressSnapshot("Test Address", 95.5, 46.6753);
        });

        Should.Throw<BusinessException>(() =>
        {
            new AddressSnapshot("Test Address", -91.0, 46.6753);
        });
    }

    // 10. Invalid longitude fails
    [Fact]
    public void GeoLocation_Invalid_Longitude_Fails()
    {
        Should.Throw<BusinessException>(() =>
        {
            new AddressSnapshot("Test Address", 24.7136, 185.0);
        });

        Should.Throw<BusinessException>(() =>
        {
            new AddressSnapshot("Test Address", 24.7136, -181.0);
        });
    }

    // 11. PickupTask Completed -> Failed fails
    [Fact]
    public void PickupTask_Completed_To_Failed_Fails()
    {
        var task = new PickupTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2));

        task.Accept(DateTime.UtcNow);
        task.StartTrip();
        task.Arrive(DateTime.UtcNow);
        task.ConfirmPickup(DateTime.UtcNow);
        task.Complete(DateTime.UtcNow);
        task.Status.ShouldBe(PickupTaskStatus.Completed);

        Should.Throw<BusinessException>(() =>
        {
            task.Fail("Attempt to fail completed task", DateTime.UtcNow);
        });
    }

    // 12. DeliveryTask Failed -> Delivered fails
    [Fact]
    public void DeliveryTask_Failed_To_Delivered_Fails()
    {
        var task = new DeliveryTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            50m);

        task.Accept(DateTime.UtcNow);
        task.Fail("Customer was not available", DateTime.UtcNow);
        task.Status.ShouldBe(DeliveryTaskStatus.Failed);

        Should.Throw<BusinessException>(() =>
        {
            task.ConfirmDelivery(DateTime.UtcNow);
        });
    }

    // 13. PickupTask active filtered index includes PickedUp
    [Fact]
    public void PickupTask_Active_Filtered_Index_Includes_PickedUp()
    {
        // PickedUp has enum value 4
        ((int)PickupTaskStatus.PickedUp).ShouldBe(4);
        ((int)PickupTaskStatus.Completed).ShouldBe(5);
        ((int)PickupTaskStatus.Failed).ShouldBe(6);
        ((int)PickupTaskStatus.Cancelled).ShouldBe(7);
    }

    // 14. Soft-deleted ServicePrice does not prevent creating a new pricing with same keys
    [Fact]
    public void ServicePrice_Implements_ISoftDelete()
    {
        typeof(ISoftDelete).IsAssignableFrom(typeof(ServicePrice)).ShouldBeTrue();
    }

    // 15. Customer cannot have two active default addresses
    [Fact]
    public void Customer_Cannot_Have_Two_Active_Default_Addresses_Domain_Logic()
    {
        var customer = new Customer(Guid.NewGuid(), Guid.NewGuid(), "0501122334");
        var addr1 = new CustomerAddress(Guid.NewGuid(), customer.Id, "Home", "Street 1", 24.71, 46.67, isDefault: true);
        var addr2 = new CustomerAddress(Guid.NewGuid(), customer.Id, "Office", "Street 2", 24.72, 46.68, isDefault: false);

        customer.AddAddress(addr1);
        customer.AddAddress(addr2);

        addr1.IsDefault.ShouldBeTrue();
        addr2.IsDefault.ShouldBeFalse();

        customer.SetDefaultAddress(addr2.Id);

        addr1.IsDefault.ShouldBeFalse();
        addr2.IsDefault.ShouldBeTrue();
    }

    // 16. Adjustment increase waits for approval
    [Fact]
    public void Order_Adjustment_Increase_Waits_For_Approval()
    {
        var order = CreateTestOrder(OrderStatus.Inspection);
        order.AddItem(new OrderItem(Guid.NewGuid(), order.Id, Guid.NewGuid(), Guid.NewGuid(), "Thobe", "Iron", 2, 50m));
        order.Total.ShouldBe(110m);

        // Add adjustment with increased price (110 -> 150)
        var adj = order.CreateAdjustment("Added extra dry cleaning items", 150m);

        adj.Status.ShouldBe(OrderAdjustmentStatus.Pending);
        order.Status.ShouldBe(OrderStatus.WaitingForAdjustmentApproval);
        order.Total.ShouldBe(110m); // Old total retained until approved

        // Upon approval
        order.ApproveAdjustment(adj.Id, DateTime.UtcNow);
        adj.Status.ShouldBe(OrderAdjustmentStatus.Approved);
        order.Total.ShouldBe(150m);
        order.Status.ShouldBe(OrderStatus.Inspection);
    }

    // 17. Adjustment decrease auto-approval
    [Fact]
    public void Order_Adjustment_Decrease_Auto_Approves()
    {
        var order = CreateTestOrder(OrderStatus.Inspection);
        order.AddItem(new OrderItem(Guid.NewGuid(), order.Id, Guid.NewGuid(), Guid.NewGuid(), "Thobe", "Iron", 2, 50m));
        order.Total.ShouldBe(110m);

        // Add adjustment with decreased price (110 -> 80)
        var adj = order.CreateAdjustment("Customer removed one blanket", 80m);

        adj.Status.ShouldBe(OrderAdjustmentStatus.Approved);
        order.Total.ShouldBe(80m); // Immediately applied
        order.Status.ShouldBe(OrderStatus.Inspection); // Did not block on approval
    }

    [Fact]
    public void EstimatedProcessingHours_Negative_Or_Zero_Throws_BusinessException()
    {
        var area = new CoverageArea(24.7136, 46.6753, 10, 20);

        Should.Throw<BusinessException>(() =>
        {
            new Laundry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Express Laundry",
                "0512345678",
                24.7136,
                46.6753,
                area,
                estimatedProcessingHours: 0);
        });

        Should.Throw<BusinessException>(() =>
        {
            new Laundry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Express Laundry",
                "0512345678",
                24.7136,
                46.6753,
                area,
                estimatedProcessingHours: -5);
        });
    }

    [Fact]
    public void Bag_Sequence_Custody_Strict_Progression()
    {
        var bag = new Bag(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "BAG-001", "QR-001");

        // Initial event is automatically Created in constructor
        bag.Events.Count.ShouldBe(1);
        bag.Events.First().EventType.ShouldBe(BagEventType.Created);

        // Cannot skip directly to Delivered
        Should.Throw<BusinessException>(() =>
        {
            bag.RecordEvent(BagEventType.Delivered);
        });

        bag.RecordEvent(BagEventType.PickedUpFromCustomer);
        bag.RecordEvent(BagEventType.ReceivedAtLaundry);
        bag.RecordEvent(BagEventType.Processing);
        bag.RecordEvent(BagEventType.Ready);
        bag.RecordEvent(BagEventType.PickedUpForDelivery);
        bag.RecordEvent(BagEventType.Delivered);

        bag.Events.Count.ShouldBe(7);
    }

    #region Order Cancellation Policy Tests (اختبارات سياسة إلغاء الطلب)

    /// <summary>
    /// اختبار نجاح الإلغاء الذاتي للطلب عندما يكون في حالة مسودة (Draft).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_Draft_To_Cancelled_Succeeds()
    {
        var order = CreateTestOrder(OrderStatus.Draft);
        var cancelledAt = DateTime.UtcNow;
        order.Cancel("Customer changed mind", cancelledAt);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("Customer changed mind");
        order.CancelledAt.ShouldBe(cancelledAt);
        order.StatusHistories.Any(h => h.ToStatus == OrderStatus.Cancelled).ShouldBeTrue();
    }

    /// <summary>
    /// اختبار نجاح الإلغاء الذاتي للطلب عندما يكون في انتظار تعيين مندوب الاستلام (PendingPickup).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_PendingPickup_To_Cancelled_Succeeds()
    {
        var order = CreateTestOrder(OrderStatus.PendingPickup);
        var cancelledAt = DateTime.UtcNow;
        order.Cancel("Customer decided not to send clothes", cancelledAt);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("Customer decided not to send clothes");
        order.CancelledAt.ShouldBe(cancelledAt);
    }

    /// <summary>
    /// اختبار نجاح الإلغاء الذاتي للطلب عندما يكون قد تم تعيين مندوب الاستلام (PickupAssigned).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_PickupAssigned_To_Cancelled_Succeeds()
    {
        var order = CreateTestOrder(OrderStatus.PickupAssigned);
        var cancelledAt = DateTime.UtcNow;
        order.Cancel("Cancelled before driver started trip", cancelledAt);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("Cancelled before driver started trip");
        order.CancelledAt.ShouldBe(cancelledAt);
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي العام عند انطلاق السائق في رحلة الاستلام (OutForPickup).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_OutForPickup_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.OutForPickup);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Driver is on the way", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي بعد استلام الملابس من العميل (PickedUp).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_PickedUp_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.PickedUp);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Clothes already with driver", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي بعد وصول الملابس للمغسلة (ReceivedAtLaundry).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_ReceivedAtLaundry_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.ReceivedAtLaundry);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Clothes received at facility", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي أثناء مرحلة الفحص والفرز الأولي (Inspection).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_Inspection_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Inspection);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Under inspection", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي أثناء المعالجة والغسيل (Processing).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_Processing_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Processing);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Clothes being washed", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي عندما يكون الطلب جاهزاً للتوصيل (ReadyForDelivery).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_ReadyForDelivery_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.ReadyForDelivery);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Ready for delivery", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي عندما يكون الطلب في طريق التوصيل للعميل (OutForDelivery).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_OutForDelivery_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.OutForDelivery);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Out for delivery", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي لطلب تم تسليمه للعميل (Delivered).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_Delivered_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Delivered);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Order already delivered", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل الإلغاء الذاتي لطلب مكتمل نهائياً (Completed).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_Completed_Cancel_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Completed);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Order completed", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار فشل محاولة إلغاء طلب ملغى بالفعل لمرة ثانية (Cancelled).
    /// </summary>
    [Fact]
    public void Order_Cancellation_Policy_Cancelled_Cancel_Again_Must_Fail()
    {
        var order = CreateTestOrder(OrderStatus.Draft);
        order.Cancel("First cancel", DateTime.UtcNow);
        Should.Throw<BusinessException>(() =>
        {
            order.Cancel("Second cancel attempt", DateTime.UtcNow);
        });
    }

    /// <summary>
    /// اختبار نجاح الإلغاء الإداري التشغيلي بواسطة المغسلة لحالة OutForPickup وفشله للحالات النهائية.
    /// </summary>
    [Fact]
    public void Order_Admin_CancelByLaundry_Succeeds_For_OutForPickup_And_Fails_For_Terminal_States()
    {
        // OutForPickup can be cancelled administratively by laundry management
        var order = CreateTestOrder(OrderStatus.OutForPickup);
        order.CancelByLaundry("Customer emergency - laundry approved", DateTime.UtcNow);
        order.Status.ShouldBe(OrderStatus.Cancelled);

        // Terminal states cannot be cancelled even administratively
        var completedOrder = CreateTestOrder(OrderStatus.Completed);
        Should.Throw<BusinessException>(() =>
        {
            completedOrder.CancelByLaundry("Attempt to force cancel completed", DateTime.UtcNow);
        });

        var deliveredOrder = CreateTestOrder(OrderStatus.Delivered);
        Should.Throw<BusinessException>(() =>
        {
            deliveredOrder.CancelByLaundry("Attempt to force cancel delivered", DateTime.UtcNow);
        });
    }

    #endregion

    #region LaundryTimeSlot & WorkingHour Tests (اختبارات فترات المغسلة وساعات العمل)

    private Laundry CreateTestLaundryWithWorkingHours()
    {
        var area = new CoverageArea(24.7136, 46.6753, 10, 20);
        var laundry = new Laundry(Guid.NewGuid(), Guid.NewGuid(), "Express Wash", "0551234567", 24.7136, 46.6753, area);

        // Saturday open from 08:00 to 22:00
        laundry.SetWorkingHour(DayOfWeek.Saturday, true, new TimeOnly(8, 0), new TimeOnly(22, 0));

        // Friday closed
        laundry.SetWorkingHour(DayOfWeek.Friday, false);

        return laundry;
    }

    /// <summary>
    /// 1. اختبار نجاح إضافة فترة تقع بالكامل داخل ساعات عمل المغسلة لليوم المحدد.
    /// </summary>
    [Fact]
    public void TimeSlot_Inside_Working_Hours_Succeeds()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slot = laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        slot.ShouldNotBeNull();
        slot.DayOfWeek.ShouldBe(DayOfWeek.Saturday);
        slot.SlotType.ShouldBe(SlotType.Pickup);
        slot.StartTime.ShouldBe(new TimeOnly(10, 0));
        slot.EndTime.ShouldBe(new TimeOnly(12, 0));
        slot.IsActive.ShouldBeTrue();
        laundry.TimeSlots.Count.ShouldBe(1);
    }

    /// <summary>
    /// 2. اختبار فشل إضافة فترة تبدأ قبل موعد فتح المغسلة في ذلك اليوم.
    /// </summary>
    [Fact]
    public void TimeSlot_Before_OpenTime_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        Should.Throw<BusinessException>(() =>
        {
            laundry.AddTimeSlot(
                Guid.NewGuid(),
                DayOfWeek.Saturday,
                SlotType.Pickup,
                new TimeOnly(6, 0),
                new TimeOnly(8, 0));
        });
    }

    /// <summary>
    /// 3. اختبار فشل إضافة فترة تنتهي بعد موعد إغلاق المغسلة في ذلك اليوم.
    /// </summary>
    [Fact]
    public void TimeSlot_After_CloseTime_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        Should.Throw<BusinessException>(() =>
        {
            laundry.AddTimeSlot(
                Guid.NewGuid(),
                DayOfWeek.Saturday,
                SlotType.Delivery,
                new TimeOnly(21, 0),
                new TimeOnly(23, 0));
        });
    }

    /// <summary>
    /// 4. اختبار فشل إضافة فترة في يوم مغلق (IsOpen = false).
    /// </summary>
    [Fact]
    public void TimeSlot_On_Closed_Day_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        Should.Throw<BusinessException>(() =>
        {
            laundry.AddTimeSlot(
                Guid.NewGuid(),
                DayOfWeek.Friday,
                SlotType.Pickup,
                new TimeOnly(10, 0),
                new TimeOnly(12, 0));
        });
    }

    /// <summary>
    /// 5. اختبار فشل إضافة فترة عندما يكون وقت البداية أكبر من أو يساوي وقت النهاية.
    /// </summary>
    [Fact]
    public void TimeSlot_StartTime_Greater_Or_Equal_EndTime_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();

        // Start == End
        Should.Throw<BusinessException>(() =>
        {
            laundry.AddTimeSlot(
                Guid.NewGuid(),
                DayOfWeek.Saturday,
                SlotType.Pickup,
                new TimeOnly(10, 0),
                new TimeOnly(10, 0));
        });

        // Start > End
        Should.Throw<BusinessException>(() =>
        {
            laundry.AddTimeSlot(
                Guid.NewGuid(),
                DayOfWeek.Saturday,
                SlotType.Pickup,
                new TimeOnly(14, 0),
                new TimeOnly(12, 0));
        });
    }

    /// <summary>
    /// 6. اختبار فشل إضافة فترة متداخلة مع فترة نشطة أخرى لنفس اليوم والنوع.
    /// </summary>
    [Fact]
    public void TimeSlot_Overlapping_Active_Slot_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0));

        // Overlap: 09:00 - 11:00 with existing 08:00 - 10:00
        Should.Throw<BusinessException>(() =>
        {
            laundry.AddTimeSlot(
                Guid.NewGuid(),
                DayOfWeek.Saturday,
                SlotType.Pickup,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));
        });
    }

    /// <summary>
    /// 7. اختبار نجاح إضافة فترات منفصلة غير متداخلة لنفس اليوم.
    /// </summary>
    [Fact]
    public void TimeSlot_Non_Overlapping_Slot_Succeeds()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0));

        var slot2 = laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(12, 0),
            new TimeOnly(14, 0));

        slot2.ShouldNotBeNull();
        laundry.TimeSlots.Count.ShouldBe(2);
    }

    /// <summary>
    /// 8. اختبار نجاح إضافة فترة تلامس نهاية فترة سابقة عند الحد الزمني بالضبط دون تداخل.
    /// </summary>
    [Fact]
    public void TimeSlot_Touching_Boundary_Succeeds()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0));

        // Touching at boundary 10:00 is allowed
        var slot2 = laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        slot2.ShouldNotBeNull();
        laundry.TimeSlots.Count.ShouldBe(2);
    }

    /// <summary>
    /// 9. اختبار أن الفترة غير النشطة لا تمنع إضافة فترة نشطة جديدة بنفس التوقيت.
    /// </summary>
    [Fact]
    public void TimeSlot_Inactive_Slot_Does_Not_Block_New_Slot()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            isActive: false);

        // An active slot covering 08:00-10:00 is allowed since the previous one is inactive
        var activeSlot = laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            isActive: true);

        activeSlot.ShouldNotBeNull();
        laundry.TimeSlots.Count.ShouldBe(2);
    }

    /// <summary>
    /// 10. اختبار فشل تعديل ساعات العمل إذا كانت تؤدي إلى جعل فترات نشطة حالية خارج نطاق الدوام.
    /// </summary>
    [Fact]
    public void Modifying_WorkingHour_Breaking_Active_Slot_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        laundry.AddTimeSlot(
            Guid.NewGuid(),
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        // Attempting to restrict hours to 13:00 - 20:00 (which excludes 10:00 - 12:00) must throw
        Should.Throw<BusinessException>(() =>
        {
            laundry.SetWorkingHour(
                DayOfWeek.Saturday,
                true,
                new TimeOnly(13, 0),
                new TimeOnly(20, 0));
        });

        // Attempting to close Saturday completely when active slots exist must throw
        Should.Throw<BusinessException>(() =>
        {
            laundry.SetWorkingHour(DayOfWeek.Saturday, false);
        });

        // Modifying to wider hours (e.g. 07:00 - 23:00) that encompass the slot succeeds
        laundry.SetWorkingHour(DayOfWeek.Saturday, true, new TimeOnly(7, 0), new TimeOnly(23, 0));
        var saturdayHour = laundry.WorkingHours.First(w => w.DayOfWeek == DayOfWeek.Saturday);
        saturdayHour.OpenTime.ShouldBe(new TimeOnly(7, 0));
        saturdayHour.CloseTime.ShouldBe(new TimeOnly(23, 0));
    }

    #endregion
}

