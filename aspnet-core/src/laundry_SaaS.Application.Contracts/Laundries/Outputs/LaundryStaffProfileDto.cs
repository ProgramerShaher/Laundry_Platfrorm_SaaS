using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مخرجات التفاصيل الكاملة لملف موظف المغسلة (Laundry Staff Profile DTO).
/// </summary>
public class LaundryStaffProfileDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المستخدم المرتبط في ABP Identity.
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
    /// البريد الإلكتروني للموظف.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// رقم الهاتف المعتمد.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// المسمى الوظيفي.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// ما إذا كان الموظف نشطاً في المغسلة.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// الملاحظات الإدارية.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// تاريخ تعيين أو إنشاء ملف الموظف.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
