using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Complaints;

/// <summary>
/// كيان تابع (Child Entity) يمثل مرفقاً إثباتياً (صورة، مستند، تقرير) مرتبطاً بملف الشكوى.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Complaint"/>. لا يتم تخزين المحتوى الثنائي (Binary File Content) للمرفق داخل قاعدة البيانات،
/// بل يتم حفظ مرجع التخزين السحابي (<see cref="BlobName"/>) مع اسم الملف ونوع المحتوى (ContentType).
/// هذا السجل تراكمي (Append-Only) ولا يدعم التعديل أو الحذف في دورة العمل.
/// </para>
/// </summary>
public class ComplaintAttachment : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// معرّف الشكوى الرئيسية المالكة لهذا المرفق.
    /// </summary>
    public Guid ComplaintId { get; private set; }

    /// <summary>
    /// اسم مرجع الملف في وحدة التخزين السحابية (Blob Name).
    /// </summary>
    public string BlobName { get; private set; } = null!;

    /// <summary>
    /// اسم الملف الأصلي بصيغته وامتداده (مثل: invoice_damaged.jpg).
    /// </summary>
    public string FileName { get; private set; } = null!;

    /// <summary>
    /// نوع وسائط الإنترنت للملف (MIME Content-Type مثل: image/jpeg, application/pdf).
    /// </summary>
    public string ContentType { get; private set; } = null!;

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private ComplaintAttachment()
    {
    }

    /// <summary>
    /// يُنشئ سجل مرفق جديد لشكوى محددة مع التحقق من صحة المرجع ونوع الملف.
    /// </summary>
    /// <param name="id">المعرّف الفريد للمرفق.</param>
    /// <param name="complaintId">معرّف الشكوى المالك.</param>
    /// <param name="blobName">اسم المرجع السحابي.</param>
    /// <param name="fileName">اسم الملف الأصلي.</param>
    /// <param name="contentType">نوع المحتوى MIME.</param>
    public ComplaintAttachment(
        Guid id,
        Guid complaintId,
        string blobName,
        string fileName,
        string contentType)
        : base(id)
    {
        ComplaintId = complaintId;
        BlobName = Check.NotNullOrWhiteSpace(blobName, nameof(blobName), maxLength: 500);
        FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName), maxLength: 255);
        ContentType = Check.NotNullOrWhiteSpace(contentType, nameof(contentType), maxLength: 100);
    }
}
