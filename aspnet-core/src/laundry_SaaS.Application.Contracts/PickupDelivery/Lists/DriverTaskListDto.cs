using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// يمثل عنصراً في قائمة مهام السائق (استلام أو توصيل) المعروضة في تطبيق السائق أو لوحة إدارة المغسلة.
/// </summary>
public class DriverTaskListDto : EntityDto<Guid>
{
    /// <summary>
    /// نوع المهمة اللوجستية (استلام Pickup أو توصيل Delivery).
    /// </summary>
    public string TaskType { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للطلب.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// رقم الطلب التتبعي.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للسائق المكلف.
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// رقم المحاولة الحالية.
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// الحالة النصية للمهمة (PickupTaskStatus أو DeliveryTaskStatus).
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// بداية النافذة الزمنية المجدولة.
    /// </summary>
    public DateTime ScheduledFrom { get; set; }

    /// <summary>
    /// نهاية النافذة الزمنية المجدولة.
    /// </summary>
    public DateTime ScheduledTo { get; set; }

    /// <summary>
    /// اسم العميل.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// رقم هاتف العميل.
    /// </summary>
    public string CustomerPhoneNumber { get; set; } = null!;

    /// <summary>
    /// العنوان المختصر للمهمة.
    /// </summary>
    public string AddressSummary { get; set; } = null!;

    /// <summary>
    /// المبلغ المطلوب تحصيله (في حالة مهمة التوصيل).
    /// </summary>
    public decimal? CashAmountToCollect { get; set; }
}
