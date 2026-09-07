using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// تفاصيل مهمة استلام الملابس من العميل من منظور السائق وإدارة المغسلة.
/// </summary>
public class PickupTaskDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد للطلب المرتبط بمهمة الاستلام.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// رقم الطلب التتبعي للعرض.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للسائق المكلف بالمهمة إن وجد.
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// اسم السائق المكلف بالمهمة إن وجد.
    /// </summary>
    public string? DriverName { get; set; }

    /// <summary>
    /// رقم محاولة الاستلام الحالية.
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// الحالة التشغيلية الحالية لمهمة الاستلام.
    /// </summary>
    public PickupTaskStatus Status { get; set; }

    /// <summary>
    /// بداية النافذة الزمنية المجدولة لقدوم السائق للاستلام.
    /// </summary>
    public DateTime ScheduledFrom { get; set; }

    /// <summary>
    /// نهاية النافذة الزمنية المجدولة لقدوم السائق للاستلام.
    /// </summary>
    public DateTime ScheduledTo { get; set; }

    /// <summary>
    /// اسم العميل صاحب الطلب.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// رقم هاتف العميل للتواصل.
    /// </summary>
    public string CustomerPhoneNumber { get; set; } = null!;

    /// <summary>
    /// عنوان استلام الملابس من العميل (العنوان النصي الكامل).
    /// </summary>
    public string PickupAddressText { get; set; } = null!;

    /// <summary>
    /// خط عرض موقع الاستلام.
    /// </summary>
    public decimal PickupLatitude { get; set; }

    /// <summary>
    /// خط طول موقع الاستلام.
    /// </summary>
    public decimal PickupLongitude { get; set; }

    /// <summary>
    /// تاريخ ووقت تعيين المهمة للسائق.
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت قبول السائق للمهمة.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت وصول السائق لموقع العميل.
    /// </summary>
    public DateTime? ArrivedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت استلام الملابس من العميل بنجاح.
    /// </summary>
    public DateTime? PickedUpAt { get; set; }

    /// <summary>
    /// تاريخ ووقت إتمام المهمة بتسليم الحقائب للمغسلة.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت تعثر المهمة إن وجد.
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// سبب التعثر أو الفشل الموثق.
    /// </summary>
    public string? FailureReason { get; set; }
}
