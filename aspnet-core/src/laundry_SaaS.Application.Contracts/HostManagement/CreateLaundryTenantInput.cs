using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.HostManagement;

/// <summary>
/// مدخلات إنشاء مستأجر مغسلة جديد في النظام من قبل إدارة المضيف (Host Admin).
/// تُنشئ المستأجر في ABP وتُهيئ كيان المغسلة الرئيسي وملف المدير الأول.
/// </summary>
public class CreateLaundryTenantInput
{
    /// <summary>
    /// اسم المستأجر الإنجليزي في النظام (Tenant Name)؛ يستخدم لعزل قاعدة البيانات والـ Identifier.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 2)]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// الاسم التجاري الرسمي للمغسلة بالعربية أو الإنجليزية.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string LaundryName { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للمسؤول الرئيسي للمغسلة (Admin User).
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// كلمة المرور الأولية لحساب المسؤول الرئيسي للمغسلة.
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// رقم الهاتف الرسمي للتواصل مع إدارة المغسلة.
    /// </summary>
    [Required]
    [Phone]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// خط العرض لمقر المغسلة الرئيسي.
    /// </summary>
    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    /// <summary>
    /// خط الطول لمقر المغسلة الرئيسي.
    /// </summary>
    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    /// <summary>
    /// نصف قطر التغطية المبدئي بالكيلومترات لاستقبال الطلبات.
    /// </summary>
    [Range(0.1, 200.0)]
    public double RadiusKm { get; set; } = 10.0;

    /// <summary>
    /// رسوم التوصيل الافتراضية للطلبات الموجهة للمغسلة.
    /// </summary>
    [Range(0.0, 10000.0)]
    public decimal DeliveryFee { get; set; } = 0m;

    /// <summary>
    /// الحد الأدنى لقيمة الطلب ليتم قبوله.
    /// </summary>
    [Range(0.0, 10000.0)]
    public decimal MinimumOrderAmount { get; set; } = 0m;

    /// <summary>
    /// الساعات المقدرة لمعالجة وتجهيز الطلب قياسياً.
    /// </summary>
    [Range(1, 720)]
    public int EstimatedProcessingHours { get; set; } = 24;
}
