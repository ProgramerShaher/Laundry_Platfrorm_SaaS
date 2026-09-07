using System;
using laundry_SaaS.Common;

namespace laundry_SaaS.Catalog;

/// <summary>
/// فئة مساعدة لحساب المسافات الجغرافية الدقيقة بين نقطتين على سطح الأرض بالكيلومتر باستخدام صيغة هافرسين (Haversine Formula).
/// <para>
/// تُستخدم لتحديد ما إذا كان موقع العميل يقع ضمن نطاق التغطية الجغرافية للمغسلة (<see cref="Laundries.CoverageArea"/>)،
/// وحساب المسافة المعروضة للعميل في قائمة المغاسل القريبة دون الحاجة لمكتبات خارجية.
/// </para>
/// </summary>
public static class HaversineDistanceCalculator
{
    /// <summary>
    /// نصف قطر الكرة الأرضية التقريبي بالكيلومتر (Mean Earth Radius in Kilometers).
    /// </summary>
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// يحسب المسافة الجغرافية المستقيمة على سطح الأرض بين إحداثيين جغرافيين بالكيلومتر.
    /// </summary>
    /// <param name="lat1">خط العرض للنقطة الأولى (بالدرجات من -90 إلى 90).</param>
    /// <param name="lon1">خط الطول للنقطة الأولى (بالدرجات من -180 إلى 180).</param>
    /// <param name="lat2">خط العرض للنقطة الثانية (بالدرجات من -90 إلى 90).</param>
    /// <param name="lon2">خط الطول للنقطة الثانية (بالدرجات من -180 إلى 180).</param>
    /// <returns>المسافة الدقيقة بالكيلومتر.</returns>
    /// <exception cref="Volo.Abp.BusinessException">يتم رميها إذا كانت أي من الإحداثيات خارج الحدود الجغرافية المعتمدة.</exception>
    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        GeoLocationValidator.ValidateCoordinates(lat1, lon1);
        GeoLocationValidator.ValidateCoordinates(lat2, lon2);

        if (Math.Abs(lat1 - lat2) < double.Epsilon && Math.Abs(lon1 - lon2) < double.Epsilon)
        {
            return 0.0;
        }

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var lat1Rad = ToRadians(lat1);
        var lat2Rad = ToRadians(lat2);

        var sinHalfDLat = Math.Sin(dLat / 2.0);
        var sinHalfDLon = Math.Sin(dLon / 2.0);

        var a = sinHalfDLat * sinHalfDLat +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                sinHalfDLon * sinHalfDLon;

        // الحماية من الأخطاء الحسابية الناتجة عن التقريب في الفاصلة العائمة
        a = Math.Clamp(a, 0.0, 1.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// يحول الدرجات الزاوية إلى راديان (Radians).
    /// </summary>
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
