using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Inspections;

/// <summary>
/// يمثل بند فحص ومعاينة لقطعة ملابس ضمن محضر الفحص الفني، موضحاً المطابقة أو الفروقات.
/// </summary>
public class InspectionItemDto : AuditedEntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد لمحضر الفحص الرئيسي.
    /// </summary>
    public Guid InspectionId { get; set; }

    /// <summary>
    /// المعرف الفريد لبند الطلب الأصلي المقابل إن وجد (يكون فارغاً للقطع الإضافية غير المسجلة أصلاً).
    /// </summary>
    public Guid? OrderItemId { get; set; }

    /// <summary>
    /// معرّف نوع الصنف المتوقع بحسب الطلب الأصلي.
    /// </summary>
    public Guid? ExpectedLaundryItemTypeId { get; set; }

    /// <summary>
    /// اسم نوع الصنف المتوقع بالعربية.
    /// </summary>
    public string? ExpectedLaundryItemTypeName { get; set; }

    /// <summary>
    /// معرّف نوع الخدمة المتوقعة بحسب الطلب الأصلي.
    /// </summary>
    public Guid? ExpectedLaundryServiceId { get; set; }

    /// <summary>
    /// اسم نوع الخدمة المتوقعة بالعربية.
    /// </summary>
    public string? ExpectedLaundryServiceName { get; set; }

    /// <summary>
    /// الكمية المتوقعة بحسب الطلب الأصلي.
    /// </summary>
    public int ExpectedQuantity { get; set; }

    /// <summary>
    /// معرّف نوع الصنف الفعلي بعد المعاينة والفرز في المغسلة.
    /// </summary>
    public Guid? ActualLaundryItemTypeId { get; set; }

    /// <summary>
    /// اسم نوع الصنف الفعلي بالعربية.
    /// </summary>
    public string? ActualLaundryItemTypeName { get; set; }

    /// <summary>
    /// معرّف نوع الخدمة الفعلية المناسبة بعد المعاينة الفنية.
    /// </summary>
    public Guid? ActualLaundryServiceId { get; set; }

    /// <summary>
    /// اسم نوع الخدمة الفعلية بالعربية.
    /// </summary>
    public string? ActualLaundryServiceName { get; set; }

    /// <summary>
    /// الكمية الفعلية المستلمة والمفحوصة.
    /// </summary>
    public int ActualQuantity { get; set; }

    /// <summary>
    /// ملاحظات الفني التفصيلية حول القطعة.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// هل هذا السطر يمثل قطعة إضافية لم تكن مسجلة بالطلب أصلاً؟
    /// </summary>
    public bool IsAdditionalItem { get; set; }

    /// <summary>
    /// هل هذا البند مفقود كلياً عند الاستلام الفعلي (كمية مستلمة = 0)؟
    /// </summary>
    public bool IsMissingItem { get; set; }

    /// <summary>
    /// هل اختلف نوع الصنف الفعلي عن نوع الصنف المتوقع؟
    /// </summary>
    public bool IsItemTypeChanged { get; set; }

    /// <summary>
    /// قائمة الأضرار والعيوب الموثقة على هذه القطعة إن وجدت.
    /// </summary>
    public List<DamageDto> Damages { get; set; } = new();
}
