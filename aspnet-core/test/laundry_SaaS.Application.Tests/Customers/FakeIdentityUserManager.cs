using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Volo.Abp.Caching;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.Settings;
using Volo.Abp.Threading;

namespace laundry_SaaS.Customers;

/// <summary>
/// بديل اختباري (Test Double / Fake) لمدير مستخدمي الهوية <see cref="IdentityUserManager"/>.
/// يُستخدم في اختبارات الوحدة المعزولة دون الحاجة لتشغيل قاعدة بيانات فعلية أو تهيئة سياق OpenIddict.
/// </summary>
public class FakeIdentityUserManager : IdentityUserManager
{
    /// <summary>
    /// كائن المستخدم المخصص للإرجاع في نتائج الاستعلامات أثناء الاختبار.
    /// </summary>
    public IdentityUser? UserToReturn { get; set; }

    /// <summary>
    /// يشير إلى ما إذا تم استدعاء دالة التحديث <see cref="UpdateAsync(IdentityUser)"/>.
    /// </summary>
    public bool UpdateCalled { get; private set; }

    /// <summary>
    /// يحتفظ بالبريد الإلكتروني الذي تم تمريره لـ <see cref="SetEmailAsync(IdentityUser, string?)"/> للتحقق منه.
    /// </summary>
    public string? UpdatedEmail { get; private set; }

    /// <summary>
    /// يُنشئ نسخة جديدة من البديل الاختباري مع تغذية المعاملات العشرين المطلوبة لمشيد الفئة الأم.
    /// </summary>
    public FakeIdentityUserManager()
        : base(
            (IdentityUserStore)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(IdentityUserStore)),
            Substitute.For<IIdentityRoleRepository>(),
            Substitute.For<IIdentityUserRepository>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<IPasswordHasher<IdentityUser>>(),
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            Substitute.For<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<IdentityUserManager>>(),
            Substitute.For<ICancellationTokenProvider>(),
            Substitute.For<IOrganizationUnitRepository>(),
            Substitute.For<ISettingProvider>(),
            Substitute.For<IDistributedEventBus>(),
            Substitute.For<IIdentityLinkUserRepository>(),
            Substitute.For<IDistributedCache<AbpDynamicClaimCacheItem>>(),
            Substitute.For<IOptions<AbpMultiTenancyOptions>>(),
            Substitute.For<ICurrentTenant>(),
            Substitute.For<IDataFilter>())
    {
    }

    /// <summary>
    /// يسترجع المستخدم المحدد للاختبار عبر معرّفه.
    /// </summary>
    /// <param name="userId">معرّف المستخدم كنص.</param>
    /// <returns>كائن المستخدم المحدد للاختبار.</returns>
    public override Task<IdentityUser?> FindByIdAsync(string userId)
    {
        return Task.FromResult(UserToReturn);
    }

    /// <summary>
    /// يسترجع المستخدم المحدد للاختبار عبر معرّفه الفريد.
    /// </summary>
    /// <param name="id">معرّف المستخدم الفريد.</param>
    /// <returns>كائن المستخدم المحدد.</returns>
    /// <exception cref="EntityNotFoundException">إذا لم يتم ضبط المستخدم مسبقاً.</exception>
    public override Task<IdentityUser> GetByIdAsync(Guid id)
    {
        return Task.FromResult(UserToReturn ?? throw new EntityNotFoundException(typeof(IdentityUser), id));
    }

    /// <summary>
    /// يحاكي عملية تعيين البريد الإلكتروني للمستخدم مع تسجيل القيمة للتحقق.
    /// </summary>
    /// <param name="user">كيان المستخدم.</param>
    /// <param name="email">البريد الإلكتروني الجديد.</param>
    /// <returns>نتيجة النجاح.</returns>
    public override Task<IdentityResult> SetEmailAsync(IdentityUser user, string? email)
    {
        UpdatedEmail = email;
        return Task.FromResult(IdentityResult.Success);
    }

    /// <summary>
    /// يحاكي عملية تحديث بيانات المستخدم في نظام الهوية ويسجل حالة الاستدعاء.
    /// </summary>
    /// <param name="user">كيان المستخدم المحدث.</param>
    /// <returns>نتيجة النجاح.</returns>
    public override Task<IdentityResult> UpdateAsync(IdentityUser user)
    {
        UpdateCalled = true;
        return Task.FromResult(IdentityResult.Success);
    }
}
