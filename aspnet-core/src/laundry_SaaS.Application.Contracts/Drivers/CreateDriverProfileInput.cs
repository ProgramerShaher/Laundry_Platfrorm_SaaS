using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Drivers;

/// <summary>
/// مدخلات إنشاء ملف سائق جديد وتعيين مستخدم له في المغسلة (Create Driver Profile Input).
/// </summary>
public class CreateDriverProfileInput
{
    /// <summary>
    /// اسم المستخدم لحساب السائق في النظام.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للسائق.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// كلمة المرور الأولية لحساب السائق.
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// الاسم الشخصي للسائق.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// اسم العائلة.
    /// </summary>
    [StringLength(64)]
    public string? Surname { get; set; }

    /// <summary>
    /// رقم هاتف السائق.
    /// </summary>
    [Required]
    [Phone]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// تفاصيل المركبة (مثل نوع السيارة، الموديل، رقم اللوحة).
    /// </summary>
    [StringLength(200)]
    public string? VehicleDetails { get; set; }
}
