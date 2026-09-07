using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Inspections;

/// <summary>
/// بيانات إدخال تسجيل وتوثيق ضرر أو عيب مسبق في قطعة ملابس أثناء الفحص الفني.
/// </summary>
public class RecordInspectionDamageInput
{
    /// <summary>
    /// المعرف الفريد لبند الفحص المراد توثيق الضرر عليه.
    /// </summary>
    [Required]
    public Guid InspectionItemId { get; set; }

    /// <summary>
    /// تصنيف نوع الضرر (تمزق، بقعة قديمة، حرق، زر مفقود، إلخ).
    /// </summary>
    [Required]
    public DamageType DamageType { get; set; }

    /// <summary>
    /// وصف دقيق لموضع الضرر وحجمه وتفاصيله على القطعة.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// اسم مرجع الصورة الفوتوغرافية للضرر في خدمة التخزين السحابي (Blob Name).
    /// </summary>
    [Required]
    [StringLength(256)]
    public string PhotoBlobName { get; set; } = null!;

    /// <summary>
    /// مستوى جسامة الضرر وشدته (طفيف، متوسط، جسيم).
    /// </summary>
    [Required]
    public DamageSeverity Severity { get; set; }
}
