using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات ضبط وتحديث نطاق التغطية الجغرافية والتشغيلية للمغسلة.
/// </summary>
public class SetCoverageAreaInput
{
    /// <summary>
    /// خط العرض لمركز نطاق التغطية الجديد.
    /// </summary>
    [Range(-90.0, 90.0)]
    public double CenterLatitude { get; set; }

    /// <summary>
    /// خط الطول لمركز نطاق التغطية الجديد.
    /// </summary>
    [Range(-180.0, 180.0)]
    public double CenterLongitude { get; set; }

    /// <summary>
    /// نصف قطر التغطية بالكيلومترات (Radius in Km).
    /// </summary>
    [Range(0.1, 200.0)]
    public double RadiusKm { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب ضمن هذا النطاق بالريال السعودي.
    /// </summary>
    [Range(0.0, 10000.0)]
    public decimal MinimumOrderAmount { get; set; }
}
