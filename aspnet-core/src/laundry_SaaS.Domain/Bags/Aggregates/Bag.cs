using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Bags;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لحقيبة الغسيل الفعلية المستخدمة لنقل وحفظ ملابس العميل.
/// <para>
/// يُعد الكيان المصدر الحصري الموثوق (Source of Truth) لسلسلة الحيازة المادية (Physical Chain of Custody) للقطع،
/// بدءاً من استلامها من العميل وحتى تسليمها إليه نظيفة.
/// يتبع مستأجراً محدداً (<see cref="TenantAuditedAggregateRoot.TenantId"/>) ولا يدعم الحذف الناعم (No Soft Delete).
/// يخضع لفهارس فريدة تمنع تكرار رقم الحقيبة (<see cref="BagNumber"/>) أو رمز الاستجابة السريعة (<see cref="QrCode"/>) داخل نفس المغسلة.
/// </para>
/// </summary>
public class Bag : TenantAuditedAggregateRoot
{
    /// <summary>
    /// معرّف الطلب المرتبط بهذه الحقيبة في دورة النقل والتشغيل الحالية.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// الرقم التسلسلي المطبوع أو المعرّف الفيزيائي للحقيبة (مثل: BAG-1004).
    /// </summary>
    public string BagNumber { get; private set; } = null!;

    /// <summary>
    /// رمز الاستجابة السريعة (QR Code) أو الباركود الملصق على الحقيبة لإجراء المسح السريع بالهاتف أو الماسح الضوئي.
    /// </summary>
    public string QrCode { get; private set; } = null!;

    /// <summary>
    /// يشير إلى ما إذا كانت الحقيبة قيد الاستخدام الفعلي والنشط حالياً.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// سجل الأحداث التاريخية المتتابعة (Append-Only) لحركة الحقيبة وسلسلة حيازتها.
    /// </summary>
    public virtual ICollection<BagEvent> Events { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Bag()
    {
        Events = new Collection<BagEvent>();
    }

    /// <summary>
    /// يُنشئ سجلاً جديداً لحقيبة غسيل مرتبطة بطلب، مع تسجيل الحدث الافتتاحي (Created).
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="orderId">معرّف الطلب المرتبط.</param>
    /// <param name="bagNumber">رقم الحقيبة الفيزيائي.</param>
    /// <param name="qrCode">رمز QR للحقيبة.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف الطلب فارغاً.</exception>
    public Bag(
        Guid id,
        Guid tenantId,
        Guid orderId,
        string bagNumber,
        string qrCode,
        bool isActive = true)
        : base(id, tenantId)
    {
        if (orderId == Guid.Empty)
        {
            throw new BusinessException("OrderId must not be empty.");
        }

        OrderId = orderId;
        BagNumber = Check.NotNullOrWhiteSpace(bagNumber, nameof(bagNumber), maxLength: 50);
        QrCode = Check.NotNullOrWhiteSpace(qrCode, nameof(qrCode), maxLength: 100);
        IsActive = isActive;
        Events = new Collection<BagEvent>();

        RecordEvent(BagEventType.Created, "Bag registered.");
    }

    /// <summary>
    /// يوثق حدثاً تشغيلياً جديداً في سلسلة حيازة الحقيبة مع التحقق الصارم من التتابع المنطقي للأحداث.
    /// <para>
    /// التسلسل الإلزامي:
    /// <c>Created (1) -> PickedUpFromCustomer (2) -> ReceivedAtLaundry (3) -> Processing (4) -> Ready (5) -> PickedUpForDelivery (6) -> Delivered (7)</c>.
    /// يُمنع القفز عبر الأحداث أو الرجوع للخلف لحماية سلسلة الحيازة المادية.
    /// </para>
    /// </summary>
    /// <param name="eventType">نوع الحدث التشغيلي المطلوب تسجيله.</param>
    /// <param name="notes">ملاحظات توضيحية حول الحدث.</param>
    /// <param name="latitude">خط العرض لموقع تسجيل الحدث.</param>
    /// <param name="longitude">خط الطول لموقع تسجيل الحدث.</param>
    /// <param name="actorUserId">معرّف المستخدم (سائق، موظف استقبال) الذي أجرى المسح أو التوثيق.</param>
    /// <exception cref="BusinessException">يتم رميها إذا خالف الحدث التسلسل التشغيلي الطبيعي.</exception>
    public void RecordEvent(
        BagEventType eventType,
        string? notes = null,
        double? latitude = null,
        double? longitude = null,
        Guid? actorUserId = null)
    {
        if (Events.Any())
        {
            var lastEventType = Events.Last().EventType;
            if ((int)eventType != (int)lastEventType + 1)
            {
                throw new BusinessException($"Invalid bag event transition from '{lastEventType}' to '{eventType}'. Bag custody events must follow the strict progressive sequence.");
            }
        }
        else if (eventType != BagEventType.Created)
        {
            throw new BusinessException($"The first bag custody event must be '{BagEventType.Created}'.");
        }

        Events.Add(new BagEvent(
            Guid.NewGuid(),
            Id,
            eventType,
            notes,
            latitude,
            longitude,
            actorUserId));
    }

    /// <summary>
    /// يعدل حالة تفعيل الحقيبة في النظام.
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
