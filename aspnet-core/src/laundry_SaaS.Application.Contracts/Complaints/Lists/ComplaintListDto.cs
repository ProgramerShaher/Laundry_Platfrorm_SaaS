using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Complaints;

/// <summary>
/// يمثل عنصراً في قائمة الشكاوى المعروضة للعميل أو إدارة المغسلة.
/// </summary>
public class ComplaintListDto : EntityDto<Guid>
{
    /// <summary>
    /// الرقم التسلسلي للشكوى (مثل: CMP-2026-001).
    /// </summary>
    public string ComplaintNumber { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للطلب.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// رقم الطلب التتبعي للعرض.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// اسم العميل.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// الحالة التشغيلية للشكوى.
    /// </summary>
    public ComplaintStatus Status { get; set; }

    /// <summary>
    /// تصنيف نوع الشكوى.
    /// </summary>
    public ComplaintType Type { get; set; }

    /// <summary>
    /// موضوع أو عنوان الشكوى.
    /// </summary>
    public string Subject { get; set; } = null!;

    /// <summary>
    /// مبلغ التعويض المالي إن وجد.
    /// </summary>
    public decimal CompensationAmount { get; set; }

    /// <summary>
    /// تاريخ ووقت تقديم الشكوى.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
