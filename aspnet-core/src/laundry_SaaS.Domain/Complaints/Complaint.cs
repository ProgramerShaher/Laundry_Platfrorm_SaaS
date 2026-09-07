using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Complaints;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لشكوى العميل المتعلقة بطلب أو خدمة معينة.
/// <para>
/// يتبع هذا الكيان مستأجراً محدداً (<see cref="TenantAuditedAggregateRoot.TenantId"/>) ولا يدعم الحذف الناعم (No Soft Delete) لحفظ تاريخ خدمة العملاء والمساءلة.
/// يحتوي الكيان على كائن القيمة المدمج (<see cref="ComplaintResolutionInfo"/>) لتوثيق إجراءات الحل والتعويض المالي،
/// بالإضافة إلى مجموعة المرفقات الإثباتية (<see cref="Attachments"/>).
/// يخضع لفهرس فريد يمنع تكرار رقم الشكوى داخل نفس المغسلة (TenantId, ComplaintNumber).
/// </para>
/// </summary>
public class Complaint : TenantAuditedAggregateRoot
{
    /// <summary>
    /// الرقم التسلسلي المميز والفريد للشكوى (مثل: CMP-2026-001).
    /// </summary>
    public string ComplaintNumber { get; private set; } = null!;

    /// <summary>
    /// معرّف الطلب محل النزاع أو الشكوى.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// معرّف العميل صاحب الشكوى.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// الحالة الحالية للشكوى (مفتوحة، قيد المراجعة، تم الحل، مغلقة).
    /// </summary>
    public ComplaintStatus Status { get; private set; }

    /// <summary>
    /// تصنيف نوع الشكوى (تلف قطعة، قطعة مفقودة، تأخير توصيل، جودة الغسيل، سلوك السائق).
    /// </summary>
    public ComplaintType Type { get; private set; }

    /// <summary>
    /// عنوان أو موضوع الشكوى البارز والموجز.
    /// </summary>
    public string Subject { get; private set; } = null!;

    /// <summary>
    /// الشرح التفصيلي الكامل لوقائع المشكلة من وجهة نظر العميل.
    /// </summary>
    public string Description { get; private set; } = null!;

    /// <summary>
    /// كائن القيمة المدمج (Value Object) الذي يوثق قرارات الحل والتسوية ومبالغ التعويض المعتمدة.
    /// </summary>
    public ComplaintResolutionInfo ResolutionInfo { get; private set; } = null!;

    /// <summary>
    /// تاريخ ووقت إغلاق ملف الشكوى نهائياً.
    /// </summary>
    public DateTime? ClosedAt { get; private set; }

    /// <summary>
    /// قائمة المرفقات والمستندات الإثباتية التابعة للشكوى (Child Entities).
    /// </summary>
    public virtual ICollection<ComplaintAttachment> Attachments { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Complaint()
    {
        Attachments = new Collection<ComplaintAttachment>();
    }

    /// <summary>
    /// يُنشئ شكوى جديدة بحالة مفتوحة (Open) مع تهيئة كائن التسوية والمرفقات.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="complaintNumber">رقم الشكوى المميز.</param>
    /// <param name="orderId">معرّف الطلب.</param>
    /// <param name="customerId">معرّف العميل.</param>
    /// <param name="type">نوع الشكوى.</param>
    /// <param name="subject">موضوع الشكوى.</param>
    /// <param name="description">الوصف التفصيلي.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المعرفات الأساسية فارغة.</exception>
    public Complaint(
        Guid id,
        Guid tenantId,
        string complaintNumber,
        Guid orderId,
        Guid customerId,
        ComplaintType type,
        string subject,
        string description)
        : base(id, tenantId)
    {
        if (orderId == Guid.Empty)
        {
            throw new BusinessException("OrderId must not be empty.");
        }

        if (customerId == Guid.Empty)
        {
            throw new BusinessException("CustomerId must not be empty.");
        }

        ComplaintNumber = Check.NotNullOrWhiteSpace(complaintNumber, nameof(complaintNumber), maxLength: 50);
        OrderId = orderId;
        CustomerId = customerId;
        Type = type;
        Subject = Check.NotNullOrWhiteSpace(subject, nameof(subject), maxLength: 200);
        Description = Check.NotNullOrWhiteSpace(description, nameof(description), maxLength: 2000);
        Status = ComplaintStatus.Open;
        ResolutionInfo = new ComplaintResolutionInfo();
        Attachments = new Collection<ComplaintAttachment>();
    }

    /// <summary>
    /// يضيف مرفقاً أو صورة إثباتية جديدة لملف الشكوى.
    /// </summary>
    /// <param name="attachment">كيان المرفق التابع.</param>
    public void AddAttachment(ComplaintAttachment attachment)
    {
        Check.NotNull(attachment, nameof(attachment));
        Attachments.Add(attachment);
    }

    /// <summary>
    /// ينقل الشكوى إلى حالة قيد المراجعة والتحقيق (InReview).
    /// </summary>
    public void MarkInReview()
    {
        Status = ComplaintStatus.InReview;
    }

    /// <summary>
    /// يحل الشكوى ويوثق ملاحظات الحل وهوية الموظف المسؤول ومبلغ التعويض إن وجد.
    /// </summary>
    /// <param name="resolutionNotes">ملاحظات وقرارات التسوية.</param>
    /// <param name="resolvedByUserId">معرّف المستخدم أو المشرف الذي حل الشكوى.</param>
    /// <param name="resolutionTime">تاريخ ووقت اعتماد الحل.</param>
    /// <param name="compensationAmount">مبلغ التعويض المالي المقترح (إن وجد).</param>
    public void Resolve(string resolutionNotes, Guid resolvedByUserId, DateTime resolutionTime, decimal compensationAmount = 0)
    {
        Status = ComplaintStatus.Resolved;
        ResolutionInfo = new ComplaintResolutionInfo(
            resolutionNotes,
            resolvedByUserId,
            resolutionTime,
            compensationAmount);
    }

    /// <summary>
    /// يغلق ملف الشكوى نهائياً ويوثق تاريخ ووقت الإغلاق.
    /// </summary>
    /// <param name="closedAt">تاريخ ووقت الإغلاق.</param>
    public void Close(DateTime closedAt)
    {
        Status = ComplaintStatus.Closed;
        ClosedAt = closedAt;
    }
}
