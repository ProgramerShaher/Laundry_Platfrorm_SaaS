using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Notifications;

/// <summary>
/// يمثل عنصراً في قائمة الإشعارات الخاصة بالمستخدم (عميل، موظف، سائق).
/// </summary>
public class AppNotificationListDto : CreationAuditedEntityDto<Guid>
{
    /// <summary>
    /// عنوان الإشعار المختصر والبارز.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// نص رسالة الإشعار التوضيحية.
    /// </summary>
    public string Body { get; set; } = null!;

    /// <summary>
    /// يشير إلى ما إذا كان المستخدم قد قرأ هذا الإشعار.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// تاريخ ووقت قراءة الإشعار إن تمت قراءته.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// نوع الكيان المرتبط (مثل "Order", "Inspection", "Complaint") للتنقل في شاشات التطبيق.
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// المعرف الفريد للكيان المرتبط لفتح صفحة التفاصيل مباشرة.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
}
