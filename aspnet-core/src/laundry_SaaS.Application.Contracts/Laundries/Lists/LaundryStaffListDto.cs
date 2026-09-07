using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Laundries;

/// <summary>
/// عنصر قائمة موظفي المغسلة المخصص للجداول المفهرسة وصفحات الإدارة (Staff List DTO).
/// </summary>
public class LaundryStaffListDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المستخدم في ABP Identity.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// اسم المستخدم للدخول.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// الاسم الكامل للموظف.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// المسمى الوظيفي للموظف.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// حالة تفعيل الموظف.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// تاريخ الإنشاء.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
