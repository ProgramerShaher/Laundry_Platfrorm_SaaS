using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Orders;

/// <summary>
/// كيان تابع (Child Entity) يمثل تعديلاً مالياً أو كمياً مقترحاً على إجمالي الطلب نتيجة لعملية المعاينة والفحص الفني.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Order"/>. يُستخدم هذا الكيان عند اكتشاف أصناف إضافية لم يطلبها العميل أو خدمات تستدعي سعراً إضافياً؛
/// فإذا ترتبت زيادة في السعر (<see cref="DifferenceAmount"/> > 0) يتوقف الطلب في حالة انتظار موافقة العميل قبل المتابعة.
/// </para>
/// </summary>
public class OrderAdjustment : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف الطلب الرئيسي المالك لهذا التعديل.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// حالة اعتماد التعديل الحالي (معلق، معتمد، مرفوض، ملغى).
    /// </summary>
    public OrderAdjustmentStatus Status { get; private set; }

    /// <summary>
    /// السبب التفصيلي لطلب التعديل (مثل: تم العثور على 3 قطع إضافية أو بقع مستعصية تتطلب تنظيفاً جافاً).
    /// </summary>
    public string Reason { get; private set; } = null!;

    /// <summary>
    /// إجمالي قيمة الطلب السابقة قبل تطبيق هذا التعديل.
    /// </summary>
    public decimal OldTotal { get; private set; }

    /// <summary>
    /// إجمالي قيمة الطلب الجديدة المقترحة بعد تطبيق التعديل.
    /// </summary>
    public decimal NewTotal { get; private set; }

    /// <summary>
    /// قيمة الفارق المالي الناتج عن التعديل: (NewTotal - OldTotal).
    /// تكون موجبة في حال الزيادة وتتطلب موافقة العميل، أو سالبة في حال الخصم/استبعاد قطعة.
    /// </summary>
    public decimal DifferenceAmount { get; private set; }

    /// <summary>
    /// تاريخ ووقت موافقة العميل على التعديل المقترح.
    /// </summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت رفض العميل للتعديل المقترح.
    /// </summary>
    public DateTime? RejectedAt { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private OrderAdjustment()
    {
    }

    /// <summary>
    /// يُنشئ مقترح تعديل جديد على إجمالي الطلب مع التحقق من وجود اختلاف حقيقي في القيمة المالية.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="orderId">معرّف الطلب المالك.</param>
    /// <param name="reason">سبب التعديل.</param>
    /// <param name="oldTotal">الإجمالي السابق.</param>
    /// <param name="newTotal">الإجمالي الجديد المقترح.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن هناك أي تغيير مالي بين القديم والجديد.</exception>
    public OrderAdjustment(
        Guid id,
        Guid orderId,
        string reason,
        decimal oldTotal,
        decimal newTotal)
        : base(id)
    {
        if (oldTotal == newTotal)
        {
            throw new BusinessException("OrderAdjustment requires a change in total amount.");
        }

        if (oldTotal < 0 || newTotal < 0)
        {
            throw new BusinessException("OldTotal and NewTotal must be greater than or equal to zero.");
        }

        OrderId = orderId;
        Reason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
        OldTotal = oldTotal;
        NewTotal = newTotal;
        DifferenceAmount = newTotal - oldTotal;
        Status = OrderAdjustmentStatus.Pending;
    }

    /// <summary>
    /// يعتمد التعديل بعد موافقة العميل ويوثق توقيت الاعتماد.
    /// </summary>
    /// <param name="approvedAt">تاريخ ووقت الاعتماد.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن التعديل في حالة معلق (Pending).</exception>
    public void Approve(DateTime approvedAt)
    {
        if (Status != OrderAdjustmentStatus.Pending)
        {
            throw new BusinessException("Only pending adjustments can be approved.");
        }

        Status = OrderAdjustmentStatus.Approved;
        ApprovedAt = approvedAt;
    }

    /// <summary>
    /// يرفض التعديل بعد رفض العميل ويوثق توقيت الرفض.
    /// </summary>
    /// <param name="rejectedAt">تاريخ ووقت الرفض.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن التعديل في حالة معلق (Pending).</exception>
    public void Reject(DateTime rejectedAt)
    {
        if (Status != OrderAdjustmentStatus.Pending)
        {
            throw new BusinessException("Only pending adjustments can be rejected.");
        }

        Status = OrderAdjustmentStatus.Rejected;
        RejectedAt = rejectedAt;
    }

    /// <summary>
    /// يلغي مقترح التعديل من قبل المغسلة قبل اتخاذ القرار بشأنه.
    /// </summary>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن التعديل معلقاً.</exception>
    public void Cancel()
    {
        if (Status != OrderAdjustmentStatus.Pending)
        {
            throw new BusinessException("Only pending adjustments can be cancelled.");
        }

        Status = OrderAdjustmentStatus.Cancelled;
    }
}
