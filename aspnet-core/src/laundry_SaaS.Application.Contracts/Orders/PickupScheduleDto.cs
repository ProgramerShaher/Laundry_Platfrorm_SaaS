using System;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات بيانات جدول وموعد استلام الملابس المعتمد للطلب (Pickup Schedule DTO).
/// يمثل لقطة تاريخية غير قابلة للتعديل تم التقاطها وقت إنشاء وتأكيد الطلب.
/// </summary>
public class PickupScheduleDto
{
    /// <summary>
    /// تاريخ الاستلام التقويمي المجدول.
    /// </summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>
    /// وقت بداية نافذة الاستلام المجدولة.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// وقت نهاية نافذة الاستلام المجدولة.
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// معرّف الفترة الزمنية المجدولة الأصلية في المغسلة.
    /// </summary>
    public Guid OriginalSlotId { get; set; }
}
