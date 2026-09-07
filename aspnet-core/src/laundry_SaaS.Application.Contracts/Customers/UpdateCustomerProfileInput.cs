using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Customers;

/// <summary>
/// مدخلات تحديث البيانات الأساسية للملف الشخصي للعميل.
/// </summary>
public class UpdateCustomerProfileInput
{
    /// <summary>
    /// الاسم الأول للعميل.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// اسم العائلة.
    /// </summary>
    [StringLength(64)]
    public string? Surname { get; set; }

    /// <summary>
    /// البريد الإلكتروني.
    /// </summary>
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }
}
