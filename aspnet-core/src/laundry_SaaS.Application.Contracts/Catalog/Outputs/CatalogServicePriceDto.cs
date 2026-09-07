using System;

namespace laundry_SaaS.Catalog;

/// <summary>
/// تسعير خدمة محددة لقطعة ملابس معروض في كتالوج العميل.
/// </summary>
public class CatalogServicePriceDto
{
    /// <summary>
    /// معرّف الخدمة (مثل غسيل وكي).
    /// </summary>
    public Guid ServiceId { get; set; }

    /// <summary>
    /// اسم الخدمة المعروض بالعربية.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// كود الخدمة.
    /// </summary>
    public string ServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// سعر الخدمة للقطعة الواحدة بالريال السعودي.
    /// </summary>
    public decimal Price { get; set; }
}
