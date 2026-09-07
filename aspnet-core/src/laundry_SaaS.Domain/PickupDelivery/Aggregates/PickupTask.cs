using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لمهمة استلام الملابس من العميل بواسطة السائق.
/// <para>
/// يتبع هذا الكيان مستأجراً محدداً (<see cref="TenantAuditedAggregateRoot.TenantId"/>) ولا يدعم الحذف الناعم (No Soft Delete).
/// يدعم النظام تسجيل محاولات استلام متعددة لنفس الطلب في حال تعثر إحداها، مع تمييزها برقم المحاولة (<see cref="AttemptNumber"/>)،
/// ويخضع لفهرس فريد مركب يضمن عدم تكرار نفس المحاولة للطلب الواحد (TenantId, OrderId, AttemptNumber).
/// </para>
/// </summary>
public class PickupTask : TenantAuditedAggregateRoot
{
    /// <summary>
    /// معرّف الطلب الرئيسي المرتبط بمهمة الاستلام هذه.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// معرّف السائق المعين لتنفيذ مهمة الاستلام إن وجد.
    /// </summary>
    public Guid? DriverId { get; private set; }

    /// <summary>
    /// رقم محاولة الاستلام الحالية لهذا الطلب (1 للمحاولة الأولى، ثم تتزايد عند إعادة الجدولة).
    /// </summary>
    public int AttemptNumber { get; private set; }

    /// <summary>
    /// الحالة التشغيلية الحالية لمهمة الاستلام.
    /// </summary>
    public PickupTaskStatus Status { get; private set; }

    /// <summary>
    /// بداية النافذة الزمنية المجدولة لقدوم السائق لاستلام الملابس.
    /// </summary>
    public DateTime ScheduledFrom { get; private set; }

    /// <summary>
    /// نهاية النافذة الزمنية المجدولة لقدوم السائق لاستلام الملابس.
    /// </summary>
    public DateTime ScheduledTo { get; private set; }

    /// <summary>
    /// تاريخ ووقت تعيين المهمة للسائق.
    /// </summary>
    public DateTime? AssignedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت قبول السائق للمهمة.
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت وصول السائق إلى موقع العميل الفعلي.
    /// </summary>
    public DateTime? ArrivedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت استلام الملابس وإغلاق الحقيبة بنجاح من العميل.
    /// </summary>
    public DateTime? PickedUpAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت اكتمال مهمة الاستلام بتوصيل الحقيبة للمغسلة.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت تعثر أو فشل محاولة الاستلام.
    /// </summary>
    public DateTime? FailedAt { get; private set; }

    /// <summary>
    /// سبب فشل أو تعثر محاولة الاستلام الموثق عند تسجيل الفشل.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private PickupTask()
    {
    }

    /// <summary>
    /// يُنشئ مهمة استلام جديدة لطلب محدد مع تعيين رقم المحاولة والنافذة الزمنية المجدولة.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="orderId">معرّف الطلب.</param>
    /// <param name="attemptNumber">رقم المحاولة.</param>
    /// <param name="scheduledFrom">بداية نافذة الجدولة.</param>
    /// <param name="scheduledTo">نهاية نافذة الجدولة.</param>
    /// <param name="driverId">معرّف السائق المعين إن وجد.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف الطلب فارغاً أو رقم المحاولة أقل من أو يساوي صفر.</exception>
    public PickupTask(
        Guid id,
        Guid tenantId,
        Guid orderId,
        int attemptNumber,
        DateTime scheduledFrom,
        DateTime scheduledTo,
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
        Status = driverId.HasValue ? PickupTaskStatus.Assigned : PickupTaskStatus.Assigned;
        AssignedAt = driverId.HasValue ? DateTime.UtcNow : null;
    }

    /// <summary>
    /// يعين سائقاً لمهمة الاستلام ويوثق توقيت التعيين، ويمنع إعادة تعيين المهام المنتهية.
    /// </summary>
    /// <param name="driverId">معرّف السائق.</param>
    /// <param name="assignedAt">تاريخ ووقت التعيين.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف السائق فارغاً أو كانت المهمة مكتملة أو ملغاة أو فاشلة.</exception>
    public void Assign(Guid driverId, DateTime assignedAt)
    {
        if (driverId == Guid.Empty)
        {
            throw new BusinessException("DriverId must not be empty.");
        }

        if (Status == PickupTaskStatus.Completed || Status == PickupTaskStatus.Failed || Status == PickupTaskStatus.Cancelled)
        {
            throw new BusinessException($"Cannot reassign a pickup task that is already in terminal status '{Status}'.");
        }

        DriverId = driverId;
        AssignedAt = assignedAt;
        Status = PickupTaskStatus.Assigned;
    }

    /// <summary>
    /// يسجل قبول السائق لمهمة الاستلام بعد تعيينها له (فقط من حالة Assigned).
    /// </summary>
    /// <param name="acceptedAt">تاريخ ووقت القبول.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن المهمة في حالة Assigned.</exception>
    public void Accept(DateTime acceptedAt)
    {
        if (Status != PickupTaskStatus.Assigned)
        {
            throw new BusinessException("Task must be in Assigned status to be accepted.");
        }

        Status = PickupTaskStatus.Accepted;
        AcceptedAt = acceptedAt;
    }

    /// <summary>
    /// يسجل انطلاق السائق في رحلة التوجه إلى موقع العميل (فقط من حالة Accepted).
    /// </summary>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن المهمة مقبولة من السائق أولاً.</exception>
    public void StartTrip()
    {
        if (Status != PickupTaskStatus.Accepted)
        {
            throw new BusinessException("Task must be accepted before starting trip.");
        }

        Status = PickupTaskStatus.OutForPickup;
    }

    /// <summary>
    /// يسجل وصول السائق الفعلي إلى موقع العميل (فقط من حالة OutForPickup).
    /// </summary>
    /// <param name="arrivedAt">تاريخ ووقت الوصول.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن السائق في حالة انطلاق (OutForPickup).</exception>
    public void Arrive(DateTime arrivedAt)
    {
        if (Status != PickupTaskStatus.OutForPickup)
        {
            throw new BusinessException("Task must be OutForPickup before marking as arrived.");
        }

        Status = PickupTaskStatus.Arrived;
        ArrivedAt = arrivedAt;
    }

    /// <summary>
    /// يؤكد استلام الملابس ووضعها في الحقيبة من العميل وتوثيق التوقيت (فقط بعد الوصول Arrived).
    /// </summary>
    /// <param name="pickedUpAt">تاريخ ووقت الاستلام.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن السائق في حالة Arrived.</exception>
    public void ConfirmPickup(DateTime pickedUpAt)
    {
        if (Status != PickupTaskStatus.Arrived)
        {
            throw new BusinessException("Task must be in Arrived status before confirming pickup.");
        }

        Status = PickupTaskStatus.PickedUp;
        PickedUpAt = pickedUpAt;
    }

    /// <summary>
    /// ينهي مهمة الاستلام ويكملها عند وصول الشحنة إلى مقر المغسلة (فقط من حالة PickedUp).
    /// </summary>
    /// <param name="completedAt">تاريخ ووقت الإكمال.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم تكن الملابس قد استلمت من العميل أولاً (PickedUp).</exception>
    public void Complete(DateTime completedAt)
    {
        if (Status != PickupTaskStatus.PickedUp)
        {
            throw new BusinessException("Task must be in PickedUp status before completing at laundry.");
        }

        Status = PickupTaskStatus.Completed;
        CompletedAt = completedAt;
    }

    /// <summary>
    /// يسجل فشل محاولة الاستلام الحالية مع توثيق السبب ووقت التعثر (فقط من الحالات النشطة قبل الإتمام).
    /// </summary>
    /// <param name="reason">سبب التعثر أو الفشل.</param>
    /// <param name="failedAt">تاريخ ووقت الفشل.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المهمة قد اكتملت أو ألغيت مسبقاً.</exception>
    public void Fail(string reason, DateTime failedAt)
    {
        if (Status == PickupTaskStatus.Completed || Status == PickupTaskStatus.Cancelled || Status == PickupTaskStatus.Failed)
        {
            throw new BusinessException($"Cannot mark task as failed when already in terminal status '{Status}'.");
        }

        Status = PickupTaskStatus.Failed;
        FailureReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
        FailedAt = failedAt;
    }

    /// <summary>
    /// يلغي مهمة الاستلام لأسباب إدارية مع توثيق السبب (فقط من الحالات القابلة للإلغاء).
    /// </summary>
    /// <param name="reason">سبب الإلغاء.</param>
    /// <exception cref="BusinessException">يتم رميها إذا تم استلام الملابس أو اكتملت المهمة أو ألغيت مسبقاً.</exception>
    public void Cancel(string reason)
    {
        if (Status == PickupTaskStatus.Completed || Status == PickupTaskStatus.PickedUp || Status == PickupTaskStatus.Failed || Status == PickupTaskStatus.Cancelled)
        {
            throw new BusinessException($"Cannot cancel a pickup task in status '{Status}'.");
        }

        Status = PickupTaskStatus.Cancelled;
        FailureReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
    }
}
