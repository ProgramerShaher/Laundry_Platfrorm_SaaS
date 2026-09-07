using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// بيانات إدخال التحقق من رمز التسليم السري (OTP) عند إتمام تسليم الطلب للعميل.
/// لا تحتوي إلا على الرمز المكون من 6 أرقام والمستلم من العميل مباشرة.
/// </summary>
public class VerifyDeliveryOtpInput
{
    /// <summary>
    /// رمز التحقق السري للتسليم المكون من 6 أرقام فقط.
    /// </summary>
    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "رمز التحقق يجب أن يتكون من 6 أرقام عددية فقط.")]
    public string OtpCode { get; set; } = null!;
}
