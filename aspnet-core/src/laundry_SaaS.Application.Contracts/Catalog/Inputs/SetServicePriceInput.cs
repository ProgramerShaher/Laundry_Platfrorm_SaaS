using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Catalog;

/// <summary>
/// مدخلات ضبط أو تحديث سعر خدمة معينة لقطعة ملابس محددة في كتالوج المغسلة.
/// </summary>
public class SetServicePriceInput
{
    /// <summary>
    /// معرّف نوع قطعة الملابس (Item Type ID).
    /// </summary>
    [Required]
    public Guid LaundryItemTypeId { get; set; }

    /// <summary>
    /// معرّف نوع الخدمة المطلوبة (Service ID).
    /// </summary>
    [Required]
    public Guid LaundryServiceId { get; set; }

    /// <summary>
    /// السعر المعتمد بالريال السعودي للقطعة الواحدة (يجب أن يكون أكبر من أو يساوي صفراً).
    /// </summary>
    [Range(0.0, 100000.0)]
    public decimal Price { get; set; }
}
