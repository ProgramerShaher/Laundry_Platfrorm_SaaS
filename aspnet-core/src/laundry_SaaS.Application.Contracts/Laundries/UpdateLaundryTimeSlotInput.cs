using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات تعديل أوقات أو حالة تفعيل فترة زمنية مجدولة موجودة في المغسلة.
/// </summary>
public class UpdateLaundryTimeSlotInput
{
    /// <summary>
    /// وقت بداية الفترة الزمنية الجديد.
    /// </summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// وقت نهاية الفترة الزمنية الجديد (يجب أن يسبق وقت البداية).
    /// </summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// حالة تفعيل الفترة الزمنية.
    /// </summary>
    public bool IsActive { get; set; }
}
