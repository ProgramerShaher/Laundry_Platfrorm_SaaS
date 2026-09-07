namespace laundry_SaaS.HostManagement;

/// <summary>
/// إحصائيات عامة لمنصة المغاسل السحابية معروضة لإدارة المضيف المركزي (Platform Statistics DTO).
/// تلخص مؤشرات الأداء والتشغيل والنشاط عبر كافة المستأجرين.
/// </summary>
public class PlatformStatisticsDto
{
    /// <summary>
    /// إجمالي عدد المغاسل والمستأجرين المسجلين في المنصة.
    /// </summary>
    public int TotalLaundriesCount { get; set; }

    /// <summary>
    /// عدد المغاسل النشطة والمتاحة للعمل حالياً.
    /// </summary>
    public int ActiveLaundriesCount { get; set; }

    /// <summary>
    /// إجمالي عدد العملاء المسجلين في المنصة عبر كل المناطق.
    /// </summary>
    public long TotalCustomersCount { get; set; }

    /// <summary>
    /// إجمالي عدد السائقين المسجلين في النظام.
    /// </summary>
    public int TotalDriversCount { get; set; }

    /// <summary>
    /// إجمالي عدد الطلبات المنفذة عبر المنصة بكافة حالاتها.
    /// </summary>
    public long TotalOrdersCount { get; set; }

    /// <summary>
    /// عدد الطلبات النشطة الجاري تنفيذها حالياً (قيد الاستلام أو المعالجة أو التوصيل).
    /// </summary>
    public long ActiveOrdersCount { get; set; }

    /// <summary>
    /// عدد الطلبات المكتملة والمسلمة بنجاح للعملاء.
    /// </summary>
    public long CompletedOrdersCount { get; set; }

    /// <summary>
    /// إجمالي القيمة المالية للطلبات المكتملة في المنصة (بالريال السعودي).
    /// </summary>
    public decimal TotalRevenueAmount { get; set; }

    /// <summary>
    /// إجمالي المبالغ النقدية المحصلة فعلياً عند الاستلام (COD).
    /// </summary>
    public decimal TotalCodCollectedAmount { get; set; }

    /// <summary>
    /// عدد الشكاوى المفتوحة حالياً وبانتظار الحل.
    /// </summary>
    public int PendingComplaintsCount { get; set; }
}
