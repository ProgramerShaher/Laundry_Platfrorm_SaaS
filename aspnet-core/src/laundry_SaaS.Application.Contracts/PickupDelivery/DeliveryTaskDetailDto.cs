using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// تفاصيل مهمة توصيل الملابس النظيفة للعميل وتحصيل المبلغ نقداً عند الاستلام (COD).
/// </summary>
public class DeliveryTaskDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد للطلب المرتبط بمهمة التوصيل.
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
    /// رقم محاولة التوصيل الحالية.
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// الحالة التشغيلية الحالية لمهمة التوصيل.
    /// </summary>
    public DeliveryTaskStatus Status { get; set; }

    /// <summary>
    /// بداية النافذة الزمنية المجدولة لتوصيل الطلب للعميل.
    /// </summary>
    public DateTime ScheduledFrom { get; set; }

    /// <summary>
    /// نهاية النافذة الزمنية المجدولة لتوصيل الطلب للعميل.
    /// </summary>
    public DateTime ScheduledTo { get; set; }

    /// <summary>
    /// المبلغ النقدي المطلوب تحصيله من العميل (COD Amount To Collect).
    /// </summary>
    public decimal CashAmountToCollect { get; set; }

    /// <summary>
    /// المبلغ النقدي المحصل فعلياً بواسطة السائق إن تم التحصيل.
    /// </summary>
    public decimal? CashAmountCollected { get; set; }

    /// <summary>
    /// يشير إلى ما إذا كان قد تم تأكيد التحصيل النقدي بنجاح.
    /// </summary>
    public bool IsCashCollected { get; set; }

    /// <summary>
    /// اسم العميل المستلم.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// رقم هاتف العميل للتواصل.
    /// </summary>
    public string CustomerPhoneNumber { get; set; } = null!;

    /// <summary>
    /// عنوان تسليم الملابس للعميل (العنوان النصي الكامل).
    /// </summary>
    public string DeliveryAddressText { get; set; } = null!;

    /// <summary>
    /// خط عرض موقع التسليم.
    /// </summary>
    public decimal DeliveryLatitude { get; set; }

    /// <summary>
    /// خط طول موقع التسليم.
    /// </summary>
    public decimal DeliveryLongitude { get; set; }

    /// <summary>
    /// حالة وتفاصيل رمز التحقق المؤقت لتسليم الطلب (دون كشف التشفير أو الرمز الصريح).
    /// </summary>
    public DeliveryOtpStatusDto OtpStatus { get; set; } = null!;

    /// <summary>
    /// تاريخ ووقت تعيين المهمة للسائق.
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت قبول السائق للمهمة.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت استلام السائق للملابس من المغسلة.
    /// </summary>
    public DateTime? PickedUpFromLaundryAt { get; set; }

    /// <summary>
    /// تاريخ ووقت خروج السائق للتوصيل (OutForDelivery).
    /// </summary>
    public DateTime? OutForDeliveryAt { get; set; }

    /// <summary>
    /// تاريخ ووقت وصول السائق لموقع العميل.
    /// </summary>
    public DateTime? ArrivedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت تسليم الملابس للعميل بنجاح.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// تاريخ ووقت تعثر المهمة إن وجد.
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// سبب التعثر أو الفشل الموثق.
    /// </summary>
    public string? FailureReason { get; set; }
}
