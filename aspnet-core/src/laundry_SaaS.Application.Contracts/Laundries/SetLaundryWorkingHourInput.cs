using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات ضبط ساعات العمل ليوم محدد من أيام الأسبوع في المغسلة.
/// </summary>
public class SetLaundryWorkingHourInput
{
    /// <summary>
    /// يوم الأسبوع المراد ضبط ساعات عمله (0 للأحد، 6 للسبت).
    /// </summary>
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// ما إذا كانت المغسلة مفتوحة وتستقبل العمل في هذا اليوم.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// وقت بدء العمل اليومي (إلزامي إذا كان اليوم مفتوحاً IsOpen = true).
    /// </summary>
    public TimeOnly? OpenTime { get; set; }

    /// <summary>
    /// وقت انتهاء العمل اليومي (إلزامي إذا كان اليوم مفتوحاً IsOpen = true).
    /// </summary>
    public TimeOnly? CloseTime { get; set; }
}
