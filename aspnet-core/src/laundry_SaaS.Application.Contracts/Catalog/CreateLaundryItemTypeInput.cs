using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مدخلات إنشاء نوع قطعة ملابس جديد في كتالوج المغسلة (مثل ثوب، قميص، عباءة).
/// </summary>
public class CreateLaundryItemTypeInput
{
    /// <summary>
    /// الاسم التجاري لنوع القطعة بالعربية.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// الرمز التعريفي الفريد لنوع القطعة (Code) داخل نطاق المستأجر (مثل THB, SHRT).
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// وصف اختياري لنوع القطعة وتعليمات المعالجة.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// ترتيب العرض في واجهة تطبيق العميل ولوحة الإدارة.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// حالة تفعيل الصنف في الكتالوج مبدئياً.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
