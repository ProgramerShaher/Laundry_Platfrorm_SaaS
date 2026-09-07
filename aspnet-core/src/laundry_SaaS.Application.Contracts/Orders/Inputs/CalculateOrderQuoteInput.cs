using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات احتساب عرض السعر المسبق للطلب قبل تأكيده (Order Quote Input).
/// ترسل البنود والكميات والمغسلة لحساب المجموع ورسوم التوصيل والتحقق من الحد الأدنى.
/// </summary>
public class CalculateOrderQuoteInput
{
    /// <summary>
    /// معرّف المغسلة المراد الطلب منها.
    /// </summary>
    [Required]
    public Guid LaundryId { get; set; }

    /// <summary>
    /// قائمة بنود الملابس والخدمات والكميات المراد احتساب تكلفتها.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<OrderQuoteItemInput> Items { get; set; } = new();
}
