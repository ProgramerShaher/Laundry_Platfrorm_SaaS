using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace laundry_SaaS.Common;

/// <summary>
/// فئة أساسية للجذور التجميعية (Aggregate Roots) التابعة لمستأجر محدد (Tenant-Owned)،
/// والتي تتطلب توثيق عمليات الإنشاء والتعديل (Audited)، مع استبعاد الحذف الناعم (No Soft Delete).
/// <para>
/// ترث الفئة من <see cref="AuditedAggregateRoot{TKey}"/> من إطار عمل ABP وتطبق واجهة تعدد المستأجرين <see cref="IMultiTenant"/>.
/// تم تخصيص هذه الفئة للكيانات التشغيلية والمالية الحساسة (مثل الطلبات، المهام، الحقائب، الشكاوى)
/// التي يُمنع حذفها لحفظ النزاهة المحاسبية والتشغيلية وسلسلة الحيازة.
/// </para>
/// </summary>
public abstract class TenantAuditedAggregateRoot : AuditedAggregateRoot<Guid>, IMultiTenant
{
    /// <summary>
    /// معرّف المستأجر (Tenant) الذي يملك هذا السجل.
    /// <para>
    /// تم تحديد نوع الخاصية كـ <see cref="Nullable{Guid}"/> تماشياً مع معيار <see cref="IMultiTenant"/> في ABP،
    /// ولكن على مستوى قواعد النطاق (Domain Invariants)، يُمنع منعاً باتاً أن تكون هذه القيمة فارغة (null أو Empty)
    /// في الكيانات التابعة للمغسلة. لا يوجد مُعدِّل عام (No public setter) لمنع التغيير العشوائي بعد الإنشاء.
    /// </para>
    /// </summary>
    public Guid? TenantId { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core لأغراض الـ Materialization.
    /// لا يُستخدم مباشرة في منطق الأعمال (Domain Logic).
    /// </summary>
    protected TenantAuditedAggregateRoot()
    {
    }

    /// <summary>
    /// يُنشئ جذراً تجميعياً جديداً خاضعاً لتدقيق التعديلات وتابعاً لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان (Primary Key).</param>
    /// <param name="tenantId">معرّف المستأجر المالك، ويُشترط ألا يكون فارغاً (Guid.Empty).</param>
    /// <exception cref="BusinessException">يتم رميها إذا تم تمرير معرّف مستأجر فارغ.</exception>
    protected TenantAuditedAggregateRoot(Guid id, Guid tenantId)
        : base(id)
    {
        if (tenantId == Guid.Empty)
        {
            throw new BusinessException("TenantId must not be empty for tenant-owned entities.");
        }

        TenantId = tenantId;
    }
}
