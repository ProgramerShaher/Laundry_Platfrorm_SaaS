using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace laundry_SaaS.Customers;

/// <summary>
/// خدمة إدارة الملف الشخصي للعميل وعناوينه في منصة الغسيل (Customer Profile Application Service).
/// <para>
/// تخدم العميل المسجل حالياً من خلال استخراج هويته من سياق المستخدم الحالي (<see cref="ICurrentUser.Id"/>)
/// وربطه بسجل العميل العام على مستوى المنصة (<see cref="Customer.UserId"/>).
/// تضمن الخدمة أن كافة العمليات مقيدة بملف وعناوين العميل الحالي ولا تقبل أي معرّفات مفروضة من العميل.
/// </para>
/// </summary>
[Authorize]
public class CustomerProfileAppService : laundry_SaaSAppService, ICustomerProfileAppService
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<CustomerAddress, Guid> _addressRepository;
    private readonly IdentityUserManager _identityUserManager;

    /// <summary>
    /// يُنشئ نسخة جديدة من خدمة ملف العميل مع حقن المستودعات وخدمات الهوية المطلوبة.
    /// </summary>
    /// <param name="customerRepository">مستودع الجذر التجميعي للعملاء.</param>
    /// <param name="addressRepository">مستودع الكيان التابع لعناوين العملاء.</param>
    /// <param name="identityUserManager">مدير مستخدمي الهوية في ABP لتحديث بيانات المستخدم الأساسية.</param>
    public CustomerProfileAppService(
        IRepository<Customer, Guid> customerRepository,
        IRepository<CustomerAddress, Guid> addressRepository,
        IdentityUserManager identityUserManager)
    {
        _customerRepository = customerRepository;
        _addressRepository = addressRepository;
        _identityUserManager = identityUserManager;
    }

    /// <summary>
    /// يسترجع الملف الشخصي الكامل للعميل المسجل حالياً بما يشمل بيانات حسابه ودفتر عناوينه المحفوظة.
    /// </summary>
    /// <returns>كائن بيانات الملف الشخصي للعميل <see cref="CustomerProfileDto"/>.</returns>
    /// <exception cref="AbpAuthorizationException">يتم رميها إذا لم يكن المستخدم مسجلاً للدخول.</exception>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يتم العثور على ملف عميل مرتبط بالمستخدم الحالي.</exception>
    public virtual async Task<CustomerProfileDto> GetProfileAsync()
    {
        var customer = await GetCurrentCustomerAsync(includeAddresses: true);
        var user = await _identityUserManager.FindByIdAsync(customer.UserId.ToString());

        return MapToProfileDto(customer, user);
    }

    /// <summary>
    /// يحدّث البيانات الأساسية لملف العميل الشخصي (الاسم واسم العائلة والبريد الإلكتروني) في نظام الهوية الموحد.
    /// </summary>
    /// <param name="input">البيانات الجديدة المراد تحديثها <see cref="UpdateCustomerProfileInput"/>.</param>
    /// <returns>بيانات الملف الشخصي للعميل بعد التحديث <see cref="CustomerProfileDto"/>.</returns>
    /// <exception cref="AbpAuthorizationException">يتم رميها إذا لم يكن المستخدم مسجلاً للدخول.</exception>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يكن للعميل سجل مرتبط.</exception>
    public virtual async Task<CustomerProfileDto> UpdateProfileAsync(UpdateCustomerProfileInput input)
    {
        Check.NotNull(input, nameof(input));

        var customer = await GetCurrentCustomerAsync(includeAddresses: true);
        var user = await _identityUserManager.GetByIdAsync(customer.UserId);

        user.Name = input.Name;
        user.Surname = input.Surname;

        if (!string.IsNullOrWhiteSpace(input.Email) && !string.Equals(user.Email, input.Email, StringComparison.OrdinalIgnoreCase))
        {
            (await _identityUserManager.SetEmailAsync(user, input.Email)).CheckErrors();
        }

        (await _identityUserManager.UpdateAsync(user)).CheckErrors();

        return MapToProfileDto(customer, user);
    }

    /// <summary>
    /// يسترجع قائمة جميع العناوين المسجلة في دفتر عناوين العميل الحالي فقط، مرتبة بحيث يظهر العنوان الافتراضي أولاً.
    /// </summary>
    /// <returns>قائمة كائنات عناوين العميل <see cref="CustomerAddressDto"/>.</returns>
    public virtual async Task<List<CustomerAddressDto>> GetAddressesAsync()
    {
        var customer = await GetCurrentCustomerAsync(includeAddresses: true);

        return customer.Addresses
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Title)
            .Select(MapToAddressDto)
            .ToList();
    }

    /// <summary>
    /// يضيف عنواناً جديداً إلى دفتر عناوين العميل الحالي مع ضمان قواعد تعيين العنوان الافتراضي.
    /// </summary>
    /// <param name="input">بيانات العنوان الجديد المراد إضافته <see cref="CreateCustomerAddressInput"/>.</param>
    /// <returns>بيانات العنوان المنشأ <see cref="CustomerAddressDto"/>.</returns>
    public virtual async Task<CustomerAddressDto> CreateAddressAsync(CreateCustomerAddressInput input)
    {
        Check.NotNull(input, nameof(input));

        var customer = await GetCurrentCustomerAsync(includeAddresses: true);

        if (input.IsDefault)
        {
            var oldDefault = customer.Addresses.FirstOrDefault(a => a.IsDefault);
            if (oldDefault != null)
            {
                typeof(CustomerAddress).GetProperty(nameof(CustomerAddress.IsDefault))?.SetValue(oldDefault, false);
                await _customerRepository.UpdateAsync(customer, autoSave: true);
            }
        }

        var address = new CustomerAddress(
            GuidGenerator.Create(),
            customer.Id,
            input.Title,
            input.AddressText,
            input.Latitude,
            input.Longitude,
            input.Building,
            input.Floor,
            input.Apartment,
            input.Notes,
            input.IsDefault
        );

        customer.AddAddress(address);

        await _customerRepository.UpdateAsync(customer, autoSave: true);

        return MapToAddressDto(address);
    }

    /// <summary>
    /// يحدّث تفاصيل عنوان مسجل مسبقاً للعميل الحالي مع التحقق الصارم من ملكية العميل للعنوان.
    /// </summary>
    /// <param name="id">معرّف العنوان المراد تحديثه.</param>
    /// <param name="input">البيانات المحدثة للعنوان <see cref="UpdateCustomerAddressInput"/>.</param>
    /// <returns>بيانات العنوان بعد التحديث <see cref="CustomerAddressDto"/>.</returns>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يتم العثور على العنوان أو كان يتبع لعميل آخر.</exception>
    public virtual async Task<CustomerAddressDto> UpdateAddressAsync(Guid id, UpdateCustomerAddressInput input)
    {
        Check.NotNull(input, nameof(input));

        var customer = await GetCurrentCustomerAsync(includeAddresses: true);

        var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
        if (address == null || address.CustomerId != customer.Id)
        {
            throw new EntityNotFoundException(typeof(CustomerAddress), id);
        }

        address.SetDetails(
            input.Title,
            input.AddressText,
            input.Latitude,
            input.Longitude,
            input.Building,
            input.Floor,
            input.Apartment,
            input.Notes
        );

        if (input.IsDefault && !address.IsDefault)
        {
            var oldDefault = customer.Addresses.FirstOrDefault(a => a.IsDefault && a.Id != id);
            if (oldDefault != null)
            {
                typeof(CustomerAddress).GetProperty(nameof(CustomerAddress.IsDefault))?.SetValue(oldDefault, false);
                await _customerRepository.UpdateAsync(customer, autoSave: true);
            }

            customer.SetDefaultAddress(id);
        }

        await _customerRepository.UpdateAsync(customer, autoSave: true);

        return MapToAddressDto(address);
    }

    /// <summary>
    /// يحذف عنواناً من دفتر عناوين العميل الحالي (حذف ناعم يحافظ على سجل الطلبات المرتبطة تاريخياً) مع التحقق من ملكية العميل له.
    /// </summary>
    /// <param name="id">معرّف العنوان المراد حذفه.</param>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يتم العثور على العنوان أو كان ملكاً لعميل آخر.</exception>
    public virtual async Task DeleteAddressAsync(Guid id)
    {
        var customer = await GetCurrentCustomerAsync(includeAddresses: true);

        var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
        if (address == null || address.CustomerId != customer.Id)
        {
            throw new EntityNotFoundException(typeof(CustomerAddress), id);
        }

        customer.Addresses.Remove(address);
        await _addressRepository.DeleteAsync(address, autoSave: true);
        await _customerRepository.UpdateAsync(customer, autoSave: true);
    }

    /// <summary>
    /// يعين أحد عناوين العميل الحالي كعنوان افتراضي وحيد مع إلغاء صفة الافتراضي عن باقي العناوين في معاملة ذرية واحدة.
    /// </summary>
    /// <param name="id">معرّف العنوان المراد تعيينه كافتراضي.</param>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يكن العنوان موجوداً أو كان يخص عميلاً آخر.</exception>
    public virtual async Task SetDefaultAddressAsync(Guid id)
    {
        var customer = await GetCurrentCustomerAsync(includeAddresses: true);

        var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
        if (address == null || address.CustomerId != customer.Id)
        {
            throw new EntityNotFoundException(typeof(CustomerAddress), id);
        }

        if (!address.IsDefault)
        {
            var oldDefault = customer.Addresses.FirstOrDefault(a => a.IsDefault && a.Id != id);
            if (oldDefault != null)
            {
                typeof(CustomerAddress).GetProperty(nameof(CustomerAddress.IsDefault))?.SetValue(oldDefault, false);
                await _customerRepository.UpdateAsync(customer, autoSave: true);
            }

            customer.SetDefaultAddress(id);
        }

        await _customerRepository.UpdateAsync(customer, autoSave: true);
    }

    /// <summary>
    /// طريقة مساعدة داخلية لاسترجاع كيان العميل الحالي من قاعدة البيانات استناداً إلى معرّف المستخدم المصادق (<see cref="ICurrentUser.Id"/>).
    /// </summary>
    /// <param name="includeAddresses">ما إذا كان يجب تضمين العناوين التابعة للعميل في الاستعلام.</param>
    /// <returns>كيان العميل الحالي <see cref="Customer"/>.</returns>
    /// <exception cref="AbpAuthorizationException">يتم رميها إذا كان المستخدم غير مسجل الدخول.</exception>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يتم العثور على ملف عميل يطابق المستخدم الحالي.</exception>
    protected virtual async Task<Customer> GetCurrentCustomerAsync(bool includeAddresses = false)
    {
        if (!CurrentUser.IsAuthenticated || CurrentUser.Id == null)
        {
            throw new AbpAuthorizationException("المستخدم غير مسجل الدخول.");
        }

        var currentUserId = CurrentUser.GetId();

        var query = includeAddresses
            ? await _customerRepository.WithDetailsAsync(c => c.Addresses)
            : await _customerRepository.GetQueryableAsync();

        var customer = await AsyncExecuter.FirstOrDefaultAsync(query.Where(c => c.UserId == currentUserId));

        if (customer == null)
        {
            throw new EntityNotFoundException(typeof(Customer), currentUserId);
        }

        return customer;
    }

    /// <summary>
    /// يُجمّع كائن نقل البيانات <see cref="CustomerProfileDto"/> من كيان العميل العام وبيانات المستخدم المقابل في نظام الهوية.
    /// </summary>
    /// <param name="customer">كيان العميل في نطاق العمل.</param>
    /// <param name="user">كيان المستخدم في نظام الهوية ABP Identity إن وجد.</param>
    /// <returns>كائن بيانات الملف الشخصي الكامل للعميل.</returns>
    protected virtual CustomerProfileDto MapToProfileDto(Customer customer, IdentityUser? user)
    {
        var name = user?.Name ?? string.Empty;
        var surname = user?.Surname;
        var fullName = string.IsNullOrWhiteSpace(surname)
            ? name
            : $"{name} {surname}".Trim();

        return new CustomerProfileDto
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Name = name,
            Surname = surname,
            FullName = fullName,
            PhoneNumber = !string.IsNullOrWhiteSpace(customer.PhoneNumber)
                ? customer.PhoneNumber
                : user?.PhoneNumber ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            IsActive = customer.IsActive,
            Addresses = customer.Addresses
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Title)
                .Select(MapToAddressDto)
                .ToList()
        };
    }

    /// <summary>
    /// يُحوّل كيان عنوان العميل <see cref="CustomerAddress"/> إلى كائن نقل البيانات <see cref="CustomerAddressDto"/>.
    /// </summary>
    /// <param name="address">كيان العنوان في النطاق.</param>
    /// <returns>كائن بيانات العنوان المخصص للواجهات.</returns>
    protected static CustomerAddressDto MapToAddressDto(CustomerAddress address)
    {
        return new CustomerAddressDto
        {
            Id = address.Id,
            Title = address.Title,
            AddressText = address.AddressText,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            Street = null,
            Building = address.Building,
            Floor = address.Floor,
            Apartment = address.Apartment,
            Notes = address.Notes,
            IsDefault = address.IsDefault
        };
    }
}
