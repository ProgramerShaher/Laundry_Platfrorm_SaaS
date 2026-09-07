using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Customers;

/// <summary>
/// واجهة خدمة إدارة الملف الشخصي وعناوين العميل (Customer Profile & Address App Service).
/// تخدم العميل المسجل حالياً (عبر CurrentUser) لإدارة بياناته الشخصية ودفتر عناوينه.
/// </summary>
public interface ICustomerProfileAppService : IApplicationService
{
    /// <summary>
    /// يسترجع الملف الشخصي الكامل للعميل الحالي بما في ذلك عناوينه المسجلة.
    /// </summary>
    /// <returns>بيانات ملف العميل وعناوينه.</returns>
    Task<CustomerProfileDto> GetProfileAsync();

    /// <summary>
    /// يحدّث البيانات الأساسية للملف الشخصي للعميل الحالي (الاسم، البريد الإلكتروني).
    /// </summary>
    /// <param name="input">البيانات الشخصية الجديدة.</param>
    /// <returns>بيانات ملف العميل بعد التحديث.</returns>
    Task<CustomerProfileDto> UpdateProfileAsync(UpdateCustomerProfileInput input);

    /// <summary>
    /// يسترجع جميع العناوين المسجلة في دفتر عناوين العميل الحالي.
    /// </summary>
    /// <returns>قائمة عناوين العميل.</returns>
    Task<List<CustomerAddressDto>> GetAddressesAsync();

    /// <summary>
    /// يضيف عنواناً جديداً إلى دفتر عناوين العميل الحالي.
    /// </summary>
    /// <param name="input">تفاصيل العنوان والإحداثيات.</param>
    /// <returns>بيانات العنوان المنشأ.</returns>
    Task<CustomerAddressDto> CreateAddressAsync(CreateCustomerAddressInput input);

    /// <summary>
    /// يحدّث بيانات عنوان مسجل مسبقاً في حساب العميل الحالي مع التحقق من ملكيته للعنوان.
    /// </summary>
    /// <param name="id">معرّف العنوان.</param>
    /// <param name="input">البيانات المحدثة للعنوان.</param>
    /// <returns>بيانات العنوان بعد التحديث.</returns>
    Task<CustomerAddressDto> UpdateAddressAsync(Guid id, UpdateCustomerAddressInput input);

    /// <summary>
    /// يحذف عنواناً من دفتر عناوين العميل الحالي (حذف ناعم).
    /// </summary>
    /// <param name="id">معرّف العنوان المراد حذفه.</param>
    Task DeleteAddressAsync(Guid id);

    /// <summary>
    /// يعين عنواناً محدداً كعنوان افتراضي للعميل الحالي ويلغي الافتراضي السابق تلقائياً.
    /// </summary>
    /// <param name="id">معرّف العنوان المراد تعيينه كافتراضي.</param>
    Task SetDefaultAddressAsync(Guid id);
}
