using System;

namespace laundry_SaaS.Orders;

/// <summary>
/// تفاصيل تسعير بند محدد ضمن عرض السعر المحسوب في الخادم.
/// </summary>
public class OrderQuoteItemDetailDto
{
    /// <summary>
    /// معرّف نوع قطعة الملابس.
    /// </summary>
    public Guid LaundryItemTypeId { get; set; }

    /// <summary>
    /// اسم نوع القطعة بالعربية.
    /// </summary>
    public string ItemTypeName { get; set; } = string.Empty;

    /// <summary>
    /// معرّف نوع الخدمة.
    /// </summary>
    public Guid LaundryServiceId { get; set; }

    /// <summary>
    /// اسم نوع الخدمة بالعربية.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// الكمية المحددة.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// سعر الوحدة المعتمد بالريال السعودي المستخرج من الكتالوج.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// إجمالي تكلفة البند (الكمية × سعر الوحدة).
    /// </summary>
    public decimal LineTotal { get; set; }
}
