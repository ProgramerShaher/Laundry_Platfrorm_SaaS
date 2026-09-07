using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.HostManagement;

/// <summary>
/// مخرجات تفاصيل المغسلة والمستأجر المخصصة لإدارة المنصة المركزية (Host Admin).
/// تحتوي على بيانات المستأجر وحالته الإدارية والتشغيلية في المنصة.
/// </summary>
public class LaundryHostDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف المستأجر المرتبط في نظام ABP.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// اسم المستأجر في نظام إدارة المستأجرين.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// الاسم التجاري للمغسلة.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف التواصل الرسمي.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للتواصل.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// وصف المغسلة وخدماتها.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// خط العرض لموقع المغسلة.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// خط الطول لموقع المغسلة.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// ما إذا كانت المغسلة مفعلة في النظام من قبل إدارة المنصة.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// ما إذا كانت المغسلة تستقبل طلبات جديدة حالياً.
    /// </summary>
    public bool AcceptingOrders { get; set; }

    /// <summary>
    /// رسوم التوصيل المحددة للمغسلة.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب.
    /// </summary>
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>
    /// عدد الساعات المقدرة لتجهيز الطلب.
    /// </summary>
    public int EstimatedProcessingHours { get; set; }

    /// <summary>
    /// إجمالي عدد الطلبات المسجلة لهذه المغسلة منذ إنشائها.
    /// </summary>
    public long TotalOrdersCount { get; set; }

    /// <summary>
    /// تاريخ ووقت إنشاء المغسلة في المنصة.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
