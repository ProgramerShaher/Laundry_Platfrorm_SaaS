using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Orders;

/// <summary>
/// كيان تابع (Child Entity) يمثل بنداً من بنود الملابس والخدمات المطلوبة داخل الطلب.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Order"/>. يحفظ هذا الكيان لقطات تاريخية ثابتة (Snapshots)
/// لاسم الصنف واسم الخدمة وسعر الوحدة وقت اعتماد الطلب لمنع أي تغير في أسعار أو أسماء البنود بأثر رجعي.
/// المجموع الإجمالي للبند (<see cref="LineTotal"/>) يتم حسابه داخلياً وفق المعادلة (Quantity * UnitPriceSnapshot).
/// </para>
/// </summary>
public class OrderItem : AuditedEntity<Guid>
{
    /// <summary>
    /// معرّف الطلب الرئيسي المالك لهذا البند.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// معرّف نوع قطعة الملابس (مرجع لصنف الكتالوج LaundryItemType).
    /// </summary>
    public Guid LaundryItemTypeId { get; private set; }

    /// <summary>
    /// معرّف نوع الخدمة المطلوبة على هذه القطعة (مرجع لخدمة الكتالوج LaundryService).
    /// </summary>
    public Guid LaundryServiceId { get; private set; }

    /// <summary>
    /// لقطة تاريخية ثابتة لاسم نوع القطعة (Item Name Snapshot) كما كان معروضاً وقت تقديم الطلب.
    /// </summary>
    public string ItemNameSnapshot { get; private set; } = null!;

    /// <summary>
    /// لقطة تاريخية ثابتة لاسم الخدمة (Service Name Snapshot) كما كان معروضاً وقت تقديم الطلب.
    /// </summary>
    public string ServiceNameSnapshot { get; private set; } = null!;

    /// <summary>
    /// عدد القطع المطلوبة من هذا الصنف والخدمة (يشترط أن تكون أكبر من صفر).
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// لقطة تاريخية ثابتة لسعر القطعة الواحدة (Unit Price Snapshot) المعتمد وقت إنشاء الطلب، غير قابلة للتغيير لاحقاً.
    /// </summary>
    public decimal UnitPriceSnapshot { get; private set; }

    /// <summary>
    /// إجمالي قيمة السطر المالي لهذا البند: (Quantity * UnitPriceSnapshot).
    /// يتم حسابه حصرياً في النطاق لمنع أي تلاعب بالقيمة من واجهة المستخدم.
    /// </summary>
    public decimal LineTotal { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private OrderItem()
    {
    }

    /// <summary>
    /// يُنشئ بند طلب جديد مع تثبيت لقطات الأسماء والأسعار وحساب الإجمالي المالي للسطر.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="orderId">معرّف الطلب المالك.</param>
    /// <param name="laundryItemTypeId">معرّف نوع القطعة.</param>
    /// <param name="laundryServiceId">معرّف نوع الخدمة.</param>
    /// <param name="itemNameSnapshot">لقطة اسم القطعة.</param>
    /// <param name="serviceNameSnapshot">لقطة اسم الخدمة.</param>
    /// <param name="quantity">الكمية (أكبر من صفر).</param>
    /// <param name="unitPriceSnapshot">لقطة سعر الوحدة (لا تقل عن صفر).</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت الكمية صفر أو سالبة، أو السعر سالباً.</exception>
    public OrderItem(
        Guid id,
        Guid orderId,
        Guid laundryItemTypeId,
        Guid laundryServiceId,
        string itemNameSnapshot,
        string serviceNameSnapshot,
        int quantity,
        decimal unitPriceSnapshot)
        : base(id)
    {
        if (quantity <= 0)
        {
            throw new BusinessException("Quantity must be greater than zero.");
        }

        if (unitPriceSnapshot < 0)
        {
            throw new BusinessException("UnitPriceSnapshot must not be negative.");
        }

        OrderId = orderId;
        LaundryItemTypeId = laundryItemTypeId;
        LaundryServiceId = laundryServiceId;
        ItemNameSnapshot = Check.NotNullOrWhiteSpace(itemNameSnapshot, nameof(itemNameSnapshot), maxLength: 100);
        ServiceNameSnapshot = Check.NotNullOrWhiteSpace(serviceNameSnapshot, nameof(serviceNameSnapshot), maxLength: 100);
        Quantity = quantity;
        UnitPriceSnapshot = unitPriceSnapshot;
        LineTotal = quantity * unitPriceSnapshot;
    }
}
