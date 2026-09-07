using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات تحديث بيانات ملف موظف المغسلة.
/// </summary>
public class UpdateLaundryStaffProfileInput
{
    /// <summary>
    /// الاسم الشخصي للموظف.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// اللقب أو اسم العائلة.
    /// </summary>
    [StringLength(64)]
    public string? Surname { get; set; }

    /// <summary>
    /// رقم هاتف الموظف المحدث.
    /// </summary>
    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// المسمى الوظيفي للموظف.
    /// </summary>
    [StringLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// حالة تفعيل الموظف للعمل في المغسلة.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// الملاحظات الإدارية.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
