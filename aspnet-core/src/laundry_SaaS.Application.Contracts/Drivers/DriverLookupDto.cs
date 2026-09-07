using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Drivers;

/// <summary>
/// عنصر قائمة اختيار السائقين السريعة لإسناد المهام (Driver Lookup DTO).
/// </summary>
public class DriverLookupDto : EntityDto<Guid>
{
    /// <summary>
    /// اسم السائق المعروض في القائمة.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف السائق.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// ما إذا كان متاحاً حالياً للتعيين الفوري.
    /// </summary>
    public bool IsAvailable { get; set; }
}
