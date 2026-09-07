using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مخرجات التفاصيل الكاملة للمغسلة المخصصة لإدارة المغسلة ذاتها (Laundry Profile DTO).
/// تتضمن بيانات التواصل، والرسوم، وساعات العمل، ونطاق التغطية، وختم التزامن.
/// </summary>
public class LaundryDetailDto : EntityDto<Guid>, IHasConcurrencyStamp
{
    /// <summary>
    /// الاسم التجاري للمغسلة.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// وصف المغسلة وخدماتها.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// رقم الهاتف المعتمد.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// مرجع الشعار في التخزين السحابي.
    /// </summary>
    public string? LogoBlobName { get; set; }

    /// <summary>
    /// خط العرض لمقر المغسلة.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// خط الطول لمقر المغسلة.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// حالة تفعيل المغسلة في النظام.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// ما إذا كانت المغسلة تستقبل طلبات جديدة حالياً.
    /// </summary>
    public bool AcceptingOrders { get; set; }

    /// <summary>
    /// رسوم التوصيل المقررة للطلبات.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب.
    /// </summary>
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>
    /// الساعات المقدرة لمعالجة الطلب قياسياً.
    /// </summary>
    public int EstimatedProcessingHours { get; set; }

    /// <summary>
    /// كائن نطاق التغطية الجغرافية والتشغيلية للمغسلة.
    /// </summary>
    public CoverageAreaDto? CoverageArea { get; set; }

    /// <summary>
    /// قائمة ساعات العمل الأسبوعية لكافة الأيام.
    /// </summary>
    public List<LaundryWorkingHourDto> WorkingHours { get; set; } = new();

    /// <summary>
    /// ختم التزامن لمنع التعديلات المتزامنة المتضاربة.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
