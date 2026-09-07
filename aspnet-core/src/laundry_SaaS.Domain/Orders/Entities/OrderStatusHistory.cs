using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Orders;

/// <summary>
/// كيان تابع (Child Entity) يمثل سجلاً تاريخياً تراكمياً (Append-Only) لجميع التحولات التي طرأت على حالة الطلب.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Order"/>. هذا السجل غير قابل للتعديل (Immutable) ولا يدعم الحذف (No Delete)،
/// ويوثق توقيت الانتقال وهوية المستخدم المنفذ تلقائياً عبر <see cref="CreationAuditedEntity{TKey}"/> في ABP.
/// أي تغيير جديد في حالة الطلب يجب أن يُسجل بإضافة Record جديد حصراً.
/// </para>
/// </summary>
public class OrderStatusHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// معرّف الطلب المالك الذي طرأ عليه هذا التغير في الحالة.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// حالة الطلب السابقة قبل هذا التحول (تكون null في أول حركة لإنشاء الطلب).
    /// </summary>
    public OrderStatus? FromStatus { get; private set; }

    /// <summary>
    /// حالة الطلب الجديدة التي انتقل إليها.
    /// </summary>
    public OrderStatus ToStatus { get; private set; }

    /// <summary>
    /// السبب أو البيان التوضيحي الذي صاحب الانتقال إلى الحالة الجديدة إن وجد.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private OrderStatusHistory()
    {
    }

    /// <summary>
    /// يُنشئ سجلاً تاريخياً جديداً لتوثيق حركة تحول حالة الطلب.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="orderId">معرّف الطلب المالك.</param>
    /// <param name="fromStatus">الحالة السابقة.</param>
    /// <param name="toStatus">الحالة الجديدة.</param>
    /// <param name="reason">سبب التحول الاختياري.</param>
    public OrderStatusHistory(
        Guid id,
        Guid orderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? reason = null)
        : base(id)
    {
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
    }
}
