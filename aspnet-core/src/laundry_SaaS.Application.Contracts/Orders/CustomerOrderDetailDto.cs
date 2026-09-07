using System;
using System.Collections.Generic;
using laundry_SaaS.Common;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات تفاصيل الطلب الكاملة المخصصة للعميل (Customer Order Detail DTO).
/// <para>
/// حماية الخصوصية وعزل البيانات:
/// لا يكشف هذا الكائن نهائياً عن معرّفات المستأجر (TenantId)، أو معرّف العميل المباشر (CustomerId)،
/// أو معرّفات طاقم العمل، أو الملاحظات التشغيلية الداخلية، أو أختام التزامن غير اللازمة للعميل.
/// </para>
/// </summary>
public class CustomerOrderDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// رقم الطلب المرجعي المعروض (مثل ORD-2026-ABCD1234).
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// الحالة التشغيلية العامة للطلب.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// مرحلة المعالجة الحالية داخل المغسلة (إن كان الطلب في مرحلة الغسيل/المعالجة).
    /// </summary>
    public ProcessingStage? ProcessingStage { get; set; }

    /// <summary>
    /// معرّف المغسلة المنفذة للطلب.
    /// </summary>
    public Guid LaundryId { get; set; }

    /// <summary>
    /// الاسم التجاري للمغسلة.
    /// </summary>
    public string LaundryName { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف المغسلة للتواصل.
    /// </summary>
    public string LaundryPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// عنوان استلام الملابس من العميل.
    /// </summary>
    public AddressSnapshotDto PickupAddress { get; set; } = new();

    /// <summary>
    /// عنوان توصيل الملابس بعد الانتهاء.
    /// </summary>
    public AddressSnapshotDto? DeliveryAddress { get; set; }

    /// <summary>
    /// جدول وموعد استلام الملابس المعتمد للطلب.
    /// </summary>
    public PickupScheduleDto PickupSchedule { get; set; } = new();

    /// <summary>
    /// قائمة بنود الملابس والخدمات والكميات والأسعار المعتمدة للطلب.
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();

    /// <summary>
    /// المجموع الفرعي لتكلفة الخدمات.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// رسوم التوصيل المعتمدة.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// قيمة الخصم المالي المطبق.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// الإجمالي الكلي المطلوب دفعه نقداً عند الاستلام (Total).
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// ملاحظات العميل المدونة عند إنشاء الطلب.
    /// </summary>
    public string? CustomerNotes { get; set; }

    /// <summary>
    /// قائمة التعديلات المالية المسجلة على الطلب إن وجدت.
    /// </summary>
    public List<OrderAdjustmentDto> Adjustments { get; set; } = new();

    /// <summary>
    /// السجل التاريخي لتغير حالات الطلب.
    /// </summary>
    public List<OrderStatusHistoryDto> StatusHistories { get; set; } = new();

    /// <summary>
    /// سبب إلغاء الطلب في حال تم إلغاؤه.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// تاريخ ووقت إنشاء الطلب.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
