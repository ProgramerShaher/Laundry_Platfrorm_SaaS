using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مدخلات تحديث بيانات صنف ملابس في الكتالوج.
/// </summary>
public class UpdateLaundryItemTypeInput
{
    /// <summary>
    /// الاسم المحدث لنوع القطعة.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// الوصف المحدث.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// ترتيب العرض في الواجهات.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// حالة تفعيل الصنف لاستقبال الطلبات.
    /// </summary>
    public bool IsActive { get; set; }
}
