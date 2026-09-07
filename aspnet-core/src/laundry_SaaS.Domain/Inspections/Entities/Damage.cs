using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Inspections;

/// <summary>
/// كيان تابع (Child Entity) يمثل ضرراً أو عيباً مسبقاً (Pre-existing Damage) تم اكتشافه على قطعة الملابس أثناء الفحص.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Inspection"/>. يفيد هذا السجل في إثبات وجود الضرر (تمزق، بقعة قديمة، حرق، زر مفقود)
/// قبل إدخال القطعة في خطوط الغسيل والمعالجة، وتوثيقه بصورة فوتوغرافية عبر مرجع تخزين سحابي (<see cref="PhotoBlobName"/>)
/// دون حفظ ملفات الصور الثنائية في قاعدة البيانات.
/// </para>
/// </summary>
public class Damage : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف محضر الفحص الرئيسي المالك لهذا السجل.
    /// </summary>
    public Guid InspectionId { get; private set; }

    /// <summary>
    /// معرّف بند الفحص الخاص بالقطعة المتضررة.
    /// </summary>
    public Guid InspectionItemId { get; private set; }

    /// <summary>
    /// تصنيف نوع الضرر (تمزق، بقعة، حرق، سحاب مكسور، إلخ).
    /// </summary>
    public DamageType DamageType { get; private set; }

    /// <summary>
    /// وصف دقيق لموضع الضرر وحجمه على القطعة.
    /// </summary>
    public string Description { get; private set; } = null!;

    /// <summary>
    /// اسم مرجع الصورة الفوتوغرافية للضرر في وحدة التخزين السحابية (Blob Name).
    /// </summary>
    public string PhotoBlobName { get; private set; } = null!;

    /// <summary>
    /// مستوى جسامة الضرر وتأثيره (طفيف، متوسط، جسيم).
    /// </summary>
    public DamageSeverity Severity { get; private set; }

    /// <summary>
    /// تاريخ ووقت رصد وتوثيق هذا الضرر من قبل الفني.
    /// </summary>
    public DateTime ObservedAt { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Damage()
    {
    }

    /// <summary>
    /// يُنشئ سجل توثيق ضرر مسبق جديد لقطعة مفحوصة مع إرفاق مرجع الصورة الفوتوغرافية.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="inspectionId">معرّف محضر الفحص المالك.</param>
    /// <param name="inspectionItemId">معرّف بند الفحص.</param>
    /// <param name="damageType">نوع الضرر.</param>
    /// <param name="description">الوصف النصي للضرر وموقعه.</param>
    /// <param name="photoBlobName">اسم المرجع السحابي لصورة الضرر.</param>
    /// <param name="severity">مستوى الجسامة.</param>
    /// <param name="observedAt">تاريخ ووقت المعاينة.</param>
    public Damage(
        Guid id,
        Guid inspectionId,
        Guid inspectionItemId,
        DamageType damageType,
        string description,
        string photoBlobName,
        DamageSeverity severity,
        DateTime observedAt)
        : base(id)
    {
        InspectionId = inspectionId;
        InspectionItemId = inspectionItemId;
        DamageType = damageType;
        Description = Check.NotNullOrWhiteSpace(description, nameof(description), maxLength: 500);
        PhotoBlobName = Check.NotNullOrWhiteSpace(photoBlobName, nameof(photoBlobName), maxLength: 500);
        Severity = severity;
        ObservedAt = observedAt;
    }
}
