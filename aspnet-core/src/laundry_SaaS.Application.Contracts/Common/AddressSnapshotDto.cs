namespace laundry_SaaS.Common;

/// <summary>
/// لقطة تاريخية مجردة لعنوان العميل (Address Snapshot DTO).
/// تُحفظ مع الطلب لضمان ثبات بيانات الاستلام أو التوصيل حتى لو تم تعديل العنوان الأصلي للمستخدم لاحقاً.
/// </summary>
public class AddressSnapshotDto
{
    /// <summary>
    /// النص التفصيلي الكامل للعنوان.
    /// </summary>
    public string AddressText { get; set; } = string.Empty;

    /// <summary>
    /// إحداثي خط العرض لموقع العنوان الجغرافي.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// إحداثي خط الطول لموقع العنوان الجغرافي.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// اسم الشارع اختياري.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// اسم أو رقم المبنى اختياري.
    /// </summary>
    public string? Building { get; set; }

    /// <summary>
    /// رقم الدور أو الطابق اختياري.
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// رقم الشقة أو المكتب اختياري.
    /// </summary>
    public string? Apartment { get; set; }

    /// <summary>
    /// ملاحظات أو تعليمات إضافية للوصول للعنوان اختياري.
    /// </summary>
    public string? Notes { get; set; }
}
