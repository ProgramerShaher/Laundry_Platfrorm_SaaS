using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Catalog;

/// <summary>
/// عنصر قائمة الاختيار المنسدلة لأنواع قطع الملابس (Item Type Lookup DTO).
/// يُستخدم في شاشات الإدخال والاختيار السريع.
/// </summary>
public class ItemTypeLookupDto : EntityDto<Guid>
{
    /// <summary>
    /// اسم نوع القطعة للعرض في القائمة.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// كود الصنف.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
