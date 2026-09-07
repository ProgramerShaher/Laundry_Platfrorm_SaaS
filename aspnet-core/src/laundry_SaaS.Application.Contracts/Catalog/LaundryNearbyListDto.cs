using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Catalog;

/// <summary>
/// عنصر قائمة المغاسل القريبة المتاحة للعميل (Nearby Laundry List DTO).
/// يعرض المسافة المحسوبة، ورسوم التوصيل، وحالة استقبال الطلبات.
/// </summary>
public class LaundryNearbyListDto : EntityDto<Guid>
{
    /// <summary>
    /// الاسم التجاري للمغسلة.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// وصف مختصر للمغسلة.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// اسم مرجع الشعار السحابي.
    /// </summary>
    public string? LogoBlobName { get; set; }

    /// <summary>
    /// المسافة التقريبية بالكيلومترات من موقع العميل المحدد.
    /// </summary>
    public double DistanceKm { get; set; }

    /// <summary>
    /// رسوم التوصيل المقررة للطلب من هذه المغسلة.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب.
    /// </summary>
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>
    /// الساعات المقدرة لتجهيز الطلب.
    /// </summary>
    public int EstimatedProcessingHours { get; set; }

    /// <summary>
    /// ما إذا كانت المغسلة تقبل وتستقبل طلبات جديدة حالياً.
    /// </summary>
    public bool AcceptingOrders { get; set; }
}
