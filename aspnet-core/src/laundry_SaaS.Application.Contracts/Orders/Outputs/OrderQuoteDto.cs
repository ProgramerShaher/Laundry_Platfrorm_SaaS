using System;
using System.Collections.Generic;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات عرض السعر المسبق للطلب المحسوب على الخادم (Order Quote DTO).
/// تُعرض للعميل في شاشة مراجعة السلة قبل تأكيد الحجز.
/// </summary>
public class OrderQuoteDto
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
    /// تفاصيل تسعير كافة البنود المطلوبة.
    /// </summary>
    public List<OrderQuoteItemDetailDto> Items { get; set; } = new();

    /// <summary>
    /// المجموع الفرعي لتكلفة غسيل الملابس (Subtotal).
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// رسوم التوصيل المقررة للمغسلة.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// قيمة الخصم إن وجد (افتراضياً 0 في هذه المرحلة).
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// الإجمالي الكلي المطلوب دفعه نقداً عند الاستلام (Total).
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// الحد الأدنى المشترط للطلب في هذه المغسلة.
    /// </summary>
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>
    /// ما إذا كان المجموع الفرعي يحقق الحد الأدنى المطلوب للطلب.
    /// </summary>
    public bool MeetsMinimumOrderAmount { get; set; }

    /// <summary>
    /// الساعات المقدرة لمعالجة وتجهيز الملابس.
    /// </summary>
    public int EstimatedProcessingHours { get; set; }
}
