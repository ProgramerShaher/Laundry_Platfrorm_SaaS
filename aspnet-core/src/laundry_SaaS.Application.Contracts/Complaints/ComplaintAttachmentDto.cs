using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Complaints;

/// <summary>
/// يمثل بيانات المرفق الإثباتي المرتبط بالشكوى (Complaint Attachment DTO).
/// يحتوي على البيانات الوصفية ومرجع التخزين السحابي المعتمد.
/// </summary>
public class ComplaintAttachmentDto : EntityDto<Guid>
{
    /// <summary>
    /// المعرّف الفريد للشكوى المالكة لهذا المرفق.
    /// </summary>
    public Guid ComplaintId { get; set; }

    /// <summary>
    /// اسم مرجع الملف في وحدة التخزين السحابية (Blob Name).
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// اسم الملف الأصلي بصيغته وامتداده.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// نوع وسائط الإنترنت للملف (MIME Content-Type).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// تاريخ ووقت إضافة المرفق.
    /// </summary>
    public DateTime CreationTime { get; set; }
}
