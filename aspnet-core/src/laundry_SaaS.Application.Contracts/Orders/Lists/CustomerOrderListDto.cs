using System;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// عنصر قائمة طلبات العميل المخصص لشاشات تصفح "طلباتي" في تطبيق العميل (Customer Order List Item DTO).
/// </summary>
public class CustomerOrderListDto : EntityDto<Guid>
{
    /// <summary>
    /// رقم الطلب المرجعي.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// اسم المغسلة المنفذة للطلب.
    /// </summary>
    public string LaundryName { get; set; } = string.Empty;

    /// <summary>
    /// الحالة العامة للطلب.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// مرحلة المعالجة الحالية للملابس إن وجدت.
    /// </summary>
    public ProcessingStage? ProcessingStage { get; set; }

    /// <summary>
    /// إجمالي قيمة الطلب بالريال السعودي.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// إجمالي عدد القطع المطلوبة في الطلب.
    /// </summary>
    public int TotalItemsCount { get; set; }

    /// <summary>
    /// تاريخ الاستلام المجدول.
    /// </summary>
    public DateOnly PickupDate { get; set; }

    /// <summary>
    /// تاريخ إنشاء الطلب.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
