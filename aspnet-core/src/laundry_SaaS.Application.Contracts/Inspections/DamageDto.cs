using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Inspections;

/// <summary>
/// يمثل ضرراً أو عيباً مسبقاً مسجلاً على قطعة ملابس أثناء الفحص الفني في المغسلة.
/// </summary>
public class DamageDto : AuditedEntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد لمحضر الفحص التابع له.
    /// </summary>
    public Guid InspectionId { get; set; }

    /// <summary>
    /// المعرف الفريد لبند الفحص المرتبط بالضرر.
    /// </summary>
    public Guid InspectionItemId { get; set; }

    /// <summary>
    /// نوع الضرر المكتشف.
    /// </summary>
    public DamageType DamageType { get; set; }

    /// <summary>
    /// اسم نوع الضرر بالعربية للعرض.
    /// </summary>
    public string DamageTypeName { get; set; } = null!;

    /// <summary>
    /// وصف تفصيلي لموضع الضرر وحجمه.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// اسم مرجع الصورة التوثيقية للضرر في التخزين السحابي.
    /// </summary>
    public string PhotoBlobName { get; set; } = null!;

    /// <summary>
    /// مستوى جسامة الضرر.
    /// </summary>
    public DamageSeverity Severity { get; set; }

    /// <summary>
    /// تاريخ ووقت رصد وتوثيق هذا الضرر.
    /// </summary>
    public DateTime ObservedAt { get; set; }
}
