using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Catalog;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) لنوع قطعة الملابس أو المفروشات (مثل: ثوب، قميص، بدلة، بطانية، فستان).
/// <para>
/// يتبع هذا الكيان مستأجراً محدداً (Tenant-Owned)، ويتيح لكل مغسلة تخصيص كتالوج أصناف الملابس الخاص بها،
/// مع دعم الحذف الناعم (Soft Delete) والتدقيق الكامل لعمليات الإنشاء والتعديل.
/// </para>
/// </summary>
public class LaundryItemType : TenantFullAuditedAggregateRoot
{
    /// <summary>
    /// اسم نوع القطعة الظاهر للعملاء وطاقم العمل (مثل: ثوب رجالي، بنطال جينز).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// رمز كودي فريد لتمييز نوع القطعة وسرعة البحث والفرز (مثل: THOB-01, SHT-02).
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// وصف تفصيلي اختياري للقطعة أو أي تعليمات فرز خاصة بها.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان هذا الصنف متاحاً ومفعلاً للاختيار في التطبيق وقائمة الأسعار.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private LaundryItemType()
    {
    }

    /// <summary>
    /// يُنشئ صنف قطعة ملابس جديد تابع لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="name">اسم نوع القطعة.</param>
    /// <param name="code">كود نوع القطعة.</param>
    /// <param name="description">وصف اختياري للقطعة.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    public LaundryItemType(
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
    /// يعدل اسم نوع القطعة مع التحقق من عدم فراغه ومطابقته للحد الأقصى للطول (100 حرف).
    /// </summary>
    /// <param name="name">الاسم الجديد.</param>
    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 100);
    }

    /// <summary>
    /// يعدل كود نوع القطعة مع التحقق من عدم فراغه ومطابقته للحد الأقصى للطول (50 حرفاً).
    /// </summary>
    /// <param name="code">الكود الجديد.</param>
    public void SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), maxLength: 50);
    }

    /// <summary>
    /// يعدل حالة تفعيل نوع القطعة في قائمة المغسلة.
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
