using System;
using System.Collections.Generic;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مخرجات كتالوج الخدمات والأسعار المخصص للعميل لمغسلة معينة (Customer Catalog DTO).
/// يحتوي على معلومات المغسلة الأساسية، وقائمة الأصناف والخدمات والأسعار المعتمدة.
/// </summary>
public class CustomerCatalogDto
{
    /// <summary>
    /// معرّف المغسلة.
    /// </summary>
    public Guid LaundryId { get; set; }

    /// <summary>
    /// اسم المغسلة التجاري.
    /// </summary>
    public string LaundryName { get; set; } = string.Empty;

    /// <summary>
    /// وصف المغسلة.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// رسوم التوصيل المقررة للمغسلة.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب.
    /// </summary>
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>
    /// الساعات المقدرة لمعالجة الطلب.
    /// </summary>
    public int EstimatedProcessingHours { get; set; }

    /// <summary>
    /// قائمة أصناف الملابس والخدمات والأسعار المتاحة للطلب من هذه المغسلة.
    /// </summary>
    public List<CatalogItemDto> Items { get; set; } = new();
}
