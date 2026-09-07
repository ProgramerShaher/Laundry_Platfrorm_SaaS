using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مدخلات تحديث بيانات خدمة غسيل أو معالجة في الكتالوج.
/// </summary>
public class UpdateLaundryServiceInput
{
    /// <summary>
    /// الاسم المحدث للخدمة.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// الوصف المحدث للخدمة.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// ترتيب العرض في الواجهات.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// حالة تفعيل الخدمة لاستقبال الطلبات.
    /// </summary>
    public bool IsActive { get; set; }
}
