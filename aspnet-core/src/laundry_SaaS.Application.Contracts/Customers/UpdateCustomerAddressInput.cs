using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Customers;

/// <summary>
/// مدخلات تحديث بيانات عنوان مسجل مسبقاً في حساب العميل.
/// </summary>
public class UpdateCustomerAddressInput
{
    /// <summary>
    /// عنوان أو تسمية العنوان المحدثة.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// النص الكامل للعنوان.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string AddressText { get; set; } = string.Empty;

    /// <summary>
    /// خط العرض لموقع العنوان.
    /// </summary>
    [Required]
    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    /// <summary>
    /// خط الطول لموقع العنوان.
    /// </summary>
    [Required]
    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    /// <summary>
    /// اسم الشارع.
    /// </summary>
    [StringLength(200)]
    public string? Street { get; set; }

    /// <summary>
    /// اسم أو رقم المبنى.
    /// </summary>
    [StringLength(100)]
    public string? Building { get; set; }

    /// <summary>
    /// رقم الدور أو الطابق.
    /// </summary>
    [StringLength(50)]
    public string? Floor { get; set; }

    /// <summary>
    /// رقم الشقة.
    /// </summary>
    [StringLength(50)]
    public string? Apartment { get; set; }

    /// <summary>
    /// ملاحظات أو تعليمات خاصة للمندوب.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// هل يعتبر هذا العنوان هو الافتراضي.
    /// </summary>
    public bool IsDefault { get; set; }
}
