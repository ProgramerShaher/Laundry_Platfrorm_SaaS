using Volo.Abp;

namespace laundry_SaaS.Common;

/// <summary>
/// فئة مساعدة للتحقق من صحة الإحداثيات الجغرافية والمسافات (GPS &amp; Geolocation Validator).
/// <para>
/// تفرض قيود النطاق الجغرافي:
/// <list type="bullet">
/// <item><description>خط العرض (Latitude) بين -90 و 90 درجة.</description></item>
/// <item><description>خط الطول (Longitude) بين -180 و 180 درجة.</description></item>
/// <item><description>نصف قطر التغطية بالكيلومتر أكبر قطباً من الصفر (Radius > 0).</description></item>
/// </list>
/// </para>
/// </summary>
public static class GeoLocationValidator
{
    /// <summary>
    /// يتحقق من وقوع خط العرض ضمن النطاق الجغرافي القانوني (-90 إلى 90 درجة).
    /// </summary>
    /// <param name="latitude">قيمة خط العرض.</param>
    /// <param name="paramName">اسم المعامل لأغراض التتبع.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت القيمة خارج النطاق المسموح.</exception>
    public static void ValidateLatitude(double latitude, string paramName = "latitude")
    {
        if (latitude < -90.0 || latitude > 90.0)
        {
            throw new BusinessException($"Invalid latitude value: {latitude}. Latitude must be between -90 and 90 degrees.");
        }
    }

    /// <summary>
    /// يتحقق من وقوع خط الطول ضمن النطاق الجغرافي القانوني (-180 إلى 180 درجة).
    /// </summary>
    /// <param name="longitude">قيمة خط الطول.</param>
    /// <param name="paramName">اسم المعامل لأغراض التتبع.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت القيمة خارج النطاق المسموح.</exception>
    public static void ValidateLongitude(double longitude, string paramName = "longitude")
    {
        if (longitude < -180.0 || longitude > 180.0)
        {
            throw new BusinessException($"Invalid longitude value: {longitude}. Longitude must be between -180 and 180 degrees.");
        }
    }

    /// <summary>
    /// يتحقق من صحة إحداثيات الموقع الجغرافي (خط العرض وخط الطول معاً).
    /// </summary>
    /// <param name="latitude">قيمة خط العرض.</param>
    /// <param name="longitude">قيمة خط الطول.</param>
    public static void ValidateCoordinates(double latitude, double longitude)
    {
        ValidateLatitude(latitude);
        ValidateLongitude(longitude);
    }

    /// <summary>
    /// يتحقق من صحة نصف قطر التغطية بالكيلومتر وأنه قيمة موجبة أكبر من الصفر.
    /// </summary>
    /// <param name="radiusKm">نصف القطر بالكيلومتر.</param>
    /// <param name="paramName">اسم المعامل لأغراض التتبع.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان نصف القطر أقل من أو يساوي صفر.</exception>
    public static void ValidateRadiusKm(double radiusKm, string paramName = "deliveryRadiusKm")
    {
        if (radiusKm <= 0)
        {
            throw new BusinessException($"Invalid radius value: {radiusKm}. Delivery radius must be greater than zero.");
        }
    }
}
