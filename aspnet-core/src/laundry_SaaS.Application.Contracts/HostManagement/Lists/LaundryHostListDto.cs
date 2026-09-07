using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.HostManagement;

/// <summary>
/// عنصر قائمة استعراض المغاسل لإدارة المضيف المركزي (Host Laundry List DTO).
/// مُخصص للجداول والقوائم ذات الصفحات (Paginated Tables).
/// </summary>
public class LaundryHostListDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المستأجر في نظام ABP.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// اسم المستأجر الإنجليزي في النظام.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// الاسم التجاري للمغسلة.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// رقم الهاتف المسجل.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// حالة تفعيل المغسلة في المنصة.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// حالة استقبال الطلبات حالياً.
    /// </summary>
    public bool AcceptingOrders { get; set; }

    /// <summary>
    /// تاريخ تسجيل المغسلة في المنصة.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
