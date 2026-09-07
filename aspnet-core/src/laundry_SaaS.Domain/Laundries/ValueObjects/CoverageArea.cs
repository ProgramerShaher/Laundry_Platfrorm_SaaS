using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace laundry_SaaS.Laundries;

/// <summary>
/// كائن قيمة (Value Object) يمثل النطاق الجغرافي والتشغيلي الذي تغطيه المغسلة لخدمات الاستلام والتوصيل.
/// <para>
/// لا يمتلك هذا الكائن معرّفاً مستقلاً (No Id) ولا مستودعاً خاصاً به (No Repository)،
/// ويتم حفظه كـ Owned Type مدمج داخل جدول المغسلة (AppLaundries).
/// تتحدد المساواة بين كائنين بناءً على تطابق جميع قيمه الذرية (Atomic Values).
/// </para>
/// </summary>
public class CoverageArea : ValueObject
{
    /// <summary>
    /// خط عرض نقطة المركز للمغسلة أو مركز نطاق التغطية.
    /// </summary>
    public double CenterLatitude { get; private set; }

    /// <summary>
    /// خط طول نقطة المركز للمغسلة أو مركز نطاق التغطية.
    /// </summary>
    public double CenterLongitude { get; private set; }

    /// <summary>
    /// نصف قطر دائرة التغطية المسموح بالتوصيل إليها محسوباً بالكيلومتر (Km).
    /// </summary>
    public double DeliveryRadiusKm { get; private set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب المالية المطلوبة لقبول التوصيل ضمن هذا النطاق.
    /// </summary>
    public decimal MinimumOrderAmount { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private CoverageArea()
    {
    }

    /// <summary>
    /// يُنشئ نطاق تغطية جغرافي وتشغيلي جديد للمغسلة مع التحقق من صحة الإحداثيات ونصف القطر والحد الأدنى للطلب.
    /// </summary>
    /// <param name="centerLatitude">خط عرض نقطة المركز (-90 إلى 90).</param>
    /// <param name="centerLongitude">خط طول نقطة المركز (-180 إلى 180).</param>
    /// <param name="deliveryRadiusKm">نصف قطر التغطية بالكيلومتر (أكبر قطباً من صفر).</param>
    /// <param name="minimumOrderAmount">الحد الأدنى لقيمة الطلب (لا يقبل قيمة سالبة).</param>
    /// <exception cref="Volo.Abp.BusinessException">يتم رميها إذا كانت الإحداثيات أو المسافة أو المبالغ خارج الحدود القانونية.</exception>
    public CoverageArea(double centerLatitude, double centerLongitude, double deliveryRadiusKm, decimal minimumOrderAmount = 0)
    {
        Common.GeoLocationValidator.ValidateCoordinates(centerLatitude, centerLongitude);
        Common.GeoLocationValidator.ValidateRadiusKm(deliveryRadiusKm);

        if (minimumOrderAmount < 0)
        {
            throw new Volo.Abp.BusinessException("MinimumOrderAmount must be greater than or equal to zero.");
        }

        CenterLatitude = centerLatitude;
        CenterLongitude = centerLongitude;
        DeliveryRadiusKm = deliveryRadiusKm;
        MinimumOrderAmount = minimumOrderAmount;
    }

    /// <summary>
    /// يُرجع القيم الذرية المحددة لهوية هذا الكائن لاختبار المساواة القائمة على القيم.
    /// </summary>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return CenterLatitude;
        yield return CenterLongitude;
        yield return DeliveryRadiusKm;
        yield return MinimumOrderAmount;
    }
}
