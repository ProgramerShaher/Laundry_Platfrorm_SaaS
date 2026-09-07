using System;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات استعلام وتصفية طلبات المغسلة لإدارة المغسلة وطاقم العمل (Laundry Orders Filter Input).
/// يدعم التصفية المتعددة حسب الحالة، مرحلة المعالجة، ونطاق التواريخ مع التقسيم لصفحات.
/// </summary>
public class GetLaundryOrdersFilterInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// رقم الطلب أو جزء منه للبحث السريع (مثل ORD-2026-...).
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// تصفية حسب حالة الطلب العامة.
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// تصفية حسب مرحلة المعالجة الحالية في المغسلة.
    /// </summary>
    public ProcessingStage? ProcessingStage { get; set; }

    /// <summary>
    /// تاريخ بدء فترة الطلبات (تاريخ الإنشاء).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// تاريخ نهاية فترة الطلبات.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// تصفية الطلبات التي تملك تعديلات مالية بانتظار موافقة العميل.
    /// </summary>
    public bool? HasPendingAdjustments { get; set; }
}
