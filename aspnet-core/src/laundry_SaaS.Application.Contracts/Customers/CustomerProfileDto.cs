using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Customers;

/// <summary>
/// مخرجات الملف الشخصي للعميل (Customer Profile DTO).
/// تُعرض للعميل في شاشة الحساب الشخصي بالتطبيق.
/// </summary>
public class CustomerProfileDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المستخدم في ABP Identity.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// الاسم الأول للعميل.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// اسم العائلة للعميل.
    /// </summary>
    public string? Surname { get; set; }

    /// <summary>
    /// الاسم الكامل المعروض.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// رقم الهاتف المعتمد للعميل.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني المسجل.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// ما إذا كان حساب العميل نشطاً وغير معطل.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// قائمة العناوين المحفوظة في دفتر عناوين العميل.
    /// </summary>
    public List<CustomerAddressDto> Addresses { get; set; } = new();
}
