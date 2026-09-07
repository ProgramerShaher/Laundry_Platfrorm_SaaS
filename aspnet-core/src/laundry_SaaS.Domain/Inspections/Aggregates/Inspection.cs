using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    /// يضيف سطر فحص ومطابقة إلى محضر الفحص مع التحقق الصارم من عدم تكرار تمثيل نفس بند الطلب الأصلي.
    /// </summary>
    /// <param name="item">كيان سطر الفحص المراد إضافته.</param>
    /// <exception cref="BusinessException">يتم رميها إذا تم تكرار إضافة نفس بند الطلب الأصلي.</exception>
    public void AddItem(InspectionItem item)
    {
        Check.NotNull(item, nameof(item));

        if (item.OrderItemId.HasValue && Items.Any(i => i.OrderItemId.HasValue && i.OrderItemId.Value == item.OrderItemId.Value))
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.InspectionErrorCodes.DuplicateOrderItemRepresentation,
                $"OrderItem with ID '{item.OrderItemId.Value}' is already represented in this inspection.");
        }

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
    /// يعتمد ويكمل محضر الفحص الفني ويوثق توقيت الانتهاء، مع التحقق الصارم من أن جميع بنود الطلب الأصلية المتوقعة قد تم تمثيلها في الفحص.
    /// </summary>
    /// <param name="completedAt">تاريخ ووقت الاكتمال.</param>
    /// <param name="expectedOrderItemIds">قائمة معرّفات بنود الطلب الأصلية التي يجب أن تكون مشمولة في الفحص.</param>
    /// <param name="notes">ملاحظات ختامية اختيارية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا وُجد بند طلب أصلي لم يُفحص أو يُمثل في المحضر.</exception>
    public void Complete(DateTime completedAt, IReadOnlyCollection<Guid> expectedOrderItemIds, string? notes = null)
    {
        Check.NotNull(expectedOrderItemIds, nameof(expectedOrderItemIds));

        var inspectedOrderItemIds = Items
            .Where(i => i.OrderItemId.HasValue)
            .Select(i => i.OrderItemId!.Value)
            .ToHashSet();

        var missingOrderItemIds = expectedOrderItemIds.Where(id => !inspectedOrderItemIds.Contains(id)).ToList();
        if (missingOrderItemIds.Count > 0)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.InspectionErrorCodes.IncompleteOrderItemsInspected,
                $"Inspection cannot be completed because {missingOrderItemIds.Count} original order item(s) are not represented in inspection results.");
        }

        Status = InspectionStatus.Completed;
        CompletedAt = completedAt;
        if (notes != null)
        {
            Notes = notes;
        }
    }

    /// <summary>
    /// يعتمد ويكمل محضر الفحص الفني باستخدام كيان الطلب المرتبط مباشرة للتحقق من تمثيل جميع بنوده.
    /// </summary>
    /// <param name="completedAt">تاريخ ووقت الاكتمال.</param>
    /// <param name="order">كيان الطلب المرتبط بالمحضر.</param>
    /// <param name="notes">ملاحظات ختامية اختيارية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يطابق الطلب محضر الفحص أو كانت هناك بنود ناقصة.</exception>
    public void Complete(DateTime completedAt, Orders.Order order, string? notes = null)
    {
        Check.NotNull(order, nameof(order));

        if (order.Id != OrderId)
        {
            throw new BusinessException($"Order mismatch: inspection belongs to order '{OrderId}', but passed order was '{order.Id}'.");
        }

        var expectedIds = order.Items.Select(x => x.Id).ToList();
        Complete(completedAt, expectedIds, notes);
    }

    /// <summary>
    /// يعتمد ويكمل محضر الفحص الفني دون تمرير قائمة بنود الطلب (تستخدم عند عدم توفر كيان الطلب في سياق الاستدعاء).
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
