using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مخرجات تفاصيل نوع قطعة الملابس في كتالوج المغسلة (Item Type DTO).
/// </summary>
public class LaundryItemTypeDto : EntityDto<Guid>
{
    /// <summary>
    /// اسم نوع القطعة بالعربية.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// الرمز التعريفي المختصر.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// وصف نوع القطعة.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ترتيب العرض في القوائم.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// ما إذا كان الصنف مفعلاً ونشطاً في الكتالوج.
    /// </summary>
    public bool IsActive { get; set; }
}
