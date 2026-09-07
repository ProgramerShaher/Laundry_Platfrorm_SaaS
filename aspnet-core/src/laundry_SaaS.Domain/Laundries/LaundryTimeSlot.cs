using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Laundries;

/// <summary>
/// كيان تابع (Child Entity) يمثل نافذة أو فترة زمنية محددة لجدولة عمليات الاستلام أو التوصيل للمغسلة.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Laundry"/>. تُستخدم هذه الفترات لمنح العميل خيارات لاختيار الوقت المناسب
/// لقدوم مندوب الاستلام أو توصيل الملابس بعد الانتهاء منها.
/// </para>
/// </summary>
public class LaundryTimeSlot : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف المغسلة المالكة التي تتبع لها هذه الفترة الزمنية.
    /// </summary>
    public Guid LaundryId { get; private set; }

    /// <summary>
    /// يوم الأسبوع الذي تتكرر فيه هذه الفترة الزمنية المتاحة لجدولة الاستلام أو التوصيل.
    /// </summary>
    public DayOfWeek DayOfWeek { get; private set; }

    /// <summary>
    /// نوع الفترة المجدولة: استلام من العميل (Pickup) أو توصيل إليه (Delivery).
    /// </summary>
    public SlotType SlotType { get; private set; }

    /// <summary>
    /// وقت بداية الفترة الزمنية المتاحة (مثل 09:00 صباحاً).
    /// </summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>
    /// وقت نهاية الفترة الزمنية المتاحة (مثل 12:00 ظهراً)، ويشترط أن يكون لاحقاً لوقت البداية.
    /// </summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>
    /// حالة تفعيل الفترة؛ تتيح للمغسلة إيقاف فترة معينة مؤقتاً دون حذفها.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private LaundryTimeSlot()
    {
    }

    /// <summary>
    /// يُنشئ فترة زمنية مجدولة جديدة تابعة لمغسلة محددة في يوم معين من أيام الأسبوع.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="laundryId">معرّف المغسلة المالكة.</param>
    /// <param name="dayOfWeek">يوم الأسبوع المحدد للفترة.</param>
    /// <param name="slotType">نوع الفترة (استلام أو توصيل).</param>
    /// <param name="startTime">وقت البداية.</param>
    /// <param name="endTime">وقت النهاية.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    public LaundryTimeSlot(
        Guid id,
        Guid laundryId,
        DayOfWeek dayOfWeek,
        SlotType slotType,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isActive = true)
        : base(id)
    {
        LaundryId = laundryId;
        DayOfWeek = dayOfWeek;
        SlotType = slotType;
        SetTime(startTime, endTime);
        IsActive = isActive;
    }

    /// <summary>
    /// يحدد أوقات الفترة الزمنية مع التحقق من أن وقت البداية يسبق وقت النهاية.
    /// </summary>
    /// <param name="startTime">وقت البداية الجديد.</param>
    /// <param name="endTime">وقت النهاية الجديد.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان وقت البداية مساوياً أو لاحقاً لوقت النهاية.</exception>
    public void SetTime(TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            throw new BusinessException("StartTime must be earlier than EndTime.");
        }

        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// يغير حالة تفعيل الفترة الزمنية.
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
