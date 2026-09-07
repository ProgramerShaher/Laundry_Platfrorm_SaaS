using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مخرجات تسعير خدمة لقطعة ملابس محددة (Service Price DTO).
/// </summary>
public class ServicePriceDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف نوع قطعة الملابس.
    /// </summary>
    public Guid LaundryItemTypeId { get; set; }

    /// <summary>
    /// اسم نوع قطعة الملابس بالعربية.
    /// </summary>
    public string ItemTypeName { get; set; } = string.Empty;

    /// <summary>
    /// معرّف نوع الخدمة.
    /// </summary>
    public Guid LaundryServiceId { get; set; }

    /// <summary>
    /// اسم الخدمة بالعربية.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// السعر المعتمد للقطعة بالريال السعودي.
    /// </summary>
    public decimal Price { get; set; }
}
