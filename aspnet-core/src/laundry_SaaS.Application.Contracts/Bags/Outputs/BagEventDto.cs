using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Bags;

/// <summary>
/// يمثل حدثاً تشغيلياً في سلسلة حيازة وحركة حقيبة الغسيل.
/// </summary>
public class BagEventDto : CreationAuditedEntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد للحقيبة.
    /// </summary>
    public Guid BagId { get; set; }

    /// <summary>
    /// نوع الحدث التشغيلي المسجل.
    /// </summary>
    public BagEventType EventType { get; set; }

    /// <summary>
    /// اسم أو وصف نوع الحدث بالعربية للعرض.
    /// </summary>
    public string EventTypeName { get; set; } = null!;

    /// <summary>
    /// ملاحظات إضافية حول الحدث.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// خط عرض موقع المسح أو التسجيل.
    /// </summary>
    public double? LocationLatitude { get; set; }

    /// <summary>
    /// خط طول موقع المسح أو التسجيل.
    /// </summary>
    public double? LocationLongitude { get; set; }

    /// <summary>
    /// معرف المستخدم المنفذ للحدث.
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// اسم المستخدم المنفذ للحدث للعرض.
    /// </summary>
    public string? ActorUserName { get; set; }
}
