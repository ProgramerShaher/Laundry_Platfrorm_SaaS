using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Bags;

/// <summary>
/// بيانات إدخال مسح رمز الاستجابة السريعة (QR Code) لحقيبة الغسيل.
/// لا تحتوي على نوع الحدث (BagEventType) لحماية مسار الحالة التشغيلية من القفز العشوائي؛ فالخادم يحدد الحدث تلقائياً وفق سياق العملية ودور المستخدم.
/// </summary>
public class ScanBagQrInput
{
    /// <summary>
    /// رمز الاستجابة السريعة (QR Code) للحقيبة الممسوح ضوئياً.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string QrCode { get; set; } = null!;

    /// <summary>
    /// خط عرض موقع المسح الفعلي إن وجد.
    /// </summary>
    public double? LocationLatitude { get; set; }

    /// <summary>
    /// خط طول موقع المسح الفعلي إن وجد.
    /// </summary>
    public double? LocationLongitude { get; set; }

    /// <summary>
    /// ملاحظات إضافية أثناء المسح إن وجدت.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
