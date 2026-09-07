using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Catalog;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لنوع الخدمة المقدمة من المغسلة (مثل: غسيل فقط، كي فقط، غسيل وكي، تنظيف جاف Dry Clean).
/// <para>
/// يتبع هذا الكيان مستأجراً محدداً (Tenant-Owned)، ويوفر الكود التعريفي للخدمة،
/// مع ضمان تفرد كود الخدمة داخل المستأجر الواحد عبر فهرس فريد مركب (TenantId, Code).
/// </para>
/// </summary>
public class LaundryService : TenantFullAuditedAggregateRoot
{
    /// <summary>
    /// اسم الخدمة الظاهر للعملاء (مثل: غسيل وكي، تنظيف بالبخار).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// كود فريد لتمييز الخدمة برمجياً وتشغيلياً (مثل: WASH_IRON, DRY_CLEAN, IRON_ONLY).
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// شرح تفصيلي لخطوات الخدمة ومميزاتها والمواد المستخدمة فيها.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كانت هذه الخدمة مفعلة ومتاحة للطلب من قبل العملاء.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private LaundryService()
    {
    }

    /// <summary>
    /// يُنشئ خدمة جديدة تابعة لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="name">اسم الخدمة.</param>
    /// <param name="code">كود الخدمة الفريد.</param>
    /// <param name="description">وصف تفصيلي اختياري.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    public LaundryService(
        Guid id,
        Guid tenantId,
        string name,
        string code,
        string? description = null,
        bool isActive = true)
        : base(id, tenantId)
    {
        SetName(name);
        SetCode(code);
        Description = description;
        IsActive = isActive;
    }

    /// <summary>
    /// يعدل اسم الخدمة مع التحقق من عدم فراغه ومطابقته للحد الأقصى للطول (100 حرف).
    /// </summary>
    /// <param name="name">الاسم الجديد.</param>
    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 100);
    }

    /// <summary>
    /// يعدل كود الخدمة مع التحقق من عدم فراغه ومطابقته للحد الأقصى للطول (50 حرفاً).
    /// </summary>
    /// <param name="code">الكود الجديد.</param>
    public void SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), maxLength: 50);
    }

    /// <summary>
    /// يعدل حالة تفعيل الخدمة في قائمة المغسلة.
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
