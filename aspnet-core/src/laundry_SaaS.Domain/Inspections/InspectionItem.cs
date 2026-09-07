using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Inspections;

/// <summary>
/// كيان تابع (Child Entity) يمثل بند فحص لمطابقة عدد القطع المتوقعة مع المستلمة فعلياً في المغسلة.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Inspection"/>. يربط هذا الكيان بند الطلب الأصلي (<see cref="OrderItemId"/>)
/// لتسجيل الكمية التي طلبها العميل (<see cref="ExpectedQuantity"/>) والكمية التي تم عدها واستلامها داخل المغسلة (<see cref="ActualQuantity"/>)،
/// مع إمكانية تدوين ملاحظات الفني عند وجود اختلاف.
/// </para>
/// </summary>
public class InspectionItem : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف محضر الفحص الرئيسي المالك لهذا البند.
    /// </summary>
    public Guid InspectionId { get; private set; }

    /// <summary>
    /// معرّف بند الطلب المقابل (مرجع لـ OrderItemId).
    /// </summary>
    public Guid OrderItemId { get; private set; }

    /// <summary>
    /// الكمية المتوقعة للقطعة بناءً على ما سجله العميل في الطلب.
    /// </summary>
    public int ExpectedQuantity { get; private set; }

    /// <summary>
    /// الكمية الفعلية المحصورة داخل الحقيبة بعد فتحها وفحصها في المغسلة.
    /// </summary>
    public int ActualQuantity { get; private set; }

    /// <summary>
    /// ملاحظات الفني حول حالة أو مطابقة هذا الصنف.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private InspectionItem()
    {
    }

    /// <summary>
    /// يُنشئ بند مطابقة فحص جديد يربط بين الكمية المطلوبة والمستلمة فعلياً.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="inspectionId">معرّف محضر الفحص.</param>
    /// <param name="orderItemId">معرّف بند الطلب المقابل.</param>
    /// <param name="expectedQuantity">الكمية المتوقعة.</param>
    /// <param name="actualQuantity">الكمية الفعلية المستلمة.</param>
    /// <param name="notes">ملاحظات الفحص الاختيارية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف بند الطلب فارغاً.</exception>
    public InspectionItem(
        Guid id,
        Guid inspectionId,
        Guid orderItemId,
        int expectedQuantity,
        int actualQuantity,
        string? notes = null)
        : base(id)
    {
        if (orderItemId == Guid.Empty)
        {
            throw new BusinessException("OrderItemId must not be empty.");
        }

        InspectionId = inspectionId;
        OrderItemId = orderItemId;
        ExpectedQuantity = expectedQuantity >= 0 ? expectedQuantity : 0;
        ActualQuantity = actualQuantity >= 0 ? actualQuantity : 0;
        Notes = notes;
    }

    /// <summary>
    /// يحدّث الكمية الفعلية المستلمة مع تدوين الملاحظات.
    /// </summary>
    /// <param name="actualQuantity">الكمية الفعلية الجديدة.</param>
    /// <param name="notes">ملاحظات إضافية اختيارية.</param>
    public void UpdateActualQuantity(int actualQuantity, string? notes = null)
    {
        ActualQuantity = actualQuantity >= 0 ? actualQuantity : 0;
        if (notes != null)
        {
            Notes = notes;
        }
    }
}
