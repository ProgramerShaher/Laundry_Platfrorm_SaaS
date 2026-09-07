using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات إنشاء ملف موظف جديد لمغسلة وتعيين مستخدم Identity له.
/// </summary>
public class CreateLaundryStaffProfileInput
{
    /// <summary>
    /// اسم المستخدم للدخول (Username).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للموظف.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// كلمة المرور الأولية لحساب الموظف.
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

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
    /// رقم هاتف الموظف.
    /// </summary>
    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// المسمى الوظيفي للموظف (مثل فني فحص، مشرف غسيل).
    /// </summary>
    [StringLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// الملاحظات الإدارية الخاصة بالموظف.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
