using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Customers;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) للملف الشخصي للعميل على المستوى العام للنظام (Global / Host-level profile).
/// <para>
/// العميل غير مقيد بمستأجر محدد (لا يطبق <see cref="Volo.Abp.MultiTenancy.IMultiTenant"/>)،
/// مما يتيح لنفس العميل تصفح وطلب خدمات الغسيل من عدة مغاسل ومستأجرين مختلفين عبر نفس الحساب.
/// لا يدعم الكيان الحذف الناعم (No Soft Delete) ولا الحذف الفيزيائي؛ حيث يتم التعامل مع الحسابات المتعثرة أو المخالفة
/// عبر خاصية التعطيل التشغيلي (<see cref="IsActive"/> و <see cref="Disable"/>) للحفاظ على التاريخ المالي للطلبات.
/// </para>
/// </summary>
public class Customer : AuditedAggregateRoot<Guid>
{
    /// <summary>
    /// معرّف المستخدم المقابل في نظام الهوية الموحد (ABP Identity User Id). يخضع لفهرس فريد على مستوى النظام.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// رقم الجوال الأساسي للعميل المستخدم في التواصل واستلام إشعارات الطلبات ورمز التحقق. يخضع لفهرس فريد.
    /// </summary>
    public string PhoneNumber { get; private set; } = null!;

    /// <summary>
    /// يشير إلى ما إذا كان حساب العميل نشطاً ومسموحاً له بإنشاء طلبات جديدة (true) أم معطلاً إدارياً (false).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// تاريخ ووقت تعطيل حساب العميل في حال إيقافه من قبل إدارة المنصة.
    /// </summary>
    public DateTime? DisabledAt { get; private set; }

    /// <summary>
    /// السبب الإداري أو التشغيلي الذي تم بناءً عليه تعطيل حساب العميل.
    /// </summary>
    public string? DisabledReason { get; private set; }

    /// <summary>
    /// قائمة العناوين الجغرافية التابعة للعميل (Child Entities)، وتُدار حصرياً من خلال هذا الجذر التجميعي.
    /// </summary>
    public virtual ICollection<CustomerAddress> Addresses { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Customer()
    {
        Addresses = new Collection<CustomerAddress>();
    }

    /// <summary>
    /// يُنشئ ملف عميل جديد على المستوى العام للمنصة (Host Scope).
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="userId">معرّف المستخدم في نظام الهوية.</param>
    /// <param name="phoneNumber">رقم هاتف العميل.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان UserId فارغاً.</exception>
    public Customer(
        Guid id,
        Guid userId,
        string phoneNumber,
        bool isActive = true)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new BusinessException("UserId must not be empty.");
        }

        UserId = userId;
        SetPhoneNumber(phoneNumber);
        IsActive = isActive;
        Addresses = new Collection<CustomerAddress>();
    }

    /// <summary>
    /// يعدل رقم جوال العميل مع التحقق من عدم فراغه وصحته.
    /// </summary>
    /// <param name="phoneNumber">رقم الجوال الجديد.</param>
    public void SetPhoneNumber(string phoneNumber)
    {
        PhoneNumber = Check.NotNullOrWhiteSpace(phoneNumber, nameof(phoneNumber), maxLength: 30);
    }

    /// <summary>
    /// يعطل حساب العميل ويمنعه من إجراء أي طلبات جديدة مع تسجيل السبب وتاريخ التعطيل (بدلاً من الحذف).
    /// </summary>
    /// <param name="reason">سبب التعطيل الإداري.</param>
    /// <param name="disabledAt">تاريخ ووقت التعطيل.</param>
    public void Disable(string reason, DateTime disabledAt)
    {
        IsActive = false;
        DisabledReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
        DisabledAt = disabledAt;
    }

    /// <summary>
    /// يعيد تفعيل حساب العميل المعطل وتصفير أسباب وتاريخ التعطيل.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        DisabledReason = null;
        DisabledAt = null;
    }

    /// <summary>
    /// يضيف عنواناً جغرافياً جديداً للعميل مع ضمان وجود عنوان افتراضي واحد فقط في آن واحد.
    /// </summary>
    /// <param name="address">كيان العنوان المراد إضافته.</param>
    public void AddAddress(CustomerAddress address)
    {
        Check.NotNull(address, nameof(address));
        if (address.IsDefault || !Addresses.Any())
        {
            foreach (var addr in Addresses)
            {
                addr.IsDefault = false;
            }
            address.IsDefault = true;
        }
        Addresses.Add(address);
    }

    /// <summary>
    /// يعين أحد عناوين العميل الحالية كعنوان افتراضي ويلغي صفة الافتراضي عن باقي العناوين.
    /// </summary>
    /// <param name="addressId">معرّف العنوان المطلوب جعله افتراضياً.</param>
    /// <exception cref="BusinessException">يتم رميها إذا لم يتم العثور على العنوان ضمن قائمة عناوين العميل.</exception>
    public void SetDefaultAddress(Guid addressId)
    {
        var target = Addresses.FirstOrDefault(a => a.Id == addressId);
        if (target == null)
        {
            throw new BusinessException($"Address with id {addressId} not found for this customer.");
        }

        foreach (var addr in Addresses)
        {
            addr.IsDefault = false;
        }

        target.IsDefault = true;
    }
}
