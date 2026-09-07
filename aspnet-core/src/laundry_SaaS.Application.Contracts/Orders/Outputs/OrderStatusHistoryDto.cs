using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات سجل التغيير التاريخي لحالة الطلب العامة (Order Status History DTO).
/// </summary>
public class OrderStatusHistoryDto : EntityDto<Guid>
{
    /// <summary>
    /// الحالة التي انتقل إليها الطلب.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// السبب أو الملاحظات المقترنة بتغيير الحالة إن وجدت.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// تاريخ ووقت حدوث التغيير.
    /// </summary>
    public DateTime ChangedAt { get; set; }
}
