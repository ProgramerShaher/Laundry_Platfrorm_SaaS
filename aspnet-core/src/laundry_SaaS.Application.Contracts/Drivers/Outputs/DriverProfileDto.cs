using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Drivers;

/// <summary>
/// مخرجات تفاصيل ملف السائق الكاملة (Driver Profile DTO).
/// </summary>
public class DriverProfileDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف مستخدم السائق في ABP Identity.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// اسم المستخدم لحساب السائق.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// الاسم الكامل للسائق.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني المسجل.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف السائق للتواصل.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// تفاصيل المركبة.
    /// </summary>
    public string? VehicleDetails { get; set; }

    /// <summary>
    /// هل السائق مفعل في النظام.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// هل السائق متفرغ ومتاح لاستقبال مهام جديدة حالياً.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// عدد المهام النشطة الجاري تنفيذها حالياً من قبل السائق.
    /// </summary>
    public int ActiveTasksCount { get; set; }

    /// <summary>
    /// تاريخ إنشاء ملف السائق.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
