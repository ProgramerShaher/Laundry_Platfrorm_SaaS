using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Laundries;

/// <summary>
/// كيان تابع (Child Entity) يمثل ساعات العمل والدوام الرسمي للمغسلة في يوم محدد من أيام الأسبوع.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Laundry"/>. لا يمتلك هذا الكيان مستودعاً خاصاً (No Independent Repository)،
/// وتتم إدارته بالكامل كجزء من دورة حياة المغسلة.
/// يشترط النظام فترة عمل واحدة لكل يوم في الـ MVP مع فهرس فريد يمنع تكرار نفس اليوم للمغسلة الواحدة.
/// </para>
/// </summary>
public class LaundryWorkingHour : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف المغسلة المالكة (Aggregate Root Owner) التي يتبع لها جدول ساعات العمل هذا.
    /// </summary>
    public Guid LaundryId { get; private set; }

    /// <summary>
    /// يوم الأسبوع المعني بهذه الساعات (من الأحد إلى السبت).
    /// </summary>
    public DayOfWeek DayOfWeek { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كانت المغسلة تفتح أبوابها للعمل وتستقبل الملابس في هذا اليوم (true) أم مغلقة (false).
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// وقت بدء العمل اليومي. 
    /// يشترط أن يكون غير فارغ في حال كان <see cref="IsOpen"/> يساوي true، ويجب أن يكون فارغاً (null) إذا كانت المغسلة مغلقة.
    /// </summary>
    public TimeOnly? OpenTime { get; private set; }

    /// <summary>
    /// وقت انتهاء العمل اليومي وإغلاق المغسلة.
    /// يشترط أن يكون لاحقاً لـ <see cref="OpenTime"/> إذا كانت المغسلة مفتوحة، وفارغاً (null) إذا كانت مغلقة.
    /// </summary>
    public TimeOnly? CloseTime { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private LaundryWorkingHour()
    {
    }

    /// <summary>
    /// يُنشئ جدول ساعات عمل ليوم محدد في المغسلة مع التحقق من صحة المواعيد وتوافقها.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="laundryId">معرّف المغسلة المالكة.</param>
    /// <param name="dayOfWeek">يوم الأسبوع.</param>
    /// <param name="isOpen">حالة الفتح/الإغلاق.</param>
    /// <param name="openTime">وقت بدء العمل.</param>
    /// <param name="closeTime">وقت انتهاء العمل.</param>
    public LaundryWorkingHour(
        Guid id,
        Guid laundryId,
        DayOfWeek dayOfWeek,
        bool isOpen,
        TimeOnly? openTime = null,
        TimeOnly? closeTime = null)
        : base(id)
    {
        LaundryId = laundryId;
        DayOfWeek = dayOfWeek;
        SetSchedule(isOpen, openTime, closeTime);
    }

    /// <summary>
    /// يحدّث حالة العمل والمواعيد مع فرض القواعد الصارمة:
    /// إذا كانت المغسلة مغلقة يتم تصفير الأوقات تلقائياً، وإذا كانت مفتوحة يجب التحقق من وجود الوقتين وأن وقت البدء يسبق وقت الإغلاق.
    /// </summary>
    /// <param name="isOpen">حالة الفتح/الإغلاق.</param>
    /// <param name="openTime">وقت البدء.</param>
    /// <param name="closeTime">وقت الإغلاق.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المغسلة مفتوحة مع أوقات فارغة أو إذا كان وقت البدء مساوياً أو لاحقاً لوقت الإغلاق.</exception>
    public void SetSchedule(bool isOpen, TimeOnly? openTime, TimeOnly? closeTime)
    {
        IsOpen = isOpen;
        if (!isOpen)
        {
            OpenTime = null;
            CloseTime = null;
            return;
        }

        if (openTime == null || closeTime == null)
        {
            throw new BusinessException("Open and close times are required when working hours are set to open.");
        }

        if (openTime >= closeTime)
        {
            throw new BusinessException("OpenTime must be earlier than CloseTime.");
        }

        OpenTime = openTime;
        CloseTime = closeTime;
    }
}
