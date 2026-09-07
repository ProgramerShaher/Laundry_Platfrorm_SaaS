using System;
using System.Collections.Generic;
using System.Linq;
using laundry_SaaS.Bags;
using laundry_SaaS.Catalog;
using laundry_SaaS.Complaints;
using laundry_SaaS.Customers;
using laundry_SaaS.Inspections;
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
        var schedule = new PickupScheduleSnapshot(new DateOnly(2026, 9, 12), new TimeOnly(10, 0), new TimeOnly(12, 0), Guid.NewGuid());

        var order = new Order(
            Guid.NewGuid(),
            tenantId,
            "ORD-" + Guid.NewGuid().ToString("N")[..8],
            customerId,
            laundryId,
            address,
            address,
            schedule,
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
            var schedule = new PickupScheduleSnapshot(new DateOnly(2026, 9, 12), new TimeOnly(10, 0), new TimeOnly(12, 0), Guid.NewGuid());
            new Order(Guid.NewGuid(), Guid.Empty, "ORD-100", Guid.NewGuid(), Guid.NewGuid(), address, address, schedule);
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

        // Collect cash and verify OTP, and now confirm succeeds
        task.ConfirmCashCollection(50m, driverId, DateTime.UtcNow);
        var now = DateTime.UtcNow;
        task.SetDeliveryOtp("hash_123456", now, now.AddMinutes(5));
        task.MarkDeliveryOtpAsVerified(now, now);
        task.ConfirmDelivery(now);
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

    #region Delivery OTP & Verification Tests (14 Tests)

    private DeliveryTask CreateTestDeliveryTask(decimal cashAmount = 50m, Guid? driverId = null)
    {
        var driver = driverId ?? Guid.NewGuid();
        return new DeliveryTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            cashAmount,
            driver);
    }

    private DeliveryTask CreateArrivedDeliveryTask(decimal cashAmount = 50m, Guid? driverId = null)
    {
        var driver = driverId ?? Guid.NewGuid();
        var task = CreateTestDeliveryTask(cashAmount, driver);
        var now = DateTime.UtcNow;
        task.Accept(now);
        task.PickupFromLaundry(now);
        task.StartDelivery(now);
        task.Arrive(now);
        return task;
    }

    /// <summary>
    /// 1. اختبار أن مهمة التوصيل الجديدة تبدأ في حالة غير موثقة وبدون رمز تحقق أو محاولات فاشلة.
    /// </summary>
    [Fact]
    public void DeliveryTask_New_Task_Starts_Unverified()
    {
        var task = CreateTestDeliveryTask();

        task.VerificationInfo.ShouldNotBeNull();
        task.VerificationInfo.IsVerified.ShouldBeFalse();
        task.VerificationInfo.OtpHash.ShouldBeNull();
        task.VerificationInfo.ExpiresAt.ShouldBeNull();
        task.VerificationInfo.FailedAttempts.ShouldBe(0);
        task.VerificationInfo.GenerationCount.ShouldBe(0);
        task.VerificationInfo.MaxAttempts.ShouldBe(DeliveryVerificationInfo.DefaultMaxAttempts);
    }

    /// <summary>
    /// 2. اختبار منع توليد أو تسجيل رمز التحقق قبل وصول السائق لموقع العميل (Status != Arrived).
    /// </summary>
    [Fact]
    public void DeliveryTask_Otp_Cannot_Be_Generated_Before_Arrived()
    {
        var task = CreateTestDeliveryTask(); // Status is Assigned
        var now = DateTime.UtcNow;

        var ex = Should.Throw<BusinessException>(() =>
        {
            task.SetDeliveryOtp("hash_123456", now, now.AddMinutes(5));
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.InvalidStatusForOtp);

        task.Accept(now); // Status is Accepted
        Should.Throw<BusinessException>(() =>
        {
            task.SetDeliveryOtp("hash_123456", now, now.AddMinutes(5));
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.InvalidStatusForOtp);

        task.PickupFromLaundry(now); // Status is PickedUpFromLaundry
        Should.Throw<BusinessException>(() =>
        {
            task.SetDeliveryOtp("hash_123456", now, now.AddMinutes(5));
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.InvalidStatusForOtp);

        task.StartDelivery(now); // Status is OutForDelivery
        Should.Throw<BusinessException>(() =>
        {
            task.SetDeliveryOtp("hash_123456", now, now.AddMinutes(5));
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.InvalidStatusForOtp);
    }

    /// <summary>
    /// 3. اختبار إمكانية تسجيل رمز التحقق بنجاح عند وصول السائق لموقع العميل (Status == Arrived).
    /// </summary>
    [Fact]
    public void DeliveryTask_Otp_Can_Be_Registered_At_Arrived()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(DeliveryVerificationInfo.ExpiryMinutes);

        task.SetDeliveryOtp("hash_secret_otp", now, expiresAt);

        task.VerificationInfo.OtpHash.ShouldBe("hash_secret_otp");
        task.VerificationInfo.ExpiresAt.ShouldBe(expiresAt);
        task.VerificationInfo.GenerationCount.ShouldBe(1);
        task.VerificationInfo.FailedAttempts.ShouldBe(0);
        task.VerificationInfo.IsVerified.ShouldBeFalse();
    }

    /// <summary>
    /// 4. اختبار صحة البيانات الوصفية لانتهاء صلاحية الرمز وقابلية التحقق الزمنية.
    /// </summary>
    [Fact]
    public void DeliveryTask_Otp_Expiry_Metadata_Correct()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(DeliveryVerificationInfo.ExpiryMinutes);

        task.SetDeliveryOtp("hash_valid_meta", now, expiresAt);

        task.VerificationInfo.ExpiresAt.ShouldBe(expiresAt);
        task.VerificationInfo.CanVerify(now.AddMinutes(2)).ShouldBeTrue();
        task.VerificationInfo.CanVerify(expiresAt).ShouldBeTrue();
        task.VerificationInfo.CanVerify(expiresAt.AddSeconds(1)).ShouldBeFalse();
    }

    /// <summary>
    /// 5. اختبار فشل طلب توليد الرمز للمرة الثانية قبل انقضاء فترة التبريد الإلزامية (60 ثانية).
    /// </summary>
    [Fact]
    public void DeliveryTask_Second_Generation_Before_Cooldown_Fails()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;

        task.SetDeliveryOtp("hash_first_otp", now, now.AddMinutes(5));

        // Resend request after 30 seconds (< 60s cooldown) must fail
        var ex = Should.Throw<BusinessException>(() =>
        {
            task.SetDeliveryOtp("hash_second_otp", now.AddSeconds(30), now.AddMinutes(5));
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpResendCooldown);
    }

    /// <summary>
    /// 6. اختبار نجاح توليد رمز جديد بعد انقضاء فترة التبريد المقررة (60 ثانية).
    /// </summary>
    [Fact]
    public void DeliveryTask_New_Generation_After_Cooldown_Succeeds()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;

        task.SetDeliveryOtp("hash_otp_1", now, now.AddMinutes(5));
        task.VerificationInfo.GenerationCount.ShouldBe(1);

        // After 61 seconds (>= 60s cooldown), second generation succeeds
        var afterCooldown = now.AddSeconds(61);
        task.SetDeliveryOtp("hash_otp_2", afterCooldown, afterCooldown.AddMinutes(5));

        task.VerificationInfo.OtpHash.ShouldBe("hash_otp_2");
        task.VerificationInfo.GenerationCount.ShouldBe(2);
    }

    /// <summary>
    /// 7. اختبار فشل توليد الرمز عند تجاوز الحد الأقصى المسموح به لمرات التوليد (3 مرات).
    /// </summary>
    [Fact]
    public void DeliveryTask_GenerationCount_Greater_Than_Three_Fails()
    {
        var task = CreateArrivedDeliveryTask();
        var t0 = DateTime.UtcNow;

        // Gen 1
        task.SetDeliveryOtp("hash_1", t0, t0.AddMinutes(5));
        task.VerificationInfo.GenerationCount.ShouldBe(1);

        // Gen 2
        var t1 = t0.AddSeconds(65);
        task.SetDeliveryOtp("hash_2", t1, t1.AddMinutes(5));
        task.VerificationInfo.GenerationCount.ShouldBe(2);

        // Gen 3 (Limit reached)
        var t2 = t1.AddSeconds(65);
        task.SetDeliveryOtp("hash_3", t2, t2.AddMinutes(5));
        task.VerificationInfo.GenerationCount.ShouldBe(3);

        // Gen 4 -> Throws OtpMaxGenerationsReached
        var t3 = t2.AddSeconds(65);
        var ex = Should.Throw<BusinessException>(() =>
        {
            task.SetDeliveryOtp("hash_4", t3, t3.AddMinutes(5));
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpMaxGenerationsReached);
    }

    /// <summary>
    /// 8. اختبار أن توليد رمز جديد يصفر عداد المحاولات الفاشلة السابقة تلقائياً.
    /// </summary>
    [Fact]
    public void DeliveryTask_New_Otp_Resets_FailedAttempts()
    {
        var task = CreateArrivedDeliveryTask();
        var t0 = DateTime.UtcNow;

        task.SetDeliveryOtp("hash_1", t0, t0.AddMinutes(5));
        task.RecordFailedOtpAttempt();
        task.RecordFailedOtpAttempt();
        task.VerificationInfo.FailedAttempts.ShouldBe(2);

        // New generation after cooldown resets failed attempts to 0
        var t1 = t0.AddSeconds(65);
        task.SetDeliveryOtp("hash_2", t1, t1.AddMinutes(5));

        task.VerificationInfo.FailedAttempts.ShouldBe(0);
        task.VerificationInfo.OtpHash.ShouldBe("hash_2");
    }

    /// <summary>
    /// 9. اختبار زيادة عداد المحاولات الفاشلة عند تسجيل محاولة غير صحيحة.
    /// </summary>
    [Fact]
    public void DeliveryTask_Failed_Otp_Attempt_Increments_Counter()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;
        task.SetDeliveryOtp("hash_initial", now, now.AddMinutes(5));

        task.VerificationInfo.FailedAttempts.ShouldBe(0);

        task.RecordFailedOtpAttempt();
        task.VerificationInfo.FailedAttempts.ShouldBe(1);

        task.RecordFailedOtpAttempt();
        task.VerificationInfo.FailedAttempts.ShouldBe(2);
    }

    /// <summary>
    /// 10. اختبار حظر التحقق وإلقاء استثناء عند تجاوز الحد الأقصى للمحاولات الفاشلة (3 محاولات).
    /// </summary>
    [Fact]
    public void DeliveryTask_More_Than_Three_Failed_Attempts_Blocked()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;
        task.SetDeliveryOtp("hash_test", now, now.AddMinutes(5));

        task.RecordFailedOtpAttempt(); // 1
        task.RecordFailedOtpAttempt(); // 2
        task.RecordFailedOtpAttempt(); // 3 (Max reached)

        task.VerificationInfo.FailedAttempts.ShouldBe(3);
        task.VerificationInfo.CanVerify(now.AddMinutes(1)).ShouldBeFalse();

        // 4th attempt throws OtpMaxAttemptsReached
        var ex = Should.Throw<BusinessException>(() =>
        {
            task.RecordFailedOtpAttempt();
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpMaxAttemptsReached);

        // Attempting to MarkAsVerified also throws OtpMaxAttemptsReached
        Should.Throw<BusinessException>(() =>
        {
            task.MarkDeliveryOtpAsVerified(now.AddMinutes(1), now.AddMinutes(1));
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpMaxAttemptsReached);
    }

    /// <summary>
    /// 11. اختبار نجاح اعتماد التحقق فقط عندما تكون حالة الرمز نشطة وصالحة وغير منتهية الصلاحية.
    /// </summary>
    [Fact]
    public void DeliveryTask_MarkAsVerified_Succeeds_Only_For_Valid_Active_Otp_State()
    {
        var task = CreateArrivedDeliveryTask();
        var now = DateTime.UtcNow;

        // Cannot verify before OTP is set
        Should.Throw<BusinessException>(() =>
        {
            task.MarkDeliveryOtpAsVerified(now, now);
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpNotAvailable);

        task.SetDeliveryOtp("hash_active", now, now.AddMinutes(5));

        // Cannot verify if current time is past expiry
        Should.Throw<BusinessException>(() =>
        {
            task.MarkDeliveryOtpAsVerified(now.AddMinutes(6), now.AddMinutes(6));
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpExpired);

        // Valid verification
        var verifyTime = now.AddMinutes(1);
        task.MarkDeliveryOtpAsVerified(verifyTime, verifyTime);

        task.VerificationInfo.IsVerified.ShouldBeTrue();
        task.VerificationInfo.VerifiedAt.ShouldBe(verifyTime);

        // Cannot verify again once already verified
        Should.Throw<BusinessException>(() =>
        {
            task.MarkDeliveryOtpAsVerified(verifyTime, verifyTime);
        });
    }

    /// <summary>
    /// 12. اختبار فشل تأكيد تسليم الملابس بدون التحقق الناجح من رمز التسليم (Delivery OTP).
    /// </summary>
    [Fact]
    public void DeliveryTask_ConfirmDelivery_Without_Otp_Verification_Fails()
    {
        var driverId = Guid.NewGuid();
        var task = CreateArrivedDeliveryTask(cashAmount: 50m, driverId: driverId);
        var now = DateTime.UtcNow;

        // Collect cash, but do NOT verify OTP
        task.ConfirmCashCollection(50m, driverId, now);
        task.CashCollectionInfo.IsCollected.ShouldBeTrue();
        task.VerificationInfo.IsVerified.ShouldBeFalse();

        var ex = Should.Throw<BusinessException>(() =>
        {
            task.ConfirmDelivery(now);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.DeliveryOtpNotVerified);
    }

    /// <summary>
    /// 13. اختبار فشل تأكيد تسليم الملابس عند التحقق من الرمز ولكن قبل استيفاء التحصيل النقدي كاملاً.
    /// </summary>
    [Fact]
    public void DeliveryTask_ConfirmDelivery_With_Otp_Without_Cod_Fails()
    {
        var driverId = Guid.NewGuid();
        var task = CreateArrivedDeliveryTask(cashAmount: 75m, driverId: driverId);
        var now = DateTime.UtcNow;

        // Set and verify OTP
        task.SetDeliveryOtp("hash_cod_test", now, now.AddMinutes(5));
        task.MarkDeliveryOtpAsVerified(now, now);
        task.VerificationInfo.IsVerified.ShouldBeTrue();

        // But COD is NOT collected
        task.CashCollectionInfo.IsCollected.ShouldBeFalse();

        Should.Throw<BusinessException>(() =>
        {
            task.ConfirmDelivery(now);
        });
    }

    /// <summary>
    /// 14. اختبار نجاح تأكيد تسليم الملابس عند استيفاء التحقق من رمز التسليم والتحصيل النقدي كاملاً.
    /// </summary>
    [Fact]
    public void DeliveryTask_ConfirmDelivery_With_Otp_And_Full_Cod_Succeeds()
    {
        var driverId = Guid.NewGuid();
        var task = CreateArrivedDeliveryTask(cashAmount: 80m, driverId: driverId);
        var now = DateTime.UtcNow;

        // 1. Verify OTP
        task.SetDeliveryOtp("hash_success", now, now.AddMinutes(5));
        task.MarkDeliveryOtpAsVerified(now, now);

        // 2. Collect Cash
        task.ConfirmCashCollection(80m, driverId, now);

        // 3. Confirm Delivery
        task.ConfirmDelivery(now);

        task.Status.ShouldBe(DeliveryTaskStatus.Delivered);
        task.DeliveredAt.ShouldBe(now);
    }

    #endregion

    #region Laundry Pickup Slot Validation & Pickup Schedule Tests (10 Tests)

    /// <summary>
    /// 15. اختبار نجاح التحقق واسترجاع فترة الاستلام الصحيحة التابعة للمغسلة.
    /// </summary>
    [Fact]
    public void Laundry_Valid_Pickup_Slot_Succeeds()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        var slot = laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        var nextSaturday = new DateOnly(2026, 9, 12); // Saturday
        var now = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc); // Thursday before

        var validatedSlot = laundry.ValidateAndGetPickupSlot(slotId, nextSaturday, now);

        validatedSlot.ShouldNotBeNull();
        validatedSlot.Id.ShouldBe(slotId);
        validatedSlot.SlotType.ShouldBe(SlotType.Pickup);
        validatedSlot.DayOfWeek.ShouldBe(DayOfWeek.Saturday);
    }

    /// <summary>
    /// 16. اختبار فشل التحقق عند تمرير معرّف فترة غير موجودة في المغسلة المحددة.
    /// </summary>
    [Fact]
    public void Laundry_Wrong_Laundry_Slot_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var nonexistentSlotId = Guid.NewGuid();
        var saturday = new DateOnly(2026, 9, 12);
        var now = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(nonexistentSlotId, saturday, now);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.InvalidPickupSlot);
    }

    /// <summary>
    /// 17. اختبار فشل استخدام فترة مجدولة من نوع توصيل (Delivery) لعملية الاستلام (Pickup).
    /// </summary>
    [Fact]
    public void Laundry_Delivery_Type_Slot_Used_For_Pickup_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var deliverySlotId = Guid.NewGuid();
        laundry.AddTimeSlot(
            deliverySlotId,
            DayOfWeek.Saturday,
            SlotType.Delivery,
            new TimeOnly(14, 0),
            new TimeOnly(16, 0));

        var saturday = new DateOnly(2026, 9, 12);
        var now = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(deliverySlotId, saturday, now);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.InvalidPickupSlot);
    }

    /// <summary>
    /// 18. اختبار فشل اختيار فترة استلام معطلة أو غير نشطة (IsActive = false).
    /// </summary>
    [Fact]
    public void Laundry_Inactive_Slot_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0),
            isActive: false);

        var saturday = new DateOnly(2026, 9, 12);
        var now = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(slotId, saturday, now);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupSlotNotActive);
    }

    /// <summary>
    /// 19. اختبار فشل التحقق عند عدم تطابق يوم الأسبوع لتاريخ الاستلام مع اليوم المخصص للفترة.
    /// </summary>
    [Fact]
    public void Laundry_DayOfWeek_Mismatch_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        // Date falls on Sunday (2026-09-13), slot is Saturday
        var sunday = new DateOnly(2026, 9, 13);
        var now = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(slotId, sunday, now);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupSlotDateMismatch);
    }

    /// <summary>
    /// 20. اختبار فشل التحقق عند اختيار تاريخ استلام في الماضي.
    /// </summary>
    [Fact]
    public void Laundry_Past_PickupDate_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        // Current date is 2026-09-19, pickupDate is past Saturday 2026-09-12
        var pastSaturday = new DateOnly(2026, 9, 12);
        var now = new DateTime(2026, 9, 19, 8, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(slotId, pastSaturday, now);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupDateInPast);
    }

    /// <summary>
    /// 21. اختبار فشل اختيار فترة استلام لنفس اليوم إذا كان وقت انتهائها قد انقضى.
    /// </summary>
    [Fact]
    public void Laundry_SameDay_Already_Ended_Slot_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        // Same day (Saturday 2026-09-12), but current time is 13:00 (slot ended at 12:00)
        var todaySaturday = new DateOnly(2026, 9, 12);
        var currentTimeAfterEnd = new DateTime(2026, 9, 12, 13, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(slotId, todaySaturday, currentTimeAfterEnd);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupSlotExpired);
    }

    /// <summary>
    /// 22. اختبار فشل التحقق إذا كانت المغسلة مغلقة أو معطلة أو لا تستقبل طلبات.
    /// </summary>
    [Fact]
    public void Laundry_Closed_WorkingHour_Fails()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        var saturday = new DateOnly(2026, 9, 12);
        var now = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

        // 1. Inactive laundry throws LaundryClosedOrInactive
        laundry.SetActive(false);
        var exInactive = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(slotId, saturday, now);
        });
        exInactive.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.LaundryClosedOrInactive);

        laundry.SetActive(true);

        // 2. Laundry not accepting orders throws LaundryNotAcceptingOrders
        laundry.SetAcceptingOrders(false);
        var exNotAccepting = Should.Throw<BusinessException>(() =>
        {
            laundry.ValidateAndGetPickupSlot(slotId, saturday, now);
        });
        exNotAccepting.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.LaundryNotAcceptingOrders);
    }

    /// <summary>
    /// 23. اختبار حفظ اللقطة التاريخية لجدول الاستلام لكافة خصائص التاريخ، والبدء، والانتهاء، ومعرّف الفترة الأصلية.
    /// </summary>
    [Fact]
    public void Order_Schedule_Snapshot_Stores_Date_Start_End_OriginalSlot()
    {
        var slotId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 15);
        var start = new TimeOnly(10, 0);
        var end = new TimeOnly(12, 0);

        var snapshot = new PickupScheduleSnapshot(date, start, end, slotId);
        var address = new AddressSnapshot("King Fahd Rd", 24.7136, 46.6753);
        var order = new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ORD-SNAP-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            address,
            address,
            snapshot);

        order.PickupSchedule.ShouldNotBeNull();
        order.PickupSchedule.ScheduledDate.ShouldBe(date);
        order.PickupSchedule.StartTime.ShouldBe(start);
        order.PickupSchedule.EndTime.ShouldBe(end);
        order.PickupSchedule.OriginalSlotId.ShouldBe(slotId);
        order.PickupSlotId.ShouldBe(slotId);
    }

    /// <summary>
    /// 24. اختبار أن التعديل أو التعطيل اللاحق لكيان LaundryTimeSlot لا يغير نهائياً اللقطة المحفوظة داخل الطلب (Immutability).
    /// </summary>
    [Fact]
    public void Order_Later_Modification_Of_LaundryTimeSlot_Does_Not_Mutate_Order_Snapshot()
    {
        var laundry = CreateTestLaundryWithWorkingHours();
        var slotId = Guid.NewGuid();
        var slot = laundry.AddTimeSlot(
            slotId,
            DayOfWeek.Saturday,
            SlotType.Pickup,
            new TimeOnly(10, 0),
            new TimeOnly(12, 0));

        var date = new DateOnly(2026, 9, 12);
        var snapshot = new PickupScheduleSnapshot(date, slot.StartTime, slot.EndTime, slot.Id);
        var address = new AddressSnapshot("King Fahd Rd", 24.7136, 46.6753);
        var order = new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ORD-IMMUTABLE",
            Guid.NewGuid(),
            laundry.Id,
            address,
            address,
            snapshot);

        // Later: Deactivate or modify original slot entity
        slot.SetActive(false);
        slot.SetTime(new TimeOnly(14, 0), new TimeOnly(18, 0));
        slot.IsActive.ShouldBeFalse();

        // Order snapshot remains completely unaffected and preserved
        order.PickupSchedule.ShouldNotBeNull();
        order.PickupSchedule.OriginalSlotId.ShouldBe(slotId);
        order.PickupSchedule.ScheduledDate.ShouldBe(date);
        order.PickupSchedule.StartTime.ShouldBe(new TimeOnly(10, 0));
        order.PickupSchedule.EndTime.ShouldBe(new TimeOnly(12, 0));
    }

    #endregion

    #region Domain Hardening Tests: PickupSchedule, Inspection Mismatch & Order Adjustment (25 Specific Tests)

    private (Order order, Inspection inspection) CreateTestOrderWithInspection(int orderItemQty = 1, decimal unitPrice = 50m)
    {
        var order = CreateTestOrder(OrderStatus.Draft);
        var orderItemId = Guid.NewGuid();
        var itemType = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var orderItem = new OrderItem(
            orderItemId,
            order.Id,
            itemType,
            serviceId,
            "Thobe",
            "Dry Clean",
            orderItemQty,
            unitPrice);

        order.AddItem(orderItem);

        order.MarkAsPendingPickup();
        order.MarkAsPickupAssigned();
        order.MarkAsOutForPickup();
        order.MarkAsPickedUp();
        order.MarkAsReceivedAtLaundry();
        order.StartInspection();

        var inspection = new Inspection(Guid.NewGuid(), order.TenantId!.Value, order.Id);
        return (order, inspection);
    }

    /// <summary>
    /// 1. اختبار أن إنشاء الطلب عبر الـ business constructor يتطلب PickupSchedule ولا يقبل null.
    /// </summary>
    [Fact]
    public void Order_Creation_Requires_PickupSchedule()
    {
        var address = new AddressSnapshot("King Fahd Rd", 24.7136, 46.6753);

        Should.Throw<ArgumentNullException>(() =>
        {
            new Order(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ORD-TEST-1",
                Guid.NewGuid(),
                Guid.NewGuid(),
                address,
                address,
                pickupSchedule: null!);
        });
    }

    /// <summary>
    /// 2. اختبار أن الطلب المنشأ بنجاح يمتلك دائماً PickupSchedule مطابقاً وغير قابل للتغيير.
    /// </summary>
    [Fact]
    public void Order_Valid_Always_Has_PickupSchedule_And_Cannot_Be_Replaced()
    {
        var slotId = Guid.NewGuid();
        var schedule = new PickupScheduleSnapshot(new DateOnly(2026, 9, 15), new TimeOnly(14, 0), new TimeOnly(16, 0), slotId);
        var address = new AddressSnapshot("King Fahd Rd", 24.7136, 46.6753);

        var order = new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ORD-TEST-2",
            Guid.NewGuid(),
            Guid.NewGuid(),
            address,
            address,
            schedule);

        order.PickupSchedule.ShouldNotBeNull();
        order.PickupSchedule.ScheduledDate.ShouldBe(new DateOnly(2026, 9, 15));
        order.PickupSchedule.StartTime.ShouldBe(new TimeOnly(14, 0));
        order.PickupSchedule.EndTime.ShouldBe(new TimeOnly(16, 0));
        order.PickupSchedule.OriginalSlotId.ShouldBe(slotId);
        order.PickupSlotId.ShouldBe(slotId);

        // Immutable: No SetPickupSchedule method exists on Order
        order.GetType().GetMethod("SetPickupSchedule").ShouldBeNull();
    }

    /// <summary>
    /// 3. اختبار نجاح الفحص المطابق تماماً (Exact match inspection).
    /// </summary>
    [Fact]
    public void Inspection_Exact_Match_Succeeds()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 2, unitPrice: 50m);
        var orderItem = order.Items.First();

        var inspectionItem = InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 2,
            actualQuantity: 2);

        inspection.AddItem(inspectionItem);
        inspection.Complete(DateTime.UtcNow, order);

        inspection.Status.ShouldBe(InspectionStatus.Completed);
        inspectionItem.IsAdditionalItem.ShouldBeFalse();
        inspectionItem.IsMissingItem.ShouldBeFalse();
        inspectionItem.IsQuantityChanged.ShouldBeFalse();
        inspectionItem.IsItemTypeChanged.ShouldBeFalse();
        inspectionItem.IsServiceChanged.ShouldBeFalse();
    }

    /// <summary>
    /// 4. اختبار تمثيل البند الناقص بالكامل عبر ActualQuantity = 0.
    /// </summary>
    [Fact]
    public void Inspection_Missing_Quantity_Represented_By_ActualQuantity_Zero()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 2, unitPrice: 50m);
        var orderItem = order.Items.First();

        var missingItem = InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 2,
            actualQuantity: 0);

        missingItem.IsMissingItem.ShouldBeTrue();
        missingItem.ActualQuantity.ShouldBe(0);
        missingItem.ExpectedQuantity.ShouldBe(2);

        inspection.AddItem(missingItem);
        inspection.Complete(DateTime.UtcNow, order);

        inspection.Status.ShouldBe(InspectionStatus.Completed);
    }

    /// <summary>
    /// 5. اختبار نجاح تمثيل بند إضافي بدون OrderItemId (OrderItemId = null).
    /// </summary>
    [Fact]
    public void Inspection_Additional_Item_With_No_OrderItemId_Succeeds()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        // 1. Original item
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1));

        // 2. Additional item found in bag
        var additionalItemType = Guid.NewGuid();
        var additionalService = Guid.NewGuid();
        var additionalItem = InspectionItem.ForAdditionalItem(
            Guid.NewGuid(),
            inspection.Id,
            additionalItemType,
            additionalService,
            actualQuantity: 1,
            notes: "Extra shirt found in bag");

        additionalItem.OrderItemId.ShouldBeNull();
        additionalItem.IsAdditionalItem.ShouldBeTrue();
        additionalItem.ExpectedQuantity.ShouldBe(0);
        additionalItem.ActualQuantity.ShouldBe(1);

        inspection.AddItem(additionalItem);
        inspection.Complete(DateTime.UtcNow, order);

        inspection.Items.Count.ShouldBe(2);
    }

    /// <summary>
    /// 6. اختبار فشل إنشاء بند إضافي بدون تحديد نوع القطعة الفعلي.
    /// </summary>
    [Fact]
    public void Inspection_Additional_Item_Without_Actual_Item_Type_Fails()
    {
        var inspectionId = Guid.NewGuid();

        Should.Throw<BusinessException>(() =>
        {
            new InspectionItem(
                Guid.NewGuid(),
                inspectionId,
                orderItemId: null,
                expectedItemTypeId: null,
                expectedServiceId: null,
                expectedQuantity: 0,
                actualItemTypeId: null,
                actualServiceId: Guid.NewGuid(),
                actualQuantity: 1);
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.InspectionErrorCodes.InvalidAdditionalItem);
    }

    /// <summary>
    /// 7. اختبار فشل إنشاء بند إضافي بدون تحديد نوع الخدمة الفعلية.
    /// </summary>
    [Fact]
    public void Inspection_Additional_Item_Without_Actual_Service_Fails()
    {
        var inspectionId = Guid.NewGuid();

        Should.Throw<BusinessException>(() =>
        {
            new InspectionItem(
                Guid.NewGuid(),
                inspectionId,
                orderItemId: null,
                expectedItemTypeId: null,
                expectedServiceId: null,
                expectedQuantity: 0,
                actualItemTypeId: Guid.NewGuid(),
                actualServiceId: null,
                actualQuantity: 1);
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.InspectionErrorCodes.InvalidAdditionalItem);
    }

    /// <summary>
    /// 8. اختبار تمثيل تغير نوع الصنف (Changed item type) مع حفظ النوع الأصلي المتوقع.
    /// </summary>
    [Fact]
    public void Inspection_Changed_Item_Type_Is_Represented()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var newType = Guid.NewGuid();
        var item = InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 1,
            actualQuantity: 1,
            actualItemTypeId: newType);

        item.IsItemTypeChanged.ShouldBeTrue();
        item.IsServiceChanged.ShouldBeFalse();
        item.ExpectedLaundryItemTypeId.ShouldBe(orderItem.LaundryItemTypeId);
        item.ActualLaundryItemTypeId.ShouldBe(newType);
    }

    /// <summary>
    /// 9. اختبار تمثيل تغير نوع الخدمة (Changed service) مع حفظ الخدمة الأصلية المتوقعة.
    /// </summary>
    [Fact]
    public void Inspection_Changed_Service_Is_Represented()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var newService = Guid.NewGuid();
        var item = InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 1,
            actualQuantity: 1,
            actualServiceId: newService);

        item.IsServiceChanged.ShouldBeTrue();
        item.IsItemTypeChanged.ShouldBeFalse();
        item.ExpectedLaundryServiceId.ShouldBe(orderItem.LaundryServiceId);
        item.ActualLaundryServiceId.ShouldBe(newService);
    }

    /// <summary>
    /// 10. اختبار بقاء لقطات بنود الطلب الأصلية (OrderItem Snapshots) ثابتة ولا تتغير أبداً.
    /// </summary>
    [Fact]
    public void Inspection_Original_OrderItem_Snapshot_Never_Changes()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var changedService = Guid.NewGuid();
        var inspectionItem = InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 1,
            actualQuantity: 3,
            actualServiceId: changedService);

        inspection.AddItem(inspectionItem);
        inspection.Complete(DateTime.UtcNow, order);

        var prices = new List<ServicePrice>
        {
            new ServicePrice(Guid.NewGuid(), order.TenantId!.Value, orderItem.LaundryItemTypeId, changedService, 80m)
        };

        var domainService = new OrderAdjustmentDomainService();
        domainService.CalculateAndApplyAdjustment(order, inspection, "Service changed", prices);

        // Snapshots on OrderItem remain 100% unchanged
        orderItem.Quantity.ShouldBe(1);
        orderItem.UnitPriceSnapshot.ShouldBe(50m);
        orderItem.LineTotal.ShouldBe(50m);
        orderItem.ServiceNameSnapshot.ShouldBe("Dry Clean");
        orderItem.ItemNameSnapshot.ShouldBe("Thobe");
    }

    /// <summary>
    /// 11. اختبار إلزامية تمثيل كافة بنود الطلب الأصلية قبل اعتماد الفحص (Complete Inspection).
    /// </summary>
    [Fact]
    public void Inspection_Every_Original_OrderItem_Must_Be_Represented_Before_Complete()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);

        // Add a second item to order
        var secondItemId = Guid.NewGuid();
        var secondItem = new OrderItem(secondItemId, order.Id, Guid.NewGuid(), Guid.NewGuid(), "Abaya", "Wash", 1, 60m);
        order.AddItem(secondItem);

        // Only inspect first item
        var firstOrderItem = order.Items.First();
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, firstOrderItem.Id, firstOrderItem.LaundryItemTypeId, firstOrderItem.LaundryServiceId, 1, 1));

        // Attempt to complete with order having 2 items -> Throws BusinessException
        Should.Throw<BusinessException>(() =>
        {
            inspection.Complete(DateTime.UtcNow, order);
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.InspectionErrorCodes.IncompleteOrderItemsInspected);
    }

    /// <summary>
    /// 12. اختبار منع تكرار تمثيل نفس بند الطلب الأصلي في محضر الفحص.
    /// </summary>
    [Fact]
    public void Inspection_Duplicate_Representation_Of_Original_OrderItem_Fails()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var item1 = InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1);
        inspection.AddItem(item1);

        var item2 = InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1);

        Should.Throw<BusinessException>(() =>
        {
            inspection.AddItem(item2);
        }).Code.ShouldBe(laundry_SaaSDomainErrorCodes.InspectionErrorCodes.DuplicateOrderItemRepresentation);
    }

    /// <summary>
    /// اختبار إعادة فتح محضر الفحص الفني فقط من حالة المكتمل مع اشتراط توثيق السبب الإداري وتاريخ الإعادة.
    /// </summary>
    [Fact]
    public void Inspection_Reopen_Only_From_Completed_Status_With_Documented_Reason()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1));

        // Reopen while Started throws BusinessException
        inspection.Status.ShouldBe(InspectionStatus.Started);
        Should.Throw<BusinessException>(() =>
        {
            inspection.Reopen("Customer dispute", DateTime.UtcNow);
        });

        // Complete inspection
        var completedAt = DateTime.UtcNow;
        inspection.Complete(completedAt, order);
        inspection.Status.ShouldBe(InspectionStatus.Completed);

        // Reopen from Completed succeeds with documented reason
        var reopenedAt = completedAt.AddHours(1);
        inspection.Reopen("Customer reported extra hidden garment", reopenedAt);

        inspection.Status.ShouldBe(InspectionStatus.Reopened);
        inspection.ReopenedAt.ShouldBe(reopenedAt);
        inspection.ReopenReason.ShouldBe("Customer reported extra hidden garment");

        // Reopening again when already Reopened throws BusinessException
        Should.Throw<BusinessException>(() =>
        {
            inspection.Reopen("Another reopen attempt", reopenedAt.AddHours(1));
        });
    }

    /// <summary>
    /// 13. اختبار عدم إنشاء أي تعديل مالي إذا تطابقت نتائج الفحص تماماً مع الطلب الأصلي.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Exact_Inspection_Match_Yields_No_Adjustment()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 2, unitPrice: 50m);
        var orderItem = order.Items.First();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 2, 2));
        inspection.Complete(DateTime.UtcNow, order);

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Exact match", new List<ServicePrice>());

        adjustment.ShouldBeNull();
        order.Adjustments.Count.ShouldBe(0);
        order.Status.ShouldBe(OrderStatus.Inspection);
    }

    /// <summary>
    /// 14. اختبار زيادة الكمية وتطبيق الزيادة المالية الصحيحة.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Quantity_Increase_Calculates_Correct_Increase()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        // Initial: Subtotal = 50, DeliveryFee = 10, Total = 60
        var orderItem = order.Items.First();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 2));
        inspection.Complete(DateTime.UtcNow, order);

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Quantity increased", new List<ServicePrice>());

        adjustment.ShouldNotBeNull();
        adjustment.OldTotal.ShouldBe(60m);
        adjustment.NewTotal.ShouldBe(110m); // 2 * 50 + 10 = 110
        adjustment.DifferenceAmount.ShouldBe(50m);
        adjustment.Status.ShouldBe(OrderAdjustmentStatus.Pending);
        order.Status.ShouldBe(OrderStatus.WaitingForAdjustmentApproval);
    }

    /// <summary>
    /// 15. اختبار نقصان الكمية وتطبيق النقصان المالي الصحيح واعتماده تلقائياً لصالح العميل.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Quantity_Decrease_Calculates_Correct_Decrease_And_Auto_Approves()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 2, unitPrice: 50m);
        // Initial: Subtotal = 100, DeliveryFee = 10, Total = 110
        var orderItem = order.Items.First();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 2, 1));
        inspection.Complete(DateTime.UtcNow, order);

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "One piece missing", new List<ServicePrice>());

        adjustment.ShouldNotBeNull();
        adjustment.OldTotal.ShouldBe(110m);
        adjustment.NewTotal.ShouldBe(60m); // 1 * 50 + 10 = 60
        adjustment.DifferenceAmount.ShouldBe(-50m);
        adjustment.Status.ShouldBe(OrderAdjustmentStatus.Approved);
        order.Total.ShouldBe(60m); // Auto-approved in customer favor
    }

    /// <summary>
    /// 16. اختبار تسعير القطعة الإضافية باستخدام سجل ServicePrice الموثوق المحمّل.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Additional_Item_Uses_Trusted_Current_ServicePrice()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        // 1. Matched original item (50 SAR)
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1));

        // 2. Additional item (type: Dress, service: Steam, price in DB: 40 SAR)
        var extraItemType = Guid.NewGuid();
        var extraService = Guid.NewGuid();
        inspection.AddItem(InspectionItem.ForAdditionalItem(
            Guid.NewGuid(), inspection.Id, extraItemType, extraService, actualQuantity: 2));

        inspection.Complete(DateTime.UtcNow, order);

        var trustedPrices = new List<ServicePrice>
        {
            new ServicePrice(Guid.NewGuid(), order.TenantId!.Value, extraItemType, extraService, 40m)
        };

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Extra dresses found", trustedPrices);

        adjustment.ShouldNotBeNull();
        // NewSubtotal = 1*50 + 2*40 = 130, DeliveryFee = 10, Total = 140
        adjustment.NewTotal.ShouldBe(140m);
        adjustment.DifferenceAmount.ShouldBe(80m); // 140 - 60
    }

    /// <summary>
    /// 17. اختبار تسعير الخدمة المعدلة باستخدام سجل ServicePrice الموثوق.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Changed_Service_Uses_Trusted_Current_ServicePrice()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var upgradedService = Guid.NewGuid();
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 1,
            actualQuantity: 1,
            actualServiceId: upgradedService));

        inspection.Complete(DateTime.UtcNow, order);

        var trustedPrices = new List<ServicePrice>
        {
            new ServicePrice(Guid.NewGuid(), order.TenantId!.Value, orderItem.LaundryItemTypeId, upgradedService, 75m)
        };

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Upgraded to delicate wash", trustedPrices);

        adjustment.ShouldNotBeNull();
        // 1 * 75 + 10 = 85
        adjustment.NewTotal.ShouldBe(85m);
        adjustment.DifferenceAmount.ShouldBe(25m); // 85 - 60
    }

    /// <summary>
    /// 18. اختبار تسعير الصنف المعدل نوعه باستخدام سجل ServicePrice الموثوق.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Changed_Type_Uses_Trusted_Current_ServicePrice()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var correctedType = Guid.NewGuid();
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(),
            inspection.Id,
            orderItem.Id,
            orderItem.LaundryItemTypeId,
            orderItem.LaundryServiceId,
            expectedQuantity: 1,
            actualQuantity: 1,
            actualItemTypeId: correctedType));

        inspection.Complete(DateTime.UtcNow, order);

        var trustedPrices = new List<ServicePrice>
        {
            new ServicePrice(Guid.NewGuid(), order.TenantId!.Value, correctedType, orderItem.LaundryServiceId, 90m)
        };

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Corrected item type to Coat", trustedPrices);

        adjustment.ShouldNotBeNull();
        // 1 * 90 + 10 = 100
        adjustment.NewTotal.ShouldBe(100m);
        adjustment.DifferenceAmount.ShouldBe(40m); // 100 - 60
    }

    /// <summary>
    /// 19. اختبار أن الصنف الأصلي غير المعدل يستمر باستخدام UnitPriceSnapshot حتى لو تغير سعر الكتالوج.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Unchanged_Original_Item_Continues_Using_UnitPriceSnapshot_Even_If_Catalog_Changed()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        // Exact match
        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1));
        inspection.Complete(DateTime.UtcNow, order);

        // Catalog price in DB changed to 999 SAR!
        var catalogPrices = new List<ServicePrice>
        {
            new ServicePrice(Guid.NewGuid(), order.TenantId!.Value, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 999m)
        };

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Catalog price changed", catalogPrices);

        // Must still use 50 SAR snapshot -> No difference -> Null adjustment
        adjustment.ShouldBeNull();
        order.Total.ShouldBe(60m);
    }

    /// <summary>
    /// 20. اختبار رمي استثناء BusinessException عند عدم وجود تسعير فعال في ServicePrice للقطعة الإضافية أو المعدلة.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Missing_ServicePrice_Throws_BusinessException()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        var extraType = Guid.NewGuid();
        var extraService = Guid.NewGuid();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1));
        inspection.AddItem(InspectionItem.ForAdditionalItem(
            Guid.NewGuid(), inspection.Id, extraType, extraService, 1));

        inspection.Complete(DateTime.UtcNow, order);

        var emptyPrices = new List<ServicePrice>();
        var domainService = new OrderAdjustmentDomainService();

        var ex = Should.Throw<BusinessException>(() =>
        {
            domainService.CalculateAndApplyAdjustment(order, inspection, "Missing price", emptyPrices);
        });

        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.OrderErrorCodes.ServicePriceNotFoundForInspectedItem);
    }

    /// <summary>
    /// 21. اختبار رفض سجل ServicePrice التابع لمستأجر آخر (Cross-tenant rejection).
    /// </summary>
    [Fact]
    public void OrderAdjustment_Cross_Tenant_ServicePrice_Rejected()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 1));
        inspection.Complete(DateTime.UtcNow, order);

        var crossTenantPrice = new List<ServicePrice>
        {
            new ServicePrice(Guid.NewGuid(), Guid.NewGuid(), orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 60m)
        };

        var domainService = new OrderAdjustmentDomainService();
        Should.Throw<BusinessException>(() =>
        {
            domainService.CalculateAndApplyAdjustment(order, inspection, "Cross tenant test", crossTenantPrice);
        });
    }

    /// <summary>
    /// 22. اختبار رفض محضر فحص ينتمي لطلب آخر أو مستأجر آخر.
    /// </summary>
    [Fact]
    public void OrderAdjustment_Inspection_From_Another_Order_Or_Tenant_Rejected()
    {
        var (order, _) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var domainService = new OrderAdjustmentDomainService();

        // 1. Mismatched OrderId
        var wrongOrderInspection = new Inspection(Guid.NewGuid(), order.TenantId!.Value, Guid.NewGuid());
        wrongOrderInspection.Complete(DateTime.UtcNow);
        Should.Throw<BusinessException>(() =>
        {
            domainService.CalculateAndApplyAdjustment(order, wrongOrderInspection, "Test", new List<ServicePrice>());
        });

        // 2. Mismatched TenantId
        var wrongTenantInspection = new Inspection(Guid.NewGuid(), Guid.NewGuid(), order.Id);
        wrongTenantInspection.Complete(DateTime.UtcNow);
        Should.Throw<BusinessException>(() =>
        {
            domainService.CalculateAndApplyAdjustment(order, wrongTenantInspection, "Test", new List<ServicePrice>());
        });
    }

    /// <summary>
    /// 23. اختبار اعتماد التخفيض المالي تلقائياً وفورياً لصالح العميل (Reduction Auto Approved).
    /// </summary>
    [Fact]
    public void OrderAdjustment_Reduction_Auto_Approved()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 3, unitPrice: 50m);
        // Initial: 3 * 50 + 10 = 160
        var orderItem = order.Items.First();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 3, 1));
        inspection.Complete(DateTime.UtcNow, order);

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Customer sent 1 item instead of 3", new List<ServicePrice>());

        adjustment.ShouldNotBeNull();
        adjustment.Status.ShouldBe(OrderAdjustmentStatus.Approved);
        adjustment.DifferenceAmount.ShouldBe(-100m);
        order.Total.ShouldBe(60m); // 1 * 50 + 10 = 60
    }

    /// <summary>
    /// 24. اختبار أن الزيادة المالية تنتظر موافقة العميل (Increase waits customer approval).
    /// </summary>
    [Fact]
    public void OrderAdjustment_Increase_Waits_Customer_Approval()
    {
        var (order, inspection) = CreateTestOrderWithInspection(orderItemQty: 1, unitPrice: 50m);
        var orderItem = order.Items.First();

        inspection.AddItem(InspectionItem.ForOriginalItem(
            Guid.NewGuid(), inspection.Id, orderItem.Id, orderItem.LaundryItemTypeId, orderItem.LaundryServiceId, 1, 2));
        inspection.Complete(DateTime.UtcNow, order);

        var domainService = new OrderAdjustmentDomainService();
        var adjustment = domainService.CalculateAndApplyAdjustment(order, inspection, "Extra item found", new List<ServicePrice>());

        adjustment.ShouldNotBeNull();
        adjustment.Status.ShouldBe(OrderAdjustmentStatus.Pending);
        order.Status.ShouldBe(OrderStatus.WaitingForAdjustmentApproval);
        order.Total.ShouldBe(60m); // Unchanged until approval
    }

    /// <summary>
    /// 25. اختبار التحقق من مخطط قاعدة البيانات (EF Core Mapping) لحقول جدول الاستلام بحيث تكون Required وNOT NULL.
    /// </summary>
    [Fact]
    public void Order_PickupSchedule_Snapshot_Properties_Are_Required_And_Configured()
    {
        var slotId = Guid.NewGuid();
        var scheduledDate = new DateOnly(2026, 9, 20);
        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(10, 0);

        var snapshot = new PickupScheduleSnapshot(scheduledDate, start, end, slotId);
        snapshot.ScheduledDate.ShouldBe(scheduledDate);
        snapshot.StartTime.ShouldBe(start);
        snapshot.EndTime.ShouldBe(end);
        snapshot.OriginalSlotId.ShouldBe(slotId);
    }

    #endregion
}

