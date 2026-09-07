using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace laundry_SaaS.Common;

/// <summary>
/// فئة أساسية للجذور التجميعية (Aggregate Roots) التابعة لمستأجر محدد (Tenant-Owned)،
/// والتي تتطلب تدقيقاً شاملاً لكافة العمليات بما فيها الإنشاء والتعديل والحذف الناعم (Full Audited with Soft Delete).
/// <para>
/// ترث الفئة من <see cref="FullAuditedAggregateRoot{TKey}"/> من إطار عمل ABP وتطبق واجهة تعدد المستأجرين <see cref="IMultiTenant"/>.
/// خُصصت هذه الفئة للبيانات التعريفية والإعدادية والأرشيفية الخاصة بالمستأجر (مثل بيانات المغسلة، الكتالوج والخدمات، الأسعار، طاقم العمل، والسائقين)،
/// حيث يُسمح فيها بالحذف الناعم وإخفاء السجلات دون فقدان أثرها في قاعدة البيانات.
/// </para>
/// </summary>
public abstract class TenantFullAuditedAggregateRoot : FullAuditedAggregateRoot<Guid>, IMultiTenant
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
    protected TenantFullAuditedAggregateRoot()
    {
    }

    /// <summary>
    /// يُنشئ جذراً تجميعياً جديداً خاضعاً للتدقيق الشامل والحذف الناعم وتابعاً لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان (Primary Key).</param>
    /// <param name="tenantId">معرّف المستأجر المالك، ويُشترط ألا يكون فارغاً (Guid.Empty).</param>
    /// <exception cref="BusinessException">يتم رميها إذا تم تمرير معرّف مستأجر فارغ.</exception>
    protected TenantFullAuditedAggregateRoot(Guid id, Guid tenantId)
        : base(id)
    {
        if (tenantId == Guid.Empty)
        {
            throw new BusinessException("TenantId must not be empty for tenant-owned entities.");
        }

        TenantId = tenantId;
    }
}
