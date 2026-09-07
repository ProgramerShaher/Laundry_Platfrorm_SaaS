using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Drivers;

/// <summary>
/// مدخلات تحديث بيانات ملف السائق والمركبة.
/// </summary>
public class UpdateDriverProfileInput
{
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
    /// تفاصيل المركبة.
    /// </summary>
    [StringLength(200)]
    public string? VehicleDetails { get; set; }

    /// <summary>
    /// ما إذا كان حساب السائق مفعل في النظام.
    /// </summary>
    public bool IsActive { get; set; }
}
