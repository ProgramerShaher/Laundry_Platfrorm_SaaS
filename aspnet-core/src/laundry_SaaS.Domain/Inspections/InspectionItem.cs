using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Inspections;

/// <summary>
/// كيان تابع (Child Entity) يمثل سطر فحص ومعاينة دقيق داخل محضر الفحص الفني (<see cref="Inspection"/>).
/// <para>
/// تم تصميم هذا الكيان لتمثيل الفروقات والمطابقات الواقعية عند استلام الملابس وفرزها داخل المغسلة بدقة متناهية:
/// <list type="bullet">
/// <item><description><b>المطابقة التامة (Exact Match):</b> <see cref="OrderItemId"/> يحمل قيمة، الكمية متطابقة، والصنف والخدمة الفعليان يطابقان المتوقع.</description></item>
/// <item><description><b>تغير الكمية (Quantity Mismatch):</b> <see cref="OrderItemId"/> يحمل قيمة، والكمية الفعلية تختلف عن المتوقعة (زيادة أو نقصان).</description></item>
/// <item><description><b>قطعة مفقودة بالكامل (Missing Item):</b> <see cref="OrderItemId"/> يحمل قيمة، <see cref="ExpectedQuantity"/> &gt; 0، و <see cref="ActualQuantity"/> = 0.</description></item>
/// <item><description><b>قطعة إضافية (Additional Item):</b> <see cref="OrderItemId"/> يساوي <c>null</c> (لم تكن بالطلب)، <see cref="ExpectedQuantity"/> = 0، والبيانات الفعلية إلزامية (<see cref="ActualLaundryItemTypeId"/> و <see cref="ActualLaundryServiceId"/> و <see cref="ActualQuantity"/> &gt; 0).</description></item>
/// <item><description><b>تغير نوع الصنف أو الخدمة (Changed Item/Service):</b> <see cref="OrderItemId"/> يحمل قيمة، ويتم توثيق النوع والخدمة المتوقعة وتوثيق النوع والخدمة الفعلية الجديدة دون المساس بلقطة الطلب الأصلية.</description></item>
/// </list>
/// </para>
/// </summary>
public class InspectionItem : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف محضر الفحص الرئيسي المالك لهذا السطر (Aggregate Root).
    /// </summary>
    public Guid InspectionId { get; private set; }

    /// <summary>
    /// معرّف بند الطلب الأصلي المقابل (<see cref="Orders.OrderItem"/>).
    /// <para>
    /// <b>لماذا هو Nullable ومتى يكون null؟</b><br/>
    /// يكون <c>null</c> حصراً عندما يجد موظف الفحص في المغسلة قطعة إضافية داخل الحقيبة لم يقم العميل بإدراجها في الطلب أصلاً.
    /// ويكون حاملاً لقيمة (<c>Guid</c>) في جميع الحالات الأخرى التي تعبر عن بند كان موجوداً في طلب العميل المعتمد (سواء استُلم كاملاً، أو ناقصاً، أو بنوع/خدمة مختلفة).
    /// </para>
    /// </summary>
    public Guid? OrderItemId { get; private set; }

    /// <summary>
    /// معرّف نوع الصنف المتوقع للقطعة بناءً على طلب العميل الأصلي.
    /// يكون <c>null</c> إذا كان البند إضافياً لم يكن مسجلاً في الطلب.
    /// </summary>
    public Guid? ExpectedLaundryItemTypeId { get; private set; }

    /// <summary>
    /// معرّف نوع الخدمة المتوقعة للقطعة بناءً على طلب العميل الأصلي.
    /// يكون <c>null</c> إذا كان البند إضافياً لم يكن مسجلاً في الطلب.
    /// </summary>
    public Guid? ExpectedLaundryServiceId { get; private set; }

    /// <summary>
    /// الكمية المتوقعة للقطعة المسجلة بالطلب الأصلي (تكون 0 في حال البنود الإضافية غير المسجلة).
    /// </summary>
    public int ExpectedQuantity { get; private set; }

    /// <summary>
    /// معرّف نوع الصنف الفعلي للقطعة بعد معاينتها وفرزها فحصاً في المغسلة.
    /// يكون إلزامياً عند الفحص (أو يطابق المتوقع).
    /// </summary>
    public Guid? ActualLaundryItemTypeId { get; private set; }

    /// <summary>
    /// معرّف نوع الخدمة الفعلية المناسبة للقطعة بعد المعاينة الفنية في المغسلة.
    /// يكون إلزامياً عند الفحص (أو يطابق المتوقع).
    /// </summary>
    public Guid? ActualLaundryServiceId { get; private set; }

    /// <summary>
    /// الكمية الفعلية المستلمة والمفحوصة داخل المغسلة (تكون 0 إذا كانت القطعة مفقودة بالكامل).
    /// </summary>
    public int ActualQuantity { get; private set; }

    /// <summary>
    /// ملاحظات الفني التفصيلية حول أسباب الاختلاف أو حالة القطعة.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان هذا السطر يمثل قطعة إضافية لم تكن موجودة في الطلب الأصلي.
    /// </summary>
    public bool IsAdditionalItem => !OrderItemId.HasValue;

    /// <summary>
    /// يشير إلى ما إذا كان هذا البند الأصلي مفقوداً بالكامل في الاستلام الفعلي (كمية فعلية = 0).
    /// </summary>
    public bool IsMissingItem => OrderItemId.HasValue && ExpectedQuantity > 0 && ActualQuantity == 0;

    /// <summary>
    /// يشير إلى ما إذا كان نوع الصنف الفعلي يختلف عن نوع الصنف المتوقع المسجل في الطلب.
    /// </summary>
    public bool IsItemTypeChanged => OrderItemId.HasValue && ExpectedLaundryItemTypeId.HasValue && ActualLaundryItemTypeId.HasValue && ExpectedLaundryItemTypeId.Value != ActualLaundryItemTypeId.Value;

    /// <summary>
    /// يشير إلى ما إذا كانت الخدمة الفعلية تختلف عن الخدمة المتوقعة المسجلة في الطلب.
    /// </summary>
    public bool IsServiceChanged => OrderItemId.HasValue && ExpectedLaundryServiceId.HasValue && ActualLaundryServiceId.HasValue && ExpectedLaundryServiceId.Value != ActualLaundryServiceId.Value;

    /// <summary>
    /// يشير إلى ما إذا كان هناك اختلاف في الكمية فقط بين المتوقع والفعلي.
    /// </summary>
    public bool IsQuantityChanged => ExpectedQuantity != ActualQuantity;

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private InspectionItem()
    {
    }

    /// <summary>
    /// مُنشئ عام كامل لإنشاء سطر فحص ومطابقة مرن وفق القواعد الصارمة لنطاق العمل.
    /// </summary>
    /// <param name="id">المعرّف الفريد للسطر.</param>
    /// <param name="inspectionId">معرّف محضر الفحص المالك.</param>
    /// <param name="orderItemId">معرّف بند الطلب الأصلي المقابل إن وجد (null للقطعة الإضافية).</param>
    /// <param name="expectedItemTypeId">نوع الصنف المتوقع (null للإضافي).</param>
    /// <param name="expectedServiceId">نوع الخدمة المتوقعة (null للإضافي).</param>
    /// <param name="expectedQuantity">الكمية المتوقعة (0 للإضافي).</param>
    /// <param name="actualItemTypeId">نوع الصنف الفعلي المفحوص.</param>
    /// <param name="actualServiceId">نوع الخدمة الفعلية المفحوصة.</param>
    /// <param name="actualQuantity">الكمية الفعلية المستلمة (0 للمفقود).</param>
    /// <param name="notes">ملاحظات الفني الاختيارية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا خالفت البيانات قواعد النطاق الصارمة.</exception>
    public InspectionItem(
        Guid id,
        Guid inspectionId,
        Guid? orderItemId,
        Guid? expectedItemTypeId,
        Guid? expectedServiceId,
        int expectedQuantity,
        Guid? actualItemTypeId,
        Guid? actualServiceId,
        int actualQuantity,
        string? notes = null)
        : base(id)
    {
        if (inspectionId == Guid.Empty)
        {
            throw new BusinessException("InspectionId must not be empty.");
        }

        if (expectedQuantity < 0)
        {
            throw new BusinessException("ExpectedQuantity must not be negative.");
        }

        if (actualQuantity < 0)
        {
            throw new BusinessException("ActualQuantity must not be negative.");
        }

        // قاعدة القطعة الإضافية: إذا لم يكن هناك OrderItemId
        if (!orderItemId.HasValue)
        {
            if (expectedQuantity != 0)
            {
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.InspectionErrorCodes.InvalidAdditionalItem,
                    "Additional inspection item must have ExpectedQuantity equal to 0.");
            }

            if (!actualItemTypeId.HasValue || actualItemTypeId.Value == Guid.Empty)
            {
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.InspectionErrorCodes.InvalidAdditionalItem,
                    "Additional inspection item must specify an ActualLaundryItemTypeId.");
            }

            if (!actualServiceId.HasValue || actualServiceId.Value == Guid.Empty)
            {
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.InspectionErrorCodes.InvalidAdditionalItem,
                    "Additional inspection item must specify an ActualLaundryServiceId.");
            }

            if (actualQuantity <= 0)
            {
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.InspectionErrorCodes.InvalidAdditionalItem,
                    "Additional inspection item must have ActualQuantity greater than 0.");
            }
        }
        else
        {
            // بند أصلي من الطلب
            if (orderItemId.Value == Guid.Empty)
            {
                throw new BusinessException("OrderItemId must not be Guid.Empty when provided.");
            }

            if (expectedQuantity <= 0)
            {
                throw new BusinessException("Original order item inspection line must have ExpectedQuantity greater than 0.");
            }

            if (!actualItemTypeId.HasValue || actualItemTypeId.Value == Guid.Empty)
            {
                actualItemTypeId = expectedItemTypeId;
            }

            if (!actualServiceId.HasValue || actualServiceId.Value == Guid.Empty)
            {
                actualServiceId = expectedServiceId;
            }
        }

        InspectionId = inspectionId;
        OrderItemId = orderItemId;
        ExpectedLaundryItemTypeId = expectedItemTypeId;
        ExpectedLaundryServiceId = expectedServiceId;
        ExpectedQuantity = expectedQuantity;
        ActualLaundryItemTypeId = actualItemTypeId;
        ActualLaundryServiceId = actualServiceId;
        ActualQuantity = actualQuantity;
        Notes = notes;
    }

    /// <summary>
    /// منشئ مصنعي ملائم لإنشاء سطر فحص مطابق أو معدل لبند موجود مسبقاً في الطلب.
    /// </summary>
    public static InspectionItem ForOriginalItem(
        Guid id,
        Guid inspectionId,
        Guid orderItemId,
        Guid expectedItemTypeId,
        Guid expectedServiceId,
        int expectedQuantity,
        int actualQuantity,
        Guid? actualItemTypeId = null,
        Guid? actualServiceId = null,
        string? notes = null)
    {
        return new InspectionItem(
            id,
            inspectionId,
            orderItemId,
            expectedItemTypeId,
            expectedServiceId,
            expectedQuantity,
            actualItemTypeId ?? expectedItemTypeId,
            actualServiceId ?? expectedServiceId,
            actualQuantity,
            notes);
    }

    /// <summary>
    /// منشئ مصنعي ملائم لإنشاء سطر فحص لقطعة إضافية وجدت في الحقيبة ولم تكن ضمن طلب العميل.
    /// </summary>
    public static InspectionItem ForAdditionalItem(
        Guid id,
        Guid inspectionId,
        Guid actualItemTypeId,
        Guid actualServiceId,
        int actualQuantity,
        string? notes = null)
    {
        return new InspectionItem(
            id,
            inspectionId,
            orderItemId: null,
            expectedItemTypeId: null,
            expectedServiceId: null,
            expectedQuantity: 0,
            actualItemTypeId: actualItemTypeId,
            actualServiceId: actualServiceId,
            actualQuantity: actualQuantity,
            notes: notes);
    }

    /// <summary>
    /// يحدّث الكمية والبيانات الفعلية المستلمة مع تدوين الملاحظات.
    /// </summary>
    /// <param name="actualQuantity">الكمية الفعلية الجديدة.</param>
    /// <param name="actualItemTypeId">نوع الصنف الفعلي الاختياري.</param>
    /// <param name="actualServiceId">نوع الخدمة الفعلية الاختياري.</param>
    /// <param name="notes">ملاحظات إضافية اختيارية.</param>
    public void UpdateActualResults(
        int actualQuantity,
        Guid? actualItemTypeId = null,
        Guid? actualServiceId = null,
        string? notes = null)
    {
        if (actualQuantity < 0)
        {
            throw new BusinessException("ActualQuantity must not be negative.");
        }

        if (IsAdditionalItem && actualQuantity <= 0)
        {
            throw new BusinessException("Additional item cannot have an actual quantity of 0.");
        }

        ActualQuantity = actualQuantity;

        if (actualItemTypeId.HasValue && actualItemTypeId.Value != Guid.Empty)
        {
            ActualLaundryItemTypeId = actualItemTypeId.Value;
        }

        if (actualServiceId.HasValue && actualServiceId.Value != Guid.Empty)
        {
            ActualLaundryServiceId = actualServiceId.Value;
        }

        if (notes != null)
        {
            Notes = notes;
        }
    }
}
