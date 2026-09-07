namespace laundry_SaaS.Laundries;

/// <summary>
/// بيانات نطاق التغطية الجغرافية والتشغيلية للمغسلة (Coverage Area DTO).
/// </summary>
public class CoverageAreaDto
{
    /// <summary>
    /// خط العرض لمركز نطاق التغطية.
    /// </summary>
    public double CenterLatitude { get; set; }

    /// <summary>
    /// خط الطول لمركز نطاق التغطية.
    /// </summary>
    public double CenterLongitude { get; set; }

    /// <summary>
    /// نصف قطر التغطية بالكيلومترات (Radius in Km).
    /// </summary>
    public double RadiusKm { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب ضمن هذا النطاق بالريال السعودي.
    /// </summary>
    public decimal MinimumOrderAmount { get; set; }
}
