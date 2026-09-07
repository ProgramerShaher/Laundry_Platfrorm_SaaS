using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Inspections;

/// <summary>
/// تفاصيل محضر المعاينة والفحص الفني الشامل لملابس الطلب داخل المغسلة.
/// </summary>
public class InspectionDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد للطلب المفحوص.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// رقم الطلب التتبعي للعرض.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للحقيبة التي وردت فيها الملابس إن وجدت.
    /// </summary>
    public Guid? BagId { get; set; }

    /// <summary>
    /// الحالة التشغيلية الحالية لمحضر الفحص (In Progress, Completed, Reopened).
    /// </summary>
    public InspectionStatus Status { get; set; }

    /// <summary>
    /// تاريخ ووقت بدء عملية الفحص الفني.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// تاريخ ووقت اكتمال محضر الفحص إن اكتمل.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// ملاحظات عامة حول الفحص.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// تاريخ ووقت إعادة فتح محضر الفحص إن تمت إعادته.
    /// </summary>
    public DateTime? ReopenedAt { get; set; }

    /// <summary>
    /// سبب إعادة فتح المحضر إن وجد.
    /// </summary>
    public string? ReopenReason { get; set; }

    /// <summary>
    /// بنود الفحص التفصيلية ومطابقة القطع والأضرار.
    /// </summary>
    public List<InspectionItemDto> Items { get; set; } = new();
}
