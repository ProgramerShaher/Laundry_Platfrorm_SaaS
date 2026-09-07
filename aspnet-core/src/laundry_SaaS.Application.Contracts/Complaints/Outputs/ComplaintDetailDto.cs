using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Complaints;

/// <summary>
/// تفاصيل شكوى العميل شاملة قرارات التسوية والمرفقات.
/// </summary>
public class ComplaintDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// الرقم التسلسلي المميز للشكوى (مثل: CMP-2026-001).
    /// </summary>
    public string ComplaintNumber { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للطلب المرتبط.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// رقم الطلب التتبعي للعرض.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// المعرف الفريد للعميل صاحب الشكوى.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// اسم العميل صاحب الشكوى للعرض.
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
    /// الشرح التفصيلي للشكوى.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// ملاحظات وإجراءات الحل إن تم حل الشكوى.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// تاريخ ووقت اعتماد الحل.
    /// </summary>
    public DateTime? ResolutionTime { get; set; }

    /// <summary>
    /// مبلغ التعويض المالي إن وجد.
    /// </summary>
    public decimal CompensationAmount { get; set; }

    /// <summary>
    /// تاريخ ووقت تقديم الشكوى.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// أسماء ملفات المرفقات الإثباتية في التخزين السحابي.
    /// </summary>
    public List<string> AttachmentBlobNames { get; set; } = new();

    /// <summary>
    /// قائمة المرفقات الإثباتية التفصيلية للشكوى.
    /// </summary>
    public List<ComplaintAttachmentDto> Attachments { get; set; } = new();
}
