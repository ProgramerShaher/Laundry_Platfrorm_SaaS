using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مخرجات تفاصيل الفترة الزمنية المجدولة للمغسلة (Laundry Time Slot DTO).
/// تُستخدم لعرض النوافذ المتاحة لاختيار مواعيد الاستلام أو التوصيل.
/// </summary>
public class LaundryTimeSlotDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المغسلة التابع لها.
    /// </summary>
    public Guid LaundryId { get; set; }

    /// <summary>
    /// يوم الأسبوع المخصص للفترة.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// نوع الفترة (استلام Pickup أو توصيل Delivery).
    /// </summary>
    public SlotType SlotType { get; set; }

    /// <summary>
    /// وقت بداية الفترة.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// وقت نهاية الفترة.
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// حالة تفعيل الفترة للجدولة.
    /// </summary>
    public bool IsActive { get; set; }
}
