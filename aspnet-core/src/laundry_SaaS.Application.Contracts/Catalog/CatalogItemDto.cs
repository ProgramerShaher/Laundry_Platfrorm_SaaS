using System;
using System.Collections.Generic;

namespace laundry_SaaS.Catalog;

/// <summary>
/// صنف ملابس معروض في كتالوج العميل مع قائمة الخدمات المتاحة له وأسعارها.
/// </summary>
public class CatalogItemDto
{
    /// <summary>
    /// معرّف نوع قطعة الملابس.
    /// </summary>
    public Guid ItemTypeId { get; set; }

    /// <summary>
    /// اسم نوع القطعة بالعربية (مثل ثوب رجالي، عباءة، فستان).
    /// </summary>
    public string ItemTypeName { get; set; } = string.Empty;

    /// <summary>
    /// كود الصنف.
    /// </summary>
    public string ItemTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// وصف نوع القطعة.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// قائمة الخدمات المتاحة لهذه القطعة مع أسعارها المعتمدة.
    /// </summary>
    public List<CatalogServicePriceDto> AvailableServices { get; set; } = new();
}
