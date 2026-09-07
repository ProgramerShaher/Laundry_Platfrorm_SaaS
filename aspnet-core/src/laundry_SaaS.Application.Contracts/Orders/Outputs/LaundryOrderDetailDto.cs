using System;
using System.Collections.Generic;
using laundry_SaaS.Bags;
using laundry_SaaS.Common;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات تفاصيل الطلب التشغيلية الكاملة المخصصة لإدارة وطاقم عمل المغسلة (Laundry Order Detail DTO).
/// تتضمن بيانات العميل للتواصل، والعناوين، والبنود، والتعديلات، وسجلات المراحل، والحقائب، ومعرّف الفحص، وختم التزامن.
/// </summary>
public class LaundryOrderDetailDto : EntityDto<Guid>, IHasConcurrencyStamp
{
    /// <summary>
    /// معرّف المستأجر للمغسلة.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// رقم الطلب الرسمي.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// معرّف العميل صاحب الطلب.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// اسم العميل المعروض.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف العميل للتواصل والتنسيق.
    /// </summary>
    public string CustomerPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// الحالة التشغيلية العامة للطلب.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// مرحلة المعالجة التشغيلية داخل المغسلة.
    /// </summary>
    public ProcessingStage? ProcessingStage { get; set; }

    /// <summary>
    /// عنوان استلام الملابس.
    /// </summary>
    public AddressSnapshotDto PickupAddress { get; set; } = new();

    /// <summary>
    /// عنوان تسليم الملابس بعد الانتهاء.
    /// </summary>
    public AddressSnapshotDto? DeliveryAddress { get; set; }

    /// <summary>
    /// جدول وموعد الاستلام المعتمد للطلب.
    /// </summary>
    public PickupScheduleDto PickupSchedule { get; set; } = new();

    /// <summary>
    /// المجموع الفرعي للطلب.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// رسوم التوصيل المقررة.
    /// </summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// قيمة الخصم.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// الإجمالي الكلي للطلب بالريال السعودي.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// ملاحظات العميل المدونة على الطلب.
    /// </summary>
    public string? CustomerNotes { get; set; }

    /// <summary>
    /// سبب إلغاء الطلب إن وجد.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// معرّف محضر الفحص الفني المرتبط بهذا الطلب إن وجد.
    /// </summary>
    public Guid? InspectionId { get; set; }

    /// <summary>
    /// قائمة بنود الملابس والخدمات للطلب.
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();

    /// <summary>
    /// قائمة التعديلات المالية المسجلة على الطلب.
    /// </summary>
    public List<OrderAdjustmentDto> Adjustments { get; set; } = new();

    /// <summary>
    /// السجل التاريخي لحالات الطلب العامة.
    /// </summary>
    public List<OrderStatusHistoryDto> StatusHistories { get; set; } = new();

    /// <summary>
    /// السجل التاريخي لمراحل المعالجة داخل المغسلة.
    /// </summary>
    public List<ProcessingStageHistoryDto> ProcessingStageHistories { get; set; } = new();

    /// <summary>
    /// قائمة الحقائب المرتبطة بالطلب.
    /// </summary>
    public List<BagListDto> Bags { get; set; } = new();

    /// <summary>
    /// ختم التزامن للتحكم بالتحديث المتزامن ومنع تضارب تعديلات طاقم العمل.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = string.Empty;

    /// <summary>
    /// تاريخ ووقت إنشاء الطلب.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
