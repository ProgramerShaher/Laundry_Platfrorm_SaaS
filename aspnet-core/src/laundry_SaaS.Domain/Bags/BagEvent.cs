using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Bags;

/// <summary>
/// كيان تابع (Child Entity) يمثل حدثاً تشغيلياً تراكمياً (Append-Only) في سلسلة حيازة وحركة حقيبة الغسيل.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Bag"/>. يقتصر هذا السجل على توثيق حركة الحقيبة الفعلية (Created, PickedUp, Received, Processing, Ready, Delivered)
/// ولا يُستخدم لتوثيق مراحل الغسيل والكي التفصيلية التي تخص كيان الطلب (<see cref="Orders.Order"/>).
/// السجل غير قابل للتعديل (Immutable) ولا يدعم الحذف.
/// </para>
/// </summary>
public class BagEvent : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// معرّف الحقيبة المالكة التي وقع عليها هذا الحدث التشغيلي.
    /// </summary>
    public Guid BagId { get; private set; }

    /// <summary>
    /// نوع الحدث التشغيلي المسجل في سلسلة الحيازة.
    /// </summary>
    public BagEventType EventType { get; private set; }

    /// <summary>
    /// ملاحظات إضافية حول الحدث أو ظروف الاستلام/التسليم.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// خط العرض الجغرافي للموقع الذي تم فيه مسح رمز الحقيبة أو تسجيل الحدث.
    /// </summary>
    public double? LocationLatitude { get; private set; }

    /// <summary>
    /// خط الطول الجغرافي للموقع الذي تم فيه مسح رمز الحقيبة أو تسجيل الحدث.
    /// </summary>
    public double? LocationLongitude { get; private set; }

    /// <summary>
    /// معرّف المستخدم (سائق، موظف فرز) الذي قام بتنفيذ وتوثيق هذا الحدث.
    /// </summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private BagEvent()
    {
    }

    /// <summary>
    /// يُنشئ سجلاً تراكمياً لحدث تشغيلي جديد لحقيبة الملابس.
    /// </summary>
    /// <param name="id">المعرّف الفريد للحدث.</param>
    /// <param name="bagId">معرّف الحقيبة.</param>
    /// <param name="eventType">نوع الحدث.</param>
    /// <param name="notes">ملاحظات توضيحية.</param>
    /// <param name="locationLatitude">خط العرض للموقع.</param>
    /// <param name="locationLongitude">خط الطول للموقع.</param>
    /// <param name="actorUserId">معرّف المستخدم المنفذ للحدث.</param>
    public BagEvent(
        Guid id,
        Guid bagId,
        BagEventType eventType,
        string? notes = null,
        double? locationLatitude = null,
        double? locationLongitude = null,
        Guid? actorUserId = null)
        : base(id)
    {
        if (locationLatitude.HasValue)
        {
            Common.GeoLocationValidator.ValidateLatitude(locationLatitude.Value);
        }

        if (locationLongitude.HasValue)
        {
            Common.GeoLocationValidator.ValidateLongitude(locationLongitude.Value);
        }

        BagId = bagId;
        EventType = eventType;
        Notes = notes;
        LocationLatitude = locationLatitude;
        LocationLongitude = locationLongitude;
        ActorUserId = actorUserId;
    }
}
