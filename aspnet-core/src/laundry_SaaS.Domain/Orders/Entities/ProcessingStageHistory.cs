using System;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Orders;

/// <summary>
/// كيان تابع (Child Entity) يمثل سجلاً تاريخياً تراكمياً (Append-Only) لحركة مراحل المعالجة والغسيل الداخلية للطلب.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Order"/> الذي يعد المصدر الحصري والموثوق (Source of Truth) لمراحل المعالجة وليس الحقيبة (Bag).
/// هذا السجل غير قابل للتعديل (Immutable) ولا يدعم الحذف (No Delete)،
/// ويوثق الانتقال بين مراحل الغسيل (فرز، غسيل، تجفيف، كي، تطبيق، تغليف) تلقائياً مع توقيت ومعرّف المنفذ.
/// </para>
/// </summary>
public class ProcessingStageHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// معرّف الطلب المالك الذي تمت معالجة ملابسه.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// مرحلة المعالجة السابقة (تكون null عند بدء المعالجة في أول مرحلة).
    /// </summary>
    public ProcessingStage? FromStage { get; private set; }

    /// <summary>
    /// مرحلة المعالجة الجديدة التي انتقل إليها الطلب.
    /// </summary>
    public ProcessingStage ToStage { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private ProcessingStageHistory()
    {
    }

    /// <summary>
    /// يُنشئ سجلاً تاريخياً جديداً لتوثيق انتقال مرحلة المعالجة الداخلية للطلب.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="orderId">معرّف الطلب المالك.</param>
    /// <param name="fromStage">المرحلة السابقة.</param>
    /// <param name="toStage">المرحلة الجديدة.</param>
    public ProcessingStageHistory(
        Guid id,
        Guid orderId,
        ProcessingStage? fromStage,
        ProcessingStage toStage)
        : base(id)
    {
        OrderId = orderId;
        FromStage = fromStage;
        ToStage = toStage;
    }
}
