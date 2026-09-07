using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Laundries;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) للملف الوظيفي والتشغيلي لأحد موظفي المغسلة.
/// <para>
/// يربط هذا الكيان حساب المستخدم في نظام الهوية (<see cref="UserId"/>) بالمستأجر المحدد (<see cref="TenantAuditedAggregateRoot.TenantId"/>)،
/// دون تكرار البيانات الأساسية للمستخدم (مثل الاسم أو الهاتف أو البريد) إلا ما يتعلق بالدور التشغيلي المباشر للمغسلة.
/// يخضع لفهرس فريد مركب يضمن عدم ارتباط المستخدم بأكثر من ملف موظف داخل المستأجر الواحد.
/// </para>
/// </summary>
public class LaundryStaffProfile : TenantFullAuditedAggregateRoot
{
    /// <summary>
    /// معرّف المستخدم في نظام الهوية الموحد (ABP Identity User Id).
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// حالة تفعيل الموظف وصلاحيته لمباشرة العمل داخل نظام المغسلة.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// المسمى الوظيفي الداخلي للموظف (مثل: فني فرز، مسؤول فحص، عامل غسيل، مشرف تشغيل).
    /// </summary>
    public string? JobTitle { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private LaundryStaffProfile()
    {
    }

    /// <summary>
    /// يُنشئ ملفاً وظيفياً جديداً لموظف تابع لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="userId">معرّف المستخدم في نظام الهوية.</param>
    /// <param name="jobTitle">المسمى الوظيفي الاختياري.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان UserId فارغاً.</exception>
    public LaundryStaffProfile(
        Guid id,
        Guid tenantId,
        Guid userId,
        string? jobTitle = null,
        bool isActive = true)
        : base(id, tenantId)
    {
        if (userId == Guid.Empty)
        {
            throw new BusinessException("UserId must not be empty.");
        }

        UserId = userId;
        JobTitle = jobTitle;
        IsActive = isActive;
    }

    /// <summary>
    /// يعدل حالة تفعيل الموظف داخل المغسلة.
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// يعدل المسمى الوظيفي للموظف.
    /// </summary>
    /// <param name="jobTitle">المسمى الوظيفي الجديد.</param>
    public void SetJobTitle(string? jobTitle)
    {
        JobTitle = jobTitle;
    }
}
