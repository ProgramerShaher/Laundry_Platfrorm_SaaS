using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مدخلات إنشاء نوع خدمة غسيل أو معالجة جديد في الكتالوج (مثل غسيل وكي، تنظيف جاف، كي فقط).
/// </summary>
public class CreateLaundryServiceInput
{
    /// <summary>
    /// اسم الخدمة بالعربية.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// الرمز التعريفي للخدمة داخل نطاق المستأجر (مثل WASH_IRON, DRY_CLEAN).
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// وصف الخدمة والمراحل المتضمنة فيها.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// ترتيب عرض الخدمة في واجهات المستخدم.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// ما إذا كانت الخدمة مفعلة مبدئياً.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
