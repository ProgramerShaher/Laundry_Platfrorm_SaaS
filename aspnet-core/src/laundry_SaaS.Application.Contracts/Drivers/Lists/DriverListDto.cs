using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Drivers;

/// <summary>
/// عنصر قائمة السائقين للجداول الإدارية في لوحة المغسلة (Driver List DTO).
/// </summary>
public class DriverListDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف مستخدم السائق.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// الاسم الكامل للسائق.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف السائق.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// هل السائق متاح لاستقبال مهام جديدة.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// حالة تفعيل الحساب.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// عدد المهام المسندة للسائق حالياً.
    /// </summary>
    public int ActiveTasksCount { get; set; }
}
