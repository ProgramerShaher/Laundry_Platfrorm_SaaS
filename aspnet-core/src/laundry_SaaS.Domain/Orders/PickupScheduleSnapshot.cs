using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace laundry_SaaS.Orders;

/// <summary>
/// كائن قيمة (Value Object) يمثل لقطة تاريخية ثابتة وغير قابلة للتغيير لموعد استلام ملابس الطلب من العميل.
/// <para>
/// يُعد هذا الكائن المصدر الحصري والموثوق (Source of Truth) لتفاصيل موعد الاستلام المعتمد للطلب؛
/// وتبقى قيمه محفوظة بالكامل حتى لو قامت إدارة المغسلة بتعديل أو تعطيل أو حذف قالب الفترة الزمنية الأصلية (<see cref="OriginalSlotId"/>)
/// أو تغيير ساعات العمل الرسمية لاحقاً. لا يحتفظ الكيان بعلاقة ملاحة برمجية (No Navigation Property) لقالب الفترة حمايةً لحدود النطاق.
/// </para>
/// </summary>
public class PickupScheduleSnapshot : ValueObject
{
    /// <summary>
    /// التاريخ التقويمي المجدول لاستلام الملابس من العميل.
    /// </summary>
    public DateOnly ScheduledDate { get; private set; }

    /// <summary>
    /// وقت بداية الفترة الزمنية المعتمدة للاستلام.
    /// </summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>
    /// وقت نهاية الفترة الزمنية المعتمدة للاستلام.
    /// </summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>
    /// المعرّف المرجعي لقالب الفترة الزمنية الأصلي في المغسلة وقت إنشاء الطلب (للمرجعية التتبعية فقط).
    /// </summary>
    public Guid OriginalSlotId { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private PickupScheduleSnapshot()
    {
    }

    /// <summary>
    /// يُنشئ لقطة تاريخية جديدة لجدولة موعد الاستلام مع التحقق الصارم من صحة البيانات.
    /// </summary>
    /// <param name="scheduledDate">تاريخ الاستلام المجدول.</param>
    /// <param name="startTime">وقت بداية فترة الاستلام.</param>
    /// <param name="endTime">وقت نهاية فترة الاستلام.</param>
    /// <param name="originalSlotId">المعرّف المرجعي للفترة الزمنية في المغسلة.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف الفترة فارغاً أو كان وقت البداية لاحقاً أو مساوياً لوقت النهاية.</exception>
    public PickupScheduleSnapshot(
        DateOnly scheduledDate,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid originalSlotId)
    {
        if (originalSlotId == Guid.Empty)
        {
            throw new BusinessException("OriginalSlotId must not be empty.");
        }

        if (startTime >= endTime)
        {
            throw new BusinessException("StartTime must be strictly earlier than EndTime for pickup schedule snapshot.");
        }

        ScheduledDate = scheduledDate;
        StartTime = startTime;
        EndTime = endTime;
        OriginalSlotId = originalSlotId;
    }

    /// <summary>
    /// يُرجع القيم الذرية المحددة لمساواة كائن القيمة.
    /// </summary>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ScheduledDate;
        yield return StartTime;
        yield return EndTime;
        yield return OriginalSlotId;
    }
}
