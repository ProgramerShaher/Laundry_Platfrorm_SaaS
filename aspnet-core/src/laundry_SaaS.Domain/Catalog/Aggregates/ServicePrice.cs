using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Catalog;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) المستقل لتسعير خدمة معينة على صنف ملابس محدد (Matrix Pricing).
/// <para>
/// يربط هذا الكيان بين نوع القطعة (<see cref="LaundryItemTypeId"/>) ونوع الخدمة (<see cref="LaundryServiceId"/>)
/// بروابط معرفات (Reference by ID) دون وجود علاقات ملاحة مباشرة لحماية حدود DDD.
/// يمثل السعر القيمة المالية الأساسية المعروضة للعميل في التطبيق قبل إنشاء الطلب.
/// ويخضع لفهرس فريد يمنع تكرار السعر لنفس الزوج (ItemType, Service) داخل المستأجر الواحد.
/// </para>
/// </summary>
public class ServicePrice : TenantFullAuditedAggregateRoot
{
    /// <summary>
    /// معرّف صنف قطعة الملابس (مشار إليه بالـ ID دون Navigation Property).
    /// </summary>
    public Guid LaundryItemTypeId { get; private set; }

    /// <summary>
    /// معرّف نوع الخدمة المقدمة على القطعة (مشار إليه بالـ ID دون Navigation Property).
    /// </summary>
    public Guid LaundryServiceId { get; private set; }

    /// <summary>
    /// سعر تقديم الخدمة لهذا الصنف (بالريال/العملة المحلية)، بدقة decimal(18,2).
    /// يشترط أن يكون صفراً أو قيمة موجبة ولا يُقبل سعر سالب.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان هذا السعر مفعلاً ونشطاً في قائمة الأسعار الحالية.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private ServicePrice()
    {
    }

    /// <summary>
    /// يُنشئ سجل تسعير جديد لصنف وخدمة محددين تابعين لمستأجر.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="laundryItemTypeId">معرّف نوع القطعة.</param>
    /// <param name="laundryServiceId">معرّف نوع الخدمة.</param>
    /// <param name="price">قيمة السعر المالية.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المعرفات فارغة أو السعر سالباً.</exception>
    public ServicePrice(
        Guid id,
        Guid tenantId,
        Guid laundryItemTypeId,
        Guid laundryServiceId,
        decimal price,
        bool isActive = true)
        : base(id, tenantId)
    {
        if (laundryItemTypeId == Guid.Empty)
        {
            throw new BusinessException("LaundryItemTypeId must not be empty.");
        }

        if (laundryServiceId == Guid.Empty)
        {
            throw new BusinessException("LaundryServiceId must not be empty.");
        }

        LaundryItemTypeId = laundryItemTypeId;
        LaundryServiceId = laundryServiceId;
        SetPrice(price);
        IsActive = isActive;
    }

    /// <summary>
    /// يحدّث قيمة السعر مع التحقق الصارم من أن السعر لا يقل عن الصفر (non-negative).
    /// </summary>
    /// <param name="price">قيمة السعر الجديدة.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان السعر أقل من صفر.</exception>
    public void SetPrice(decimal price)
    {
        if (price < 0)
        {
            throw new BusinessException("Price must be greater than or equal to zero.");
        }

        Price = price;
    }

    /// <summary>
    /// يعدل حالة تفعيل هذا السعر في الكتالوج.
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
