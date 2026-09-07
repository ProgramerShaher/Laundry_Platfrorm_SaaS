using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات بند من بنود احتساب عرض السعر الأولي للطلب (Quote Item Input).
/// لا يقبل أسعاراً من العميل، بل يعتمد تحديد السعر كلياً على الكتالوج في الخادم.
/// </summary>
public class OrderQuoteItemInput
{
    /// <summary>
    /// معرّف نوع قطعة الملابس (مثل ثوب).
    /// </summary>
    [Required]
    public Guid LaundryItemTypeId { get; set; }

    /// <summary>
    /// معرّف الخدمة المطلوبة (مثل غسيل وكي).
    /// </summary>
    [Required]
    public Guid LaundryServiceId { get; set; }

    /// <summary>
    /// الكمية المطلوبة من هذه القطعة بهذه الخدمة.
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }
}
