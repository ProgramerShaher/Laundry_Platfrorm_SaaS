using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace laundry_SaaS.Notifications;

/// <summary>
/// يمثل الجذر التجميعي الخفيف (Lightweight Aggregate Root) لإشعارات التطبيق المرسلة للمستخدمين (عملاء، موظفين، سائقين).
/// <para>
/// يرث الكيان من <see cref="CreationAuditedAggregateRoot{TKey}"/> ويطبق واجهة <see cref="IMultiTenant"/>.
/// يتميز بنطاق هجين (Hybrid Scope)؛ حيث يُسمح بأن تكون قيمة <see cref="TenantId"/> فارغة (<c>null</c>) للإشعارات العامة الصادرة من إدارة المنصة (Host Level)،
/// أو تحمل معرّف مستأجر محدد للإشعارات المرتبطة بطلبات ومغاسل معينة.
/// لا يدعم الكيان الحذف الناعم (No Soft Delete) ولا الحذف الفيزيائي ضمن دورة عمل الـ MVP لحفظ سجل إشعارات المستخدم.
/// </para>
/// </summary>
public class AppNotification : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    /// <summary>
    /// معرّف المستأجر المرتبط بالإشعار إن وجد، أو <c>null</c> إذا كان الإشعار عاماً على مستوى المنصة (Host).
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// معرّف المستخدم المستهدف لاستلام هذا الإشعار (Identity User Id).
    /// </summary>
    public Guid TargetUserId { get; private set; }

    /// <summary>
    /// عنوان الإشعار المختصر والبارز (مثل: تم استلام طلبك، تحديث حالة الفحص).
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// النص الكامل والتفصيلي لرسالة الإشعار.
    /// </summary>
    public string Body { get; private set; } = null!;

    /// <summary>
    /// يشير إلى ما إذا كان المستخدم قد قام بفتح وقراءة هذا الإشعار.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// تاريخ ووقت قراءة المستخدم للإشعار.
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>
    /// نوع الكيان المرتبط بالإشعار لأغراض التوجيه والتنقل المباشر في التطبيق (مثل: "Order", "Inspection", "Complaint").
    /// </summary>
    public string? RelatedEntityType { get; private set; }

    /// <summary>
    /// المعرّف الفريد للكيان المرتبط (مثل معرّف الطلب OrderId) لفتح شاشة التفاصيل عند النقر على الإشعار.
    /// </summary>
    public Guid? RelatedEntityId { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private AppNotification()
    {
    }

    /// <summary>
    /// يُنشئ إشعاراً جديداً موجهاً لمستخدم محدد بنطاق هجين (Host أو Tenant).
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="targetUserId">معرّف المستخدم المستهدف.</param>
    /// <param name="title">عنوان الإشعار.</param>
    /// <param name="body">نص الإشعار.</param>
    /// <param name="tenantId">معرّف المستأجر (اختياري).</param>
    /// <param name="relatedEntityType">نوع الكيان المرتبط (اختياري).</param>
    /// <param name="relatedEntityId">معرّف الكيان المرتبط (اختياري).</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف المستخدم المستهدف فارغاً.</exception>
    public AppNotification(
        Guid id,
        Guid targetUserId,
        string title,
        string body,
        Guid? tenantId = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null)
        : base(id)
    {
        if (targetUserId == Guid.Empty)
        {
            throw new BusinessException("TargetUserId must not be empty.");
        }

        TargetUserId = targetUserId;
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        Body = Check.NotNullOrWhiteSpace(body, nameof(body), maxLength: 1000);
        TenantId = tenantId;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        IsRead = false;
    }

    /// <summary>
    /// يحدد الإشعار كمقروء ويوثق توقيت القراءة الفعلي.
    /// </summary>
    /// <param name="readAt">تاريخ ووقت القراءة.</param>
    public void MarkAsRead(DateTime readAt)
    {
        IsRead = true;
        ReadAt = readAt;
    }
}
