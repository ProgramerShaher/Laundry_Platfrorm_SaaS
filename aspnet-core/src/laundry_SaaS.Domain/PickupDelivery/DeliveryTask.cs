using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لمهمة توصيل وتسليم الملابس النظيفة إلى العميل وتحصيل المبلغ نقداً عند الاستلام.
/// <para>
/// يتبع هذا الكيان مستأجراً محدداً (<see cref="TenantAuditedAggregateRoot.TenantId"/>) ولا يدعم الحذف الناعم (No Soft Delete).
/// لا يتضمن النظام وحدة مدفوعات إلكترونية (No Payment Module)؛ حيث يتم تسجيل التحصيل المالي نقداً حصرياً
/// عبر كائن القيمة المدمج (<see cref="CashCollectionInfo"/>) أثناء تأكيد تسليم المهمة.
/// يدعم محاولات توصيل متعددة في حال تعثر العميل ويخضع لفهرس فريد مركب (TenantId, OrderId, AttemptNumber).
/// </para>
/// </summary>
public class DeliveryTask : TenantAuditedAggregateRoot
{
    /// <summary>
    /// معرّف الطلب الرئيسي المرتبط بمهمة التوصيل هذه.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// معرّف السائق المعين لتنفيذ مهمة التوصيل إن وجد.
    /// </summary>
    public Guid? DriverId { get; private set; }

    /// <summary>
    /// رقم محاولة التوصيل الحالية لهذا الطلب (1 للمحاولة الأولى، وتتزايد عند إعادة الجدولة).
    /// </summary>
    public int AttemptNumber { get; private set; }

    /// <summary>
    /// الحالة التشغيلية الحالية لمهمة التوصيل.
    /// </summary>
    public DeliveryTaskStatus Status { get; private set; }

    /// <summary>
    /// بداية النافذة الزمنية المجدولة لتوصيل الطلب للعميل.
    /// </summary>
    public DateTime ScheduledFrom { get; private set; }

    /// <summary>
    /// نهاية النافذة الزمنية المجدولة لتوصيل الطلب للعميل.
    /// </summary>
    public DateTime ScheduledTo { get; private set; }

    /// <summary>
    /// تاريخ ووقت تعيين المهمة للسائق.
    /// </summary>
    public DateTime? AssignedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت قبول السائق لمهمة التوصيل.
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت استلام السائق للملابس النظيفة والمغلفة من المغسلة.
    /// </summary>
    public DateTime? PickedUpFromLaundryAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت انطلاق السائق في الطريق إلى موقع العميل (OutForDelivery).
    /// </summary>
    public DateTime? OutForDeliveryAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت وصول السائق الفعلي إلى موقع العميل.
    /// </summary>
    public DateTime? ArrivedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت تسليم الملابس للعميل واستلام المبلغ المالي بنجاح.
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت تعثر أو فشل محاولة التوصيل.
    /// </summary>
    public DateTime? FailedAt { get; private set; }

    /// <summary>
    /// سبب فشل أو تعثر محاولة التوصيل الموثق عند تسجيل الفشل.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// كائن القيمة المدمج (Value Object) الذي يوثق تفاصيل التحصيل النقدي المطلوب والمسلم فعلياً (Cash On Delivery).
    /// </summary>
    public CashCollectionInfo CashCollectionInfo { get; private set; } = null!;

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private DeliveryTask()
    {
    }

    /// <summary>
    /// يُنشئ مهمة توصيل جديدة لطلب محدد مع تحديد مبلغ التحصيل النقدي المطلوب.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="orderId">معرّف الطلب.</param>
    /// <param name="attemptNumber">رقم المحاولة.</param>
    /// <param name="scheduledFrom">بداية نافذة الجدولة.</param>
    /// <param name="scheduledTo">نهاية نافذة الجدولة.</param>
    /// <param name="cashAmountToCollect">المبلغ النقدي المطلوب تحصيله من العميل.</param>
    /// <param name="driverId">معرّف السائق المعين إن وجد.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف الطلب فارغاً أو رقم المحاولة أقل من أو يساوي صفر.</exception>
    public DeliveryTask(
        Guid id,
        Guid tenantId,
        Guid orderId,
        int attemptNumber,
        DateTime scheduledFrom,
        DateTime scheduledTo,
        decimal cashAmountToCollect,
        Guid? driverId = null)
        : base(id, tenantId)
    {
        if (orderId == Guid.Empty)
        {
            throw new BusinessException("OrderId must not be empty.");
        }

        if (attemptNumber <= 0)
        {
            throw new BusinessException("AttemptNumber must be greater than zero.");
        }

        OrderId = orderId;
        AttemptNumber = attemptNumber;
        ScheduledFrom = scheduledFrom;
        ScheduledTo = scheduledTo;
        DriverId = driverId;
        Status = DeliveryTaskStatus.Assigned;
        AssignedAt = driverId.HasValue ? DateTime.UtcNow : null;
        CashCollectionInfo = new CashCollectionInfo(cashAmountToCollect);
    }

    /// <summary>
    /// يعين سائقاً لمهمة التوصيل ويوثق توقيت التعيين.
    /// </summary>
    /// <param name="driverId">معرّف السائق.</param>
    /// <param name="assignedAt">تاريخ ووقت التعيين.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف السائق فارغاً.</exception>
    /// <summary>
    /// يعين سائقاً لمهمة التوصيل ويوثق توقيت التعيين، ويمنع إعادة تعيين المهام المنتهية.
    /// </summary>
    /// <param name="driverId">معرّف السائق.</param>
    /// <param name="assignedAt">تاريخ ووقت التعيين.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف السائق فارغاً أو كانت المهمة في حالة نهائية.</exception>
    public void Assign(Guid driverId, DateTime assignedAt)
    {
        if (driverId == Guid.Empty)
        {
            throw new BusinessException("DriverId must not be empty.");
        }

        if (Status == DeliveryTaskStatus.Delivered || Status == DeliveryTaskStatus.Failed || Status == DeliveryTaskStatus.Cancelled)
        {
            throw new BusinessException($"Cannot reassign a delivery task that is already in terminal status '{Status}'.");
        }

        DriverId = driverId;
        AssignedAt = assignedAt;
        Status = DeliveryTaskStatus.Assigned;
    }

    /// <summary>
    /// يسجل قبول السائق لمهمة التوصيل بعد تعيينها (فقط من حالة Assigned).
    /// </summary>
    /// <param name="acceptedAt">تاريخ ووقت القبول.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن المهمة في حالة Assigned.</exception>
    public void Accept(DateTime acceptedAt)
    {
        if (Status != DeliveryTaskStatus.Assigned)
        {
            throw new BusinessException("Task must be in Assigned status to be accepted.");
        }

        Status = DeliveryTaskStatus.Accepted;
        AcceptedAt = acceptedAt;
    }

    /// <summary>
    /// يسجل استلام السائق للملابس الجاهزة من المغسلة وتوقيت ذلك (فقط من حالة Accepted).
    /// </summary>
    /// <param name="pickedUpAt">تاريخ ووقت الاستلام من المغسلة.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن المهمة مقبولة من السائق أولاً.</exception>
    public void PickupFromLaundry(DateTime pickedUpAt)
    {
        if (Status != DeliveryTaskStatus.Accepted)
        {
            throw new BusinessException("Task must be accepted before picking up from laundry facility.");
        }

        Status = DeliveryTaskStatus.PickedUpFromLaundry;
        PickedUpFromLaundryAt = pickedUpAt;
    }

    /// <summary>
    /// يسجل انطلاق السائق في رحلة التوصيل نحو موقع العميل (فقط من حالة PickedUpFromLaundry).
    /// </summary>
    /// <param name="outAt">تاريخ ووقت الانطلاق.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن الملابس قد استلمت من المغسلة بعد.</exception>
    public void StartDelivery(DateTime outAt)
    {
        if (Status != DeliveryTaskStatus.PickedUpFromLaundry)
        {
            throw new BusinessException("Items must be picked up from laundry facility before starting delivery trip.");
        }

        Status = DeliveryTaskStatus.OutForDelivery;
        OutForDeliveryAt = outAt;
    }

    /// <summary>
    /// يسجل وصول السائق إلى موقع العميل (فقط من حالة OutForDelivery).
    /// </summary>
    /// <param name="arrivedAt">تاريخ ووقت الوصول.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن السائق في حالة انطلاق (OutForDelivery).</exception>
    public void Arrive(DateTime arrivedAt)
    {
        if (Status != DeliveryTaskStatus.OutForDelivery)
        {
            throw new BusinessException("Task must be OutForDelivery before marking as arrived.");
        }

        Status = DeliveryTaskStatus.Arrived;
        ArrivedAt = arrivedAt;
    }

    /// <summary>
    /// يوثق استلام المبلغ المالي نقداً من العميل وتحديث كائن التحصيل النقدي المدمج.
    /// </summary>
    /// <param name="amount">المبلغ المحصل فعلياً.</param>
    /// <param name="driverId">معرّف السائق الذي استلم المبلغ.</param>
    /// <param name="collectedAt">تاريخ ووقت التحصيل.</param>
    public void ConfirmCashCollection(decimal amount, Guid driverId, DateTime collectedAt)
    {
        CashCollectionInfo.MarkAsCollected(amount, driverId, collectedAt);
    }

    /// <summary>
    /// يؤكد إتمام تسليم الملابس للعميل بنجاح بعد وصول السائق والتحقق الصارم من استيفاء التحصيل النقدي (COD).
    /// </summary>
    /// <param name="deliveredAt">تاريخ ووقت التسليم النهائي.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن السائق في حالة Arrived أو لم يستوفِ التحصيل النقدي كاملاً.</exception>
    public void ConfirmDelivery(DateTime deliveredAt)
    {
        if (Status != DeliveryTaskStatus.Arrived)
        {
            throw new BusinessException("Task must be in Arrived status before confirming delivery.");
        }

        if (!CashCollectionInfo.IsCollected || CashCollectionInfo.CollectedAmount != CashCollectionInfo.AmountToCollect)
        {
            throw new BusinessException($"Cannot confirm delivery without full cash collection. Collected: {CashCollectionInfo.CollectedAmount}, Required: {CashCollectionInfo.AmountToCollect}.");
        }

        Status = DeliveryTaskStatus.Delivered;
        DeliveredAt = deliveredAt;
    }

    /// <summary>
    /// يسجل تعثر أو فشل محاولة التوصيل مع توثيق السبب وفشل التحصيل النقدي المقترن به (فقط من الحالات النشطة).
    /// </summary>
    /// <param name="reason">سبب تعثر التوصيل.</param>
    /// <param name="failedAt">تاريخ ووقت الفشل.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المهمة قد سلمت أو ألغيت مسبقاً.</exception>
    public void Fail(string reason, DateTime failedAt)
    {
        if (Status == DeliveryTaskStatus.Delivered || Status == DeliveryTaskStatus.Cancelled || Status == DeliveryTaskStatus.Failed)
        {
            throw new BusinessException($"Cannot mark delivery task as failed when already in terminal status '{Status}'.");
        }

        Status = DeliveryTaskStatus.Failed;
        FailureReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
        FailedAt = failedAt;
        CashCollectionInfo.MarkAsFailed(reason);
    }

    /// <summary>
    /// يلغي مهمة التوصيل لأسباب إدارية قبل تسليمها (فقط من الحالات القابلة للإلغاء).
    /// </summary>
    /// <param name="reason">سبب الإلغاء.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المهمة قد سلمت بالفعل أو ألغيت مسبقاً.</exception>
    public void Cancel(string reason)
    {
        if (Status == DeliveryTaskStatus.Delivered || Status == DeliveryTaskStatus.Cancelled || Status == DeliveryTaskStatus.Failed)
        {
            throw new BusinessException($"Cannot cancel a delivery task in status '{Status}'.");
        }

        Status = DeliveryTaskStatus.Cancelled;
        FailureReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
    }
}
