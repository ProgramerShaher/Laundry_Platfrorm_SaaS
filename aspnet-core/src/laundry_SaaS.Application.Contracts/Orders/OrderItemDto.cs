using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات تفاصيل بند من بنود الطلب (Order Item DTO).
/// يحتوي على البيانات الثابتة واللقطات السعرية المعتمدة للبند وقت إنشاء الطلب.
/// </summary>
public class OrderItemDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف الطلب التابع له هذا البند.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// معرّف نوع قطعة الملابس الأصلي.
    /// </summary>
    public Guid LaundryItemTypeId { get; set; }

    /// <summary>
    /// معرّف نوع الخدمة الأصلي.
    /// </summary>
    public Guid LaundryServiceId { get; set; }

    /// <summary>
    /// لقطة اسم نوع القطعة وقت إنشاء الطلب (مثل ثوب رجالي).
    /// </summary>
    public string ItemNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// لقطة اسم نوع الخدمة وقت إنشاء الطلب (مثل غسيل وكي).
    /// </summary>
    public string ServiceNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// الكمية المطلوبة من الصنف.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// لقطة سعر الوحدة بالريال السعودي وقت اعتماد الطلب.
    /// </summary>
    public decimal UnitPriceSnapshot { get; set; }

    /// <summary>
    /// الإجمالي الفرعي للبند (الكمية × سعر الوحدة).
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// ملاحظات العميل الخاصة بالبند إن وجدت.
    /// </summary>
    public string? CustomerNotes { get; set; }
}
