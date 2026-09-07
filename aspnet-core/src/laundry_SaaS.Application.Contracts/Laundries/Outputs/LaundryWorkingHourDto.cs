using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مخرجات تفاصيل جدول ساعات العمل ليوم محدد للمغسلة (Working Hour DTO).
/// </summary>
public class LaundryWorkingHourDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المغسلة التابع لها.
    /// </summary>
    public Guid LaundryId { get; set; }

    /// <summary>
    /// يوم الأسبوع (مثل السبت، الأحد).
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// هل المغسلة مفتوحة في هذا اليوم.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// وقت بدء العمل اليومي.
    /// </summary>
    public TimeOnly? OpenTime { get; set; }

    /// <summary>
    /// وقت انتهاء العمل اليومي.
    /// </summary>
    public TimeOnly? CloseTime { get; set; }
}
