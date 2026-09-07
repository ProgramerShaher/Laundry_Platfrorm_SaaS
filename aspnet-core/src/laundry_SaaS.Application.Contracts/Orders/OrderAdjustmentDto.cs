using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات تفاصيل التعديل المالي للطلب الناتج عن الفحص الفني (Order Adjustment DTO).
/// </summary>
public class OrderAdjustmentDto : EntityDto<Guid>
{
    /// <summary>
    /// معرّف الطلب المعدل.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// معرّف محضر الفحص الفني المسبب للتعديل.
    /// </summary>
    public Guid InspectionId { get; set; }

    /// <summary>
    /// سبب التعديل الموثق.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// الإجمالي الكلي القديم للطلب قبل التعديل.
    /// </summary>
    public decimal OldTotal { get; set; }

    /// <summary>
    /// الإجمالي الكلي الجديد للطلب بعد التعديل.
    /// </summary>
    public decimal NewTotal { get; set; }

    /// <summary>
    /// قيمة الفرق المالي (الموجب يعني زيادة، والسالب يعني تخفيض لصالح العميل).
    /// </summary>
    public decimal DifferenceAmount { get; set; }

    /// <summary>
    /// حالة اعتماد التعديل (بانتظار موافقة العميل Pending، معتمد Approved، مرفوض Rejected).
    /// </summary>
    public OrderAdjustmentStatus Status { get; set; }

    /// <summary>
    /// تاريخ ووقت إنشاء التعديل.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// تاريخ ووقت اتخاذ القرار بالاعتماد أو الرفض.
    /// </summary>
    public DateTime? RespondedAt { get; set; }
}
