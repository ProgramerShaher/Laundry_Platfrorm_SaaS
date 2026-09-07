using System;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// عنصر قائمة طلبات المغسلة المخصص لجداول لوحة إدارة الطلبات لطاقم المغسلة (Laundry Order List Item DTO).
/// </summary>
public class LaundryOrderListDto : EntityDto<Guid>
{
    /// <summary>
    /// رقم الطلب الرسمي.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// معرّف العميل صاحب الطلب.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// اسم العميل.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف العميل.
    /// </summary>
    public string CustomerPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// الحالة العامة للطلب.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// مرحلة المعالجة التشغيلية الحالية.
    /// </summary>
    public ProcessingStage? ProcessingStage { get; set; }

    /// <summary>
    /// إجمالي قيمة الطلب بالريال السعودي.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// عدد بنود الملابس في الطلب.
    /// </summary>
    public int ItemsCount { get; set; }

    /// <summary>
    /// تاريخ الاستلام المجدول.
    /// </summary>
    public DateOnly PickupDate { get; set; }

    /// <summary>
    /// ما إذا كان الطلب بانتظار موافقة العميل على تعديل مالي.
    /// </summary>
    public bool HasPendingAdjustment { get; set; }

    /// <summary>
    /// تاريخ ووقت إنشاء الطلب.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
