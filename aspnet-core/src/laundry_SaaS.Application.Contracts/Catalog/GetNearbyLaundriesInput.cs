using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مدخلات البحث عن المغاسل القريبة من موقع العميل الجغرافي.
/// </summary>
public class GetNearbyLaundriesInput
{
    /// <summary>
    /// خط العرض لموقع العميل الحالي أو المحدد.
    /// </summary>
    [Required]
    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    /// <summary>
    /// خط الطول لموقع العميل الحالي أو المحدد.
    /// </summary>
    [Required]
    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    /// <summary>
    /// أقصى نصف قطر للبحث بالكيلومترات (الافتراضي 20 كم).
    /// </summary>
    [Range(0.5, 100.0)]
    public double MaxDistanceKm { get; set; } = 20.0;
}
