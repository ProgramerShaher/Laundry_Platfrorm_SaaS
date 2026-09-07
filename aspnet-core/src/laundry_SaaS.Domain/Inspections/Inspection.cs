using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Inspections;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لمحضر المعاينة والفحص الفني الرسمي لملابس الطلب عند وصولها للمغسلة.
/// <para>
/// يعتمد النظام محضر فحص رسمي واحد لكل طلب (One Inspection Record per Order) ويخضع لفهرس فريد مركب (TenantId, OrderId).
/// يتبع مستأجراً محدداً (<see cref="TenantAuditedAggregateRoot.TenantId"/>) ولا يدعم الحذف الناعم (No Soft Delete).
/// بدلاً من إنشاء سجلات فحص مكررة، يدعم النظام إعادة فتح نفس المحضر رسمياً (<see cref="Reopen"/>) مع توثيق السبب وتاريخ الإعادة.
/// </para>
/// </summary>
public class Inspection : TenantAuditedAggregateRoot
{
    /// <summary>
    /// معرّف الطلب الذي يتم فحص ملابسه ومطابقتها.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// معرّف الحقيبة الفيزيائية التي وردت فيها الملابس المفحوصة إن وجدت.
    /// </summary>
    public Guid? BagId { get; private set; }

    /// <summary>
    /// الحالة التشغيلية لمحضر الفحص (بدأ الفحص، مكتمل، تمت إعادة فتحه).
    /// </summary>
    public InspectionStatus Status { get; private set; }

    /// <summary>
    /// تاريخ ووقت بدء عملية الفحص والمعاينة الفنية.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت اكتمال واعتماد محضر الفحص.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// ملاحظات الفني العامة المدونة أثناء عملية الفحص.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// تاريخ ووقت إعادة فتح محضر الفحص بعد اكتماله بقرار إداري.
    /// </summary>
    public DateTime? ReopenedAt { get; private set; }

    /// <summary>
    /// السبب الإداري أو التشغيلي الموثق لإعادة فتح محضر الفحص.
    /// </summary>
    public string? ReopenReason { get; private set; }

    /// <summary>
    /// قائمة بنود الفحص لمطابقة كميات القطع المطلوبة مع المستلمة فعلياً (Child Entities).
    /// </summary>
    public virtual ICollection<InspectionItem> Items { get; protected set; }

    /// <summary>
    /// قائمة الأضرار والعيوب المسبقة المرصودة على الملابس والموثقة بالصور (Child Entities).
    /// </summary>
    public virtual ICollection<Damage> Damages { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Inspection()
    {
        Items = new Collection<InspectionItem>();
        Damages = new Collection<Damage>();
    }

    /// <summary>
    /// يُنشئ محضر فحص فني جديد لطلب محدد ويبدأ حالته في (Started).
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="orderId">معرّف الطلب.</param>
    /// <param name="bagId">معرّف الحقيبة الاختياري.</param>
    /// <param name="notes">ملاحظات أولية اختيارية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف الطلب فارغاً.</exception>
    public Inspection(
        Guid id,
        Guid tenantId,
        Guid orderId,
        Guid? bagId = null,
        string? notes = null)
        : base(id, tenantId)
    {
        if (orderId == Guid.Empty)
        {
            throw new BusinessException("OrderId must not be empty.");
        }

        OrderId = orderId;
        BagId = bagId;
        Notes = notes;
        StartedAt = DateTime.UtcNow;
        Status = InspectionStatus.Started;

        Items = new Collection<InspectionItem>();
        Damages = new Collection<Damage>();
    }

    /// <summary>
    /// يضيف بند فحص ومطابقة كميات إلى محضر الفحص.
    /// </summary>
    /// <param name="item">كيان بند الفحص.</param>
    public void AddItem(InspectionItem item)
    {
        Check.NotNull(item, nameof(item));
        Items.Add(item);
    }

    /// <summary>
    /// يضيف توثيق ضرر أو عيب مسبق إلى محضر الفحص.
    /// </summary>
    /// <param name="damage">كيان الضرر المرصود.</param>
    public void AddDamage(Damage damage)
    {
        Check.NotNull(damage, nameof(damage));
        Damages.Add(damage);
    }

    /// <summary>
    /// يعتمد ويكمل محضر الفحص ويوثق توقيت الانتهاء.
    /// </summary>
    /// <param name="completedAt">تاريخ ووقت الاكتمال.</param>
    /// <param name="notes">ملاحظات ختامية اختيارية.</param>
    public void Complete(DateTime completedAt, string? notes = null)
    {
        Status = InspectionStatus.Completed;
        CompletedAt = completedAt;
        if (notes != null)
        {
            Notes = notes;
        }
    }

    /// <summary>
    /// يعيد فتح محضر فحص مكتمل مسبقاً مع اشتراط توثيق السبب الإداري وتاريخ الإعادة.
    /// </summary>
    /// <param name="reason">سبب إعادة فتح المحضر.</param>
    /// <param name="reopenedAt">تاريخ ووقت الإعادة.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يكن المحضر في حالة مكتمل (Completed).</exception>
    public void Reopen(string reason, DateTime reopenedAt)
    {
        if (Status != InspectionStatus.Completed)
        {
            throw new BusinessException("Only completed inspections can be reopened.");
        }

        Status = InspectionStatus.Reopened;
        ReopenReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
        ReopenedAt = reopenedAt;
    }
}
