using System;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) للملف التشغيلي لسائق التوصيل والاستلام التابع للمغسلة.
/// <para>
/// يربط هذا الكيان حساب المستخدم في نظام الهوية (<see cref="UserId"/>) بالمستأجر المحدد (<see cref="TenantAuditedAggregateRoot.TenantId"/>).
/// الفرق الجوهري هو أن IdentityUser يمثل بيانات الاعتماد والمصادقة (Username/Email/Password)،
/// بينما يمثل Driver الحالة التشغيلية للمندوب (جاهزيته لاستلام المهام، بيانات المركبة، وتاريخ المهام الموكلة إليه).
/// يخضع لفهرس فريد يمنع تكرار تسجيل نفس المستخدم كسائق في نفس المغسلة.
/// </para>
/// </summary>
public class Driver : TenantFullAuditedAggregateRoot
{
    /// <summary>
    /// معرّف المستخدم في نظام الهوية الموحد (ABP Identity User Id).
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان حساب السائق مفعلاً وصالحاً للعمل لدى المغسلة.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// يشير إلى الجاهزية اللحظية للسائق لاستقبال مهام استلام وتوصيل جديدة (Online / On-Duty).
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// بيانات وتفاصيل مركبة التوصيل (مثل: نوع السيارة، رقم اللوحة، الموديل).
    /// </summary>
    public string? VehicleDetails { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Driver()
    {
    }

    /// <summary>
    /// يُنشئ ملف سائق جديد تابع لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="userId">معرّف المستخدم في نظام الهوية.</param>
    /// <param name="vehicleDetails">تفاصيل المركبة الاختيارية.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    /// <param name="isAvailable">حالة التوفر الأولية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان UserId فارغاً.</exception>
    public Driver(
        Guid id,
        Guid tenantId,
        Guid userId,
        string? vehicleDetails = null,
        bool isActive = true,
        bool isAvailable = true)
        : base(id, tenantId)
    {
        if (userId == Guid.Empty)
        {
            throw new BusinessException("UserId must not be empty.");
        }

        UserId = userId;
        VehicleDetails = vehicleDetails;
        IsActive = isActive;
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// يعدل حالة تفعيل السائق، وفي حال تعطيله يتم تحويل حالة توفره تلقائياً إلى غير متاح (<c>IsAvailable = false</c>).
    /// </summary>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        if (!isActive)
        {
            IsAvailable = false;
        }
    }

    /// <summary>
    /// يعدل حالة توفر السائق لاستقبال المهام مع التحقق من أن السائق مفعل أولاً.
    /// </summary>
    /// <param name="isAvailable">حالة التوفر الجديدة.</param>
    /// <exception cref="BusinessException">يتم رميها عند محاولة جعل سائق معطل في حالة متاح.</exception>
    public void SetAvailability(bool isAvailable)
    {
        if (isAvailable && !IsActive)
        {
            throw new BusinessException("Cannot set an inactive driver as available.");
        }

        IsAvailable = isAvailable;
    }

    /// <summary>
    /// يحدّث بيانات وتفاصيل مركبة السائق.
    /// </summary>
    /// <param name="vehicleDetails">بيانات المركبة الجديدة.</param>
    public void UpdateVehicleDetails(string? vehicleDetails)
    {
        VehicleDetails = vehicleDetails;
    }
}
