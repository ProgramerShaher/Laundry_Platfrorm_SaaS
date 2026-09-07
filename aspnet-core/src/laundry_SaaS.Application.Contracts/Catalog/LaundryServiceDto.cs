using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مخرجات تفاصيل خدمة الغسيل أو المعالجة في الكتالوج (Laundry Service DTO).
/// </summary>
public class LaundryServiceDto : EntityDto<Guid>
{
    /// <summary>
    /// اسم الخدمة بالعربية.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// الرمز التعريفي للخدمة.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// وصف الخدمة.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ترتيب العرض.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// ما إذا كانت الخدمة مفعلة ونشطة في الكتالوج.
    /// </summary>
    public bool IsActive { get; set; }
}
