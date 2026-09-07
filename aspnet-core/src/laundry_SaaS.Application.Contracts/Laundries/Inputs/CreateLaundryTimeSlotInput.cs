using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات إنشاء فترة زمنية مجدولة جديدة للاستلام أو التوصيل تابعة للمغسلة.
/// </summary>
public class CreateLaundryTimeSlotInput
{
    /// <summary>
    /// يوم الأسبوع المخصص للفترة الزمنية.
    /// </summary>
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// نوع الفترة: استلام (Pickup) أو توصيل (Delivery).
    /// </summary>
    [Required]
    public SlotType SlotType { get; set; }

    /// <summary>
    /// وقت بداية الفترة الزمنية.
    /// </summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// وقت نهاية الفترة الزمنية (يجب أن يكون لاحقاً لوقت البداية).
    /// </summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// حالة تفعيل الفترة الزمنية مبدئياً.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
