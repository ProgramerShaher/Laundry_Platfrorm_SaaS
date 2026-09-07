using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using laundry_SaaS.Common;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp;

namespace laundry_SaaS.Orders;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) الرئيسي للطلب داخل المنصة، وهو المصدر الحصري والموثوق (Source of Truth)
/// لكافة حالات الطلب التشغيلية، ومراحل الغسيل والمعالجة، والعمليات الحسابية والمالية، وسجلات تتبع التاريخ.
/// <para>
/// يتبع الطلب مستأجراً محدداً (<see cref="TenantAuditedAggregateRoot.TenantId"/>) ولا يدعم الحذف الناعم (No Soft Delete) لحفظ النزاهة المالية والقانونية.
/// يعتمد النظام نموذج الدفع نقداً عند الاستلام (Cash On Delivery) حصرياً في هذه النسخة دون بوابات دفع إلكتروني.
/// ترتبط به كائنات تابعة متعددة تدير بنود الطلب، وتعديلات الأسعار، وسجلات الانتقال بين الحالات ومراحل المعالجة.
/// </para>
/// </summary>
public class Order : TenantAuditedAggregateRoot
{
    /// <summary>
    /// رقم تسلسلي مميز وفريد للطلب على مستوى المغسلة (TenantId, OrderNumber).
    /// </summary>
    public string OrderNumber { get; private set; } = null!;

    /// <summary>
    /// معرّف العميل صاحب الطلب (مرجع لكيان Customer العام في المنصة).
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// معرّف المغسلة المنفذة للخدمة (مرجع لكيان Laundry التابع للمستأجر).
    /// </summary>
    public Guid LaundryId { get; private set; }

    /// <summary>
    /// الحالة العامة الحالية للطلب في دورة حياته الكاملة.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// المرحلة التشغيلية الحالية للغسيل والمعالجة داخل المغسلة.
    /// <para>
    /// قاعدة النطاق الصارمة: تكون هذه القيمة <c>null</c> دائماً إذا كانت <see cref="Status"/> لا تساوي <see cref="OrderStatus.Processing"/>،
    /// وتكون إحدى قيم <see cref="ProcessingStage"/> حصراً أثناء معالجة الطلب داخل المغسلة.
    /// </para>
    /// </summary>
    public ProcessingStage? CurrentProcessingStage { get; private set; }

    /// <summary>
    /// لقطة تاريخية ثابتة (AddressSnapshot) لعنوان استلام الملابس من العميل، تم حفظها وقت تأكيد الطلب.
    /// </summary>
    public AddressSnapshot PickupAddress { get; private set; } = null!;

    /// <summary>
    /// لقطة تاريخية ثابتة (AddressSnapshot) لعنوان توصيل وتسليم الملابس النظيفة للعميل، تم حفظها وقت تأكيد الطلب.
    /// </summary>
    public AddressSnapshot DeliveryAddress { get; private set; } = null!;

    /// <summary>
    /// معرّف الفترة الزمنية المجدولة لاستلام الملابس من العميل إن وُجدت.
    /// </summary>
    public Guid? PickupSlotId { get; private set; }

    /// <summary>
    /// معرّف الفترة الزمنية المجدولة لتوصيل الملابس النظيفة للعميل إن وُجدت.
    /// </summary>
    public Guid? DeliverySlotId { get; private set; }

    /// <summary>
    /// المجموع الفرعي لقيمة بنود الملابس والخدمات المطلوبة قبل إضافة رسوم التوصيل والخصومات (محسوب في Backend).
    /// </summary>
    public decimal Subtotal { get; private set; }

    /// <summary>
    /// رسوم التوصيل المعتمدة لهذا الطلب بناءً على سياسة المغسلة وقت إنشاء الطلب.
    /// </summary>
    public decimal DeliveryFee { get; private set; }

    /// <summary>
    /// قيمة الخصم المالي المطبق على الطلب إن وُجد.
    /// </summary>
    public decimal Discount { get; private set; }

    /// <summary>
    /// المبلغ الإجمالي النهائي المطلوب سداده نقداً عند الاستلام (COD): (Subtotal + DeliveryFee - Discount).
    /// يتم حسابه حصرياً داخل النظام ولا يتم الاعتماد على أي قيمة مرسلة من واجهة العميل.
    /// </summary>
    public decimal Total { get; private set; }

    /// <summary>
    /// ملاحظات العميل الخاصة بالطلب وإرشادات العناية بالملابس.
    /// </summary>
    public string? CustomerNotes { get; private set; }

    /// <summary>
    /// تاريخ ووقت إلغاء الطلب في حال تم إلغاؤه.
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// سبب إلغاء الطلب الموثق عند الإلغاء.
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// مجموعة بنود وأصناف الملابس المسجلة في الطلب (Child Entities).
    /// </summary>
    public virtual ICollection<OrderItem> Items { get; protected set; }

    /// <summary>
    /// مجموعة التعديلات المالية والكمية المقترحة على الطلب بعد الفحص (Child Entities).
    /// </summary>
    public virtual ICollection<OrderAdjustment> Adjustments { get; protected set; }

    /// <summary>
    /// سجل تاريخي غير قابل للتعديل (Append-Only) لجميع التحولات التي طرأت على حالة الطلب.
    /// </summary>
    public virtual ICollection<OrderStatusHistory> StatusHistories { get; protected set; }

    /// <summary>
    /// سجل تاريخي غير قابل للتعديل (Append-Only) لمراحل المعالجة والغسيل الداخلية للطلب داخل المغسلة.
    /// </summary>
    public virtual ICollection<ProcessingStageHistory> ProcessingStageHistories { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Order()
    {
        Items = new Collection<OrderItem>();
        Adjustments = new Collection<OrderAdjustment>();
        StatusHistories = new Collection<OrderStatusHistory>();
        ProcessingStageHistories = new Collection<ProcessingStageHistory>();
    }

    /// <summary>
    /// يُنشئ طلباً جديداً بحالة مسودة (Draft) مع تثبيت لقطات العناوين وتوثيق الحركة الأولى في السجل التاريخي.
    /// </summary>
    /// <param name="id">المعرّف الفريد للطلب.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="orderNumber">رقم الطلب المميز.</param>
    /// <param name="customerId">معرّف العميل.</param>
    /// <param name="laundryId">معرّف المغسلة.</param>
    /// <param name="pickupAddress">لقطة عنوان الاستلام.</param>
    /// <param name="deliveryAddress">لقطة عنوان التوصيل.</param>
    /// <param name="deliveryFee">رسوم التوصيل.</param>
    /// <param name="discount">قيمة الخصم.</param>
    /// <param name="pickupSlotId">معرّف فترة الاستلام.</param>
    /// <param name="deliverySlotId">معرّف فترة التوصيل.</param>
    /// <param name="customerNotes">ملاحظات العميل.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المعرفات الأساسية فارغة.</exception>
    public Order(
        Guid id,
        Guid tenantId,
        string orderNumber,
        Guid customerId,
        Guid laundryId,
        AddressSnapshot pickupAddress,
        AddressSnapshot deliveryAddress,
        decimal deliveryFee = 0,
        decimal discount = 0,
        Guid? pickupSlotId = null,
        Guid? deliverySlotId = null,
        string? customerNotes = null)
        : base(id, tenantId)
    {
        if (customerId == Guid.Empty)
        {
            throw new BusinessException("CustomerId must not be empty.");
        }

        if (laundryId == Guid.Empty)
        {
            throw new BusinessException("LaundryId must not be empty.");
        }

        OrderNumber = Check.NotNullOrWhiteSpace(orderNumber, nameof(orderNumber), maxLength: 50);
        CustomerId = customerId;
        LaundryId = laundryId;
        PickupAddress = Check.NotNull(pickupAddress, nameof(pickupAddress));
        DeliveryAddress = Check.NotNull(deliveryAddress, nameof(deliveryAddress));
        PickupSlotId = pickupSlotId;
        if (deliveryFee < 0)
        {
            throw new BusinessException("DeliveryFee must not be negative.");
        }

        if (discount < 0)
        {
            throw new BusinessException("Discount must not be negative.");
        }

        DeliveryFee = deliveryFee;
        Discount = discount;
        Subtotal = 0;
        Total = 0;

        Items = new Collection<OrderItem>();
        Adjustments = new Collection<OrderAdjustment>();
        StatusHistories = new Collection<OrderStatusHistory>();
        ProcessingStageHistories = new Collection<ProcessingStageHistory>();

        RecalculateTotals();

        Status = OrderStatus.Draft;
        CurrentProcessingStage = null;

        RecordStatusTransition(null, OrderStatus.Draft, "Order created.");
    }

    /// <summary>
    /// يضيف بند ملابس جديد إلى الطلب ويعيد احتساب الإجماليات المالية تلقائياً.
    /// </summary>
    /// <param name="item">كيان البند المراد إضافته.</param>
    public void AddItem(OrderItem item)
    {
        Check.NotNull(item, nameof(item));
        Items.Add(item);
        RecalculateTotals();
    }

    /// <summary>
    /// يعيد احتساب المجموع الفرعي والإجمالي النهائي للطلب بناءً على بنود الطلب ورسوم التوصيل والخصم.
    /// يضمن ألا يتجاوز الخصم مجموع البنود ورسوم التوصيل حتى لا يصبح الإجمالي سالباً.
    /// </summary>
    public void RecalculateTotals()
    {
        Subtotal = Items.Sum(x => x.LineTotal);
        if (Discount > Subtotal + DeliveryFee)
        {
            throw new BusinessException("Discount cannot exceed the sum of Subtotal and DeliveryFee.");
        }
        Total = Subtotal + DeliveryFee - Discount;
    }

    /// <summary>
    /// ينقل الطلب إلى حالة بانتظار الاستلام (PendingPickup) بعد تأكيده من العميل.
    /// يُشترط أن يكون الطلب في حالة مسودة (Draft).
    /// </summary>
    public void MarkAsPendingPickup()
    {
        EnsureStatus(OrderStatus.Draft, nameof(MarkAsPendingPickup));
        ChangeStatus(OrderStatus.PendingPickup, "Order placed, pending pickup.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة تعيين سائق الاستلام (PickupAssigned).
    /// يُسمح بذلك من حالة بانتظار الاستلام (PendingPickup) أو بعد تعثر محاولة استلام سابقة (PickupFailed).
    /// </summary>
    public void MarkAsPickupAssigned()
    {
        EnsureStatusIn(nameof(MarkAsPickupAssigned), OrderStatus.PendingPickup, OrderStatus.PickupFailed);
        ChangeStatus(OrderStatus.PickupAssigned, "Driver assigned for pickup.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة خروج السائق للاستلام (OutForPickup).
    /// يُشترط أن يكون الطلب في حالة تعيين السائق (PickupAssigned).
    /// </summary>
    public void MarkAsOutForPickup()
    {
        EnsureStatus(OrderStatus.PickupAssigned, nameof(MarkAsOutForPickup));
        ChangeStatus(OrderStatus.OutForPickup, "Driver out for pickup.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة استلام الملابس من العميل (PickedUp).
    /// يُشترط أن يكون السائق في طريقه للاستلام (OutForPickup).
    /// </summary>
    public void MarkAsPickedUp()
    {
        EnsureStatus(OrderStatus.OutForPickup, nameof(MarkAsPickedUp));
        ChangeStatus(OrderStatus.PickedUp, "Items picked up from customer.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة وصول الملابس إلى مقر المغسلة (ReceivedAtLaundry).
    /// يُشترط أن تكون الملابس قد تم استلامها من العميل مسبقاً (PickedUp).
    /// </summary>
    public void MarkAsReceivedAtLaundry()
    {
        EnsureStatus(OrderStatus.PickedUp, nameof(MarkAsReceivedAtLaundry));
        ChangeStatus(OrderStatus.ReceivedAtLaundry, "Items received at laundry facility.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة الفحص والمعاينة الفنية (Inspection).
    /// يُشترط أن تكون الملابس قد وصلت بالفعل إلى المغسلة (ReceivedAtLaundry).
    /// </summary>
    public void StartInspection()
    {
        EnsureStatus(OrderStatus.ReceivedAtLaundry, nameof(StartInspection));
        ChangeStatus(OrderStatus.Inspection, "Item inspection started.");
    }

    /// <summary>
    /// يعلق الطلب بانتظار موافقة العميل على تعديل السعر أو البنود (WaitingForAdjustmentApproval).
    /// يُشترط أن يكون الطلب في مرحلة الفحص (Inspection).
    /// </summary>
    public void RequireAdjustmentApproval()
    {
        EnsureStatus(OrderStatus.Inspection, nameof(RequireAdjustmentApproval));
        ChangeStatus(OrderStatus.WaitingForAdjustmentApproval, "Order adjustment requires customer approval.");
    }

    /// <summary>
    /// يبدأ مرحلة المعالجة والغسيل الفعلية للطلب وينشئ أول سجل في تاريخ مراحل المعالجة.
    /// يُشترط أن يكون الطلب قد اجتاز الفحص (Inspection) أو اعتمد تعديله (WaitingForAdjustmentApproval).
    /// تبدأ المعالجة دائماً بمرحلة الفرز (Sorting).
    /// </summary>
    /// <param name="initialStage">المرحلة الأولى للمعالجة (يجب أن تكون Sorting حصراً).</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت الحالة غير مسموحة أو المرحلة الأولى غير الفرز.</exception>
    public void StartProcessing(ProcessingStage initialStage = ProcessingStage.Sorting)
    {
        EnsureStatusIn(nameof(StartProcessing), OrderStatus.Inspection, OrderStatus.WaitingForAdjustmentApproval);

        if (initialStage != ProcessingStage.Sorting)
        {
            throw new BusinessException("Order processing must always start at the Sorting stage.");
        }

        ChangeStatus(OrderStatus.Processing, "Order processing started.");
        CurrentProcessingStage = ProcessingStage.Sorting;
        ProcessingStageHistories.Add(new ProcessingStageHistory(Guid.NewGuid(), Id, null, ProcessingStage.Sorting));
    }

    /// <summary>
    /// ينقل معالجة الطلب إلى مرحلة تالية داخل المغسلة وفق تسلسل صارم:
    /// <c>Sorting (0) -> Washing (1) -> Drying (2) -> Ironing (3) -> Folding (4) -> Packaging (5)</c>.
    /// يُمنع القفز بين المراحل أو الرجوع للوراء.
    /// </summary>
    /// <param name="nextStage">المرحلة التالية المباشرة للمعالجة.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن الطلب في حالة Processing أو كانت المرحلة غير متتالية مباشرة.</exception>
    public void MoveToProcessingStage(ProcessingStage nextStage)
    {
        EnsureStatus(OrderStatus.Processing, nameof(MoveToProcessingStage));

        if (!CurrentProcessingStage.HasValue)
        {
            throw new BusinessException("CurrentProcessingStage is not set.");
        }

        if ((int)nextStage != (int)CurrentProcessingStage.Value + 1)
        {
            throw new BusinessException($"Invalid processing stage transition from '{CurrentProcessingStage.Value}' to '{nextStage}'. Stages must follow the exact sequence: Sorting -> Washing -> Drying -> Ironing -> Folding -> Packaging.");
        }

        var oldStage = CurrentProcessingStage;
        CurrentProcessingStage = nextStage;
        ProcessingStageHistories.Add(new ProcessingStageHistory(Guid.NewGuid(), Id, oldStage, nextStage));
    }

    /// <summary>
    /// ينقل الطلب إلى حالة الجاهزية للتوصيل (ReadyForDelivery) بعد الانتهاء من المعالجة والتغليف.
    /// يُشترط أن يكون الطلب في حالة Processing وأن تكون المرحلة الحالية هي التغليف (Packaging).
    /// </summary>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن المعالجة قد وصلت لمرحلة التغليف بعد.</exception>
    public void MarkAsReadyForDelivery()
    {
        EnsureStatus(OrderStatus.Processing, nameof(MarkAsReadyForDelivery));

        if (CurrentProcessingStage != ProcessingStage.Packaging)
        {
            throw new BusinessException($"Cannot mark order as ready for delivery. Current processing stage is '{CurrentProcessingStage}', but must be '{ProcessingStage.Packaging}'.");
        }

        ChangeStatus(OrderStatus.ReadyForDelivery, "Processing completed, ready for delivery.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة خروج السائق للتوصيل (OutForDelivery).
    /// يُسمح بذلك من حالة الجاهزية للتوصيل (ReadyForDelivery) أو بعد تعثر محاولة توصيل سابقة (DeliveryFailed).
    /// </summary>
    public void MarkAsOutForDelivery()
    {
        EnsureStatusIn(nameof(MarkAsOutForDelivery), OrderStatus.ReadyForDelivery, OrderStatus.DeliveryFailed);
        ChangeStatus(OrderStatus.OutForDelivery, "Order out for delivery.");
    }

    /// <summary>
    /// ينقل الطلب إلى حالة تم التسليم للعميل بنجاح وتحصيل المبلغ نقداً (Delivered).
    /// يُشترط أن يكون السائق قد خرج للتوصيل (OutForDelivery).
    /// </summary>
    public void MarkAsDelivered()
    {
        EnsureStatus(OrderStatus.OutForDelivery, nameof(MarkAsDelivered));
        ChangeStatus(OrderStatus.Delivered, "Order delivered to customer.");
    }

    /// <summary>
    /// يغلق الطلب نهائياً ومكتملاً (Completed).
    /// يُشترط حصراً أن يكون الطلب قد سُلّم بنجاح للعميل (Delivered).
    /// </summary>
    public void Complete()
    {
        EnsureStatus(OrderStatus.Delivered, nameof(Complete));
        ChangeStatus(OrderStatus.Completed, "Order fulfilled and completed.");
    }

    /// <summary>
    /// يسجل تعثر محاولة استلام الطلب من العميل (PickupFailed) مع توثيق السبب.
    /// يُسمح فقط أثناء محاولات الاستلام النشطة (PickupAssigned أو OutForPickup).
    /// </summary>
    /// <param name="reason">سبب تعثر الاستلام.</param>
    public void MarkAsPickupFailed(string reason)
    {
        EnsureStatusIn(nameof(MarkAsPickupFailed), OrderStatus.PickupAssigned, OrderStatus.OutForPickup);
        ChangeStatus(OrderStatus.PickupFailed, reason);
    }

    /// <summary>
    /// يسجل تعثر محاولة توصيل الطلب للعميل (DeliveryFailed) مع توثيق السبب.
    /// يُسمح فقط أثناء محاولة التوصيل النشطة (OutForDelivery).
    /// </summary>
    /// <param name="reason">سبب تعثر التوصيل.</param>
    public void MarkAsDeliveryFailed(string reason)
    {
        EnsureStatus(OrderStatus.OutForDelivery, nameof(MarkAsDeliveryFailed));
        ChangeStatus(OrderStatus.DeliveryFailed, reason);
    }

    /// <summary>
    /// يلغي الطلب ذاتياً بواسطة العميل قبل بدء رحلة السائق للاستلام.
    /// الحالات المسموح بإلغائها ذاتياً: Draft، PendingPickup، PickupAssigned (وكذلك PickupFailed قبل استلام الملابس).
    /// يُمنع الإلغاء الذاتي بمجرد انطلاق السائق (OutForPickup) أو بعد استلام الملابس في عهدة السائق أو المغسلة، كما يُمنع إلغاء الحالات النهائية.
    /// </summary>
    /// <param name="reason">سبب الإلغاء المقدم من العميل.</param>
    /// <param name="cancelledAt">تاريخ ووقت طلب الإلغاء.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت حالة الطلب لا تسمح بالإلغاء الذاتي للعميل.</exception>
    public void Cancel(string reason, DateTime cancelledAt)
    {
        EnsureStatusIn(
            nameof(Cancel),
            OrderStatus.Draft,
            OrderStatus.PendingPickup,
            OrderStatus.PickupAssigned,
            OrderStatus.PickupFailed);

        ApplyCancellation(reason, cancelledAt);
    }

    /// <summary>
    /// يلغي الطلب بقرار إداري أو تشغيلي من إدارة المغسلة (Administrative/Operational Cancellation).
    /// تُستدعى هذه الدالة حصراً من طبقة التطبيق (Application Layer) بعد التحقق الصارم من صلاحيات المشرف أو إدارة المغسلة.
    /// يُسمح بها للتعامل الاستثنائي مع الحالات التشغيلية قبل التسليم (مثل OutForPickup، PickedUp، ReceivedAtLaundry، إلخ).
    /// يُمنع منعاً باتاً إلغاء الطلبات المسلمة (Delivered) أو المكتملة (Completed) أو الملغاة مسبقاً (Cancelled).
    /// </summary>
    /// <param name="reason">السبب الإداري أو التشغيلي للإلغاء.</param>
    /// <param name="cancelledAt">تاريخ ووقت اعتماد الإلغاء الإداري.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان الطلب في حالة نهائية (Delivered, Completed, Cancelled).</exception>
    public void CancelByLaundry(string reason, DateTime cancelledAt)
    {
        if (Status == OrderStatus.Completed || Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
        {
            throw new BusinessException($"Cannot administratively cancel an order that is already in terminal status '{Status}'.");
        }

        ApplyCancellation(reason, cancelledAt);
    }

    /// <summary>
    /// يطبق الإلغاء ويوثق سبب وتاريخ ووقت الإلغاء ويسجل الانتقال في تاريخ حالات الطلب.
    /// </summary>
    private void ApplyCancellation(string reason, DateTime cancelledAt)
    {
        CancelledAt = cancelledAt;
        CancellationReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
        ChangeStatus(OrderStatus.Cancelled, reason);
    }

    /// <summary>
    /// ينشئ مقترح تعديل مالي جديد على الطلب نتيجة الفحص والمعاينة.
    /// إذا كان التعديل يتضمن زيادة في السعر (NewTotal > Total) ينتقل الطلب إلى WaitingForAdjustmentApproval بانتظار موافقة العميل.
    /// إذا كان التعديل يتضمن انخفاضاً في السعر (NewTotal &lt; Total) يُعتمد التعديل تلقائياً وفورياً لصالح العميل.
    /// </summary>
    /// <param name="reason">سبب التعديل المالي.</param>
    /// <param name="newTotal">المبلغ الإجمالي الجديد المقترح للطلب.</param>
    /// <returns>كيان التعديل المُنشأ.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا كان الطلب في حالة لا تسمح بالتعديل أو كان المبلغ الجديد سالباً أو مطابقاً للسعر الحالي.</exception>
    public OrderAdjustment CreateAdjustment(string reason, decimal newTotal)
    {
        EnsureStatusIn(nameof(CreateAdjustment), OrderStatus.Inspection, OrderStatus.WaitingForAdjustmentApproval);

        if (newTotal < 0)
        {
            throw new BusinessException("NewTotal must not be negative.");
        }

        if (newTotal == Total)
        {
            throw new BusinessException("OrderAdjustment requires a change in total amount.");
        }

        if (newTotal > Total)
        {
            var adjustment = new OrderAdjustment(Guid.NewGuid(), Id, reason, Total, newTotal);
            Adjustments.Add(adjustment);
            ChangeStatus(OrderStatus.WaitingForAdjustmentApproval, "Adjustment created (price increase), awaiting customer approval.");
            return adjustment;
        }
        else
        {
            // Price decrease: auto-approved in MVP in customer favor
            var adjustment = new OrderAdjustment(Guid.NewGuid(), Id, reason, Total, newTotal);
            adjustment.Approve(DateTime.UtcNow);
            Adjustments.Add(adjustment);
            Total = newTotal;
            RecordStatusTransition(Status, Status, "Order total auto-adjusted (price reduction in customer favor).");
            return adjustment;
        }
    }

    /// <summary>
    /// يعتمد تعديل زيادة السعر المعلق بعد موافقة العميل ويحدث إجمالي الطلب ويعيد الطلب إلى Inspection ليكون جاهزاً للمعالجة.
    /// </summary>
    /// <param name="adjustmentId">معرّف التعديل المطلوب اعتماده.</param>
    /// <param name="approvedAt">تاريخ ووقت الاعتماد.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن الطلب معلقاً على التعديل أو لم يُعثر على التعديل.</exception>
    public void ApproveAdjustment(Guid adjustmentId, DateTime approvedAt)
    {
        EnsureStatus(OrderStatus.WaitingForAdjustmentApproval, nameof(ApproveAdjustment));

        var adjustment = Adjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adjustment == null)
        {
            throw new BusinessException($"Adjustment with id '{adjustmentId}' not found on this order.");
        }

        adjustment.Approve(approvedAt);
        Total = adjustment.NewTotal;
        ChangeStatus(OrderStatus.Inspection, "Order adjustment approved by customer. Ready for processing.");
    }

    /// <summary>
    /// يسجل رفض العميل لزيادة السعر المقترحة ويضع الطلب في حالة OnHold للمراجعة الإدارية دون تعديل السعر.
    /// </summary>
    /// <param name="adjustmentId">معرّف التعديل المرفوض.</param>
    /// <param name="rejectedAt">تاريخ ووقت الرفض.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن الطلب في حالة WaitingForAdjustmentApproval أو لم يُعثر على التعديل.</exception>
    public void RejectAdjustment(Guid adjustmentId, DateTime rejectedAt)
    {
        EnsureStatus(OrderStatus.WaitingForAdjustmentApproval, nameof(RejectAdjustment));

        var adjustment = Adjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adjustment == null)
        {
            throw new BusinessException($"Adjustment with id '{adjustmentId}' not found on this order.");
        }

        adjustment.Reject(rejectedAt);
        ChangeStatus(OrderStatus.OnHold, "Order adjustment rejected by customer. Order placed on hold.");
    }

    /// <summary>
    /// يتحقق من أن حالة الطلب الحالية مطابقة للحالة المطلوبة لتنفيذ العملية.
    /// </summary>
    private void EnsureStatus(OrderStatus requiredStatus, string operationName)
    {
        if (Status != requiredStatus)
        {
            throw new BusinessException($"Cannot perform '{operationName}' on order in status '{Status}'. Required status is '{requiredStatus}'.");
        }
    }

    /// <summary>
    /// يتحقق من أن حالة الطلب الحالية ضمن قائمة الحالات المسموحة لتنفيذ العملية.
    /// </summary>
    private void EnsureStatusIn(string operationName, params OrderStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
        {
            var allowedStr = string.Join(", ", allowedStatuses);
            throw new BusinessException($"Cannot perform '{operationName}' on order in status '{Status}'. Allowed statuses are: [{allowedStr}].");
        }
    }

    /// <summary>
    /// يغير حالة الطلب داخلياً، ويصفر مرحلة المعالجة تلقائياً إذا خرجت الحالة من Processing، ويوثق الانتقال في السجل التاريخي.
    /// </summary>
    private void ChangeStatus(OrderStatus newStatus, string? reason)
    {
        var oldStatus = Status;
        Status = newStatus;

        if (newStatus != OrderStatus.Processing)
        {
            CurrentProcessingStage = null;
        }

        RecordStatusTransition(oldStatus, newStatus, reason);
    }

    /// <summary>
    /// يضيف سجلاً جديداً غير قابل للتعديل إلى تاريخ تحولات حالة الطلب (Append-Only Audit Log).
    /// </summary>
    private void RecordStatusTransition(OrderStatus? fromStatus, OrderStatus toStatus, string? reason)
    {
        StatusHistories.Add(new OrderStatusHistory(Guid.NewGuid(), Id, fromStatus, toStatus, reason));
    }
}
