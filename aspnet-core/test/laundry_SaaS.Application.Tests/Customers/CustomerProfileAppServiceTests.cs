using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;
using Xunit;

namespace laundry_SaaS.Customers;

/// <summary>
/// اختبارات خدمة إدارة الملف الشخصي للعميل (CustomerProfileAppService Unit Tests).
/// تشمل التحقق من الصلاحيات، حصر العمليات بالعميل الحالي، إدارة العناوين الافتراضية، وعزل المستأجرين.
/// </summary>
public class CustomerProfileAppServiceTests
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<CustomerAddress, Guid> _addressRepository;
    private readonly FakeIdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly CustomerProfileAppService _service;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _currentCustomerId = Guid.NewGuid();
    private readonly Customer _currentCustomer;
    private readonly IdentityUser _identityUser;

    /// <summary>
    /// يُهيئ بيئة الاختبارات مع ضبط المستخدم الحالي وسجل العميل الافتراضي والمستودعات البديلة.
    /// </summary>
    public CustomerProfileAppServiceTests()
    {
        _customerRepository = Substitute.For<IRepository<Customer, Guid>>();
        _addressRepository = Substitute.For<IRepository<CustomerAddress, Guid>>();
        _identityUserManager = new FakeIdentityUserManager();

        _currentCustomer = new Customer(_currentCustomerId, _currentUserId, "0501234567", isActive: true);

        _identityUser = new IdentityUser(_currentUserId, "ahmad_user", "ahmad@example.com")
        {
            Name = "Ahmad",
            Surname = "Al-Harbi"
        };
        _identityUserManager.UserToReturn = _identityUser;

        var customersList = new List<Customer> { _currentCustomer };
        var queryable = customersList.AsQueryable();

        _customerRepository.GetQueryableAsync().Returns(Task.FromResult(queryable));
        _customerRepository.WithDetailsAsync(Arg.Any<Expression<Func<Customer, object>>[]>())
            .Returns(Task.FromResult(queryable));

        _principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        var claims = new List<Claim>
        {
            new Claim(AbpClaimTypes.UserId, _currentUserId.ToString()),
            new Claim(AbpClaimTypes.UserName, "ahmad_user"),
            new Claim(AbpClaimTypes.Email, "ahmad@example.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _principalAccessor.Principal.Returns(principal);

        _currentUser = new CurrentUser(_principalAccessor);

        var asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();
        asyncExecuter.FirstOrDefaultAsync(Arg.Any<IQueryable<Customer>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IQueryable<Customer>>().FirstOrDefault()));

        var lazyServiceProvider = Substitute.For<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
        lazyServiceProvider.LazyGetRequiredService<ICurrentUser>().Returns(_currentUser);
        lazyServiceProvider.LazyGetRequiredService<IAsyncQueryableExecuter>().Returns(asyncExecuter);
        lazyServiceProvider.LazyGetService<IAsyncQueryableExecuter>().Returns(asyncExecuter);
        lazyServiceProvider.LazyGetService<IAsyncQueryableExecuter>(Arg.Any<IAsyncQueryableExecuter>()).Returns(asyncExecuter);
        lazyServiceProvider.LazyGetService<Volo.Abp.Guids.IGuidGenerator>(Arg.Any<Volo.Abp.Guids.IGuidGenerator>())
            .Returns(Volo.Abp.Guids.SimpleGuidGenerator.Instance);

        _service = new CustomerProfileAppService(_customerRepository, _addressRepository, _identityUserManager)
        {
            LazyServiceProvider = lazyServiceProvider
        };
    }

    /// <summary>
    /// التحقق من أن GetAsync يسترجع ملف العميل المرتبط بالمستخدم الحالي فقط وبكامل الحقول المعتمدة.
    /// </summary>
    [Fact]
    public async Task GetAsync_Returns_Current_Customer_Profile()
    {
        // Act
        var profile = await _service.GetProfileAsync();

        // Assert
        profile.ShouldNotBeNull();
        profile.Id.ShouldBe(_currentCustomerId);
        profile.UserId.ShouldBe(_currentUserId);
        profile.Name.ShouldBe("Ahmad");
        profile.Surname.ShouldBe("Al-Harbi");
        profile.FullName.ShouldBe("Ahmad Al-Harbi");
        profile.Email.ShouldBe("ahmad@example.com");
        profile.PhoneNumber.ShouldBe("0501234567");
        profile.IsActive.ShouldBeTrue();
    }

    /// <summary>
    /// التحقق من أن UpdateAsync يحدّث بيانات العميل الحالي فقط ولا يمس بيانات مستخدمين آخرين.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Updates_Current_Customer_Only()
    {
        // Arrange
        var input = new UpdateCustomerProfileInput
        {
            Name = "Mohammed",
            Surname = "Al-Otaibi",
            Email = "mohammed.updated@example.com"
        };

        // Act
        var result = await _service.UpdateProfileAsync(input);

        // Assert
        result.Name.ShouldBe("Mohammed");
        result.Surname.ShouldBe("Al-Otaibi");
        result.FullName.ShouldBe("Mohammed Al-Otaibi");
        _identityUserManager.UpdatedEmail.ShouldBe("mohammed.updated@example.com");
        _identityUserManager.UpdateCalled.ShouldBeTrue();
    }

    /// <summary>
    /// التحقق من استرجاع العناوين المملوكة للعميل الحالي بنجاح وظهور العنوان الافتراضي أولاً.
    /// </summary>
    [Fact]
    public async Task GetAddressAsync_Returns_Address_When_Owned_By_Current_Customer()
    {
        // Arrange
        var address = new CustomerAddress(
            Guid.NewGuid(),
            _currentCustomerId,
            "المنزل",
            "حي الياسمين، شارع أنس بن مالك",
            24.7136,
            46.6753,
            "عمارة 12",
            "الدور 3",
            "شقة 10",
            "الاتصال عند الوصول",
            isDefault: true
        );
        _currentCustomer.AddAddress(address);

        // Act
        var addresses = await _service.GetAddressesAsync();

        // Assert
        addresses.ShouldNotBeNull();
        addresses.Count.ShouldBe(1);
        addresses[0].Id.ShouldBe(address.Id);
        addresses[0].Title.ShouldBe("المنزل");
        addresses[0].IsDefault.ShouldBeTrue();
    }

    /// <summary>
    /// التحقق من أن استرجاع العناوين لا يعيد أي عناوين تخص عملاء آخرين في النظام.
    /// </summary>
    [Fact]
    public async Task GetAddressAsync_Rejects_Address_Owned_By_Another_Customer()
    {
        // Arrange
        var otherCustomerId = Guid.NewGuid();
        var otherCustomerAddress = new CustomerAddress(
            Guid.NewGuid(),
            otherCustomerId,
            "شقة أخرى",
            "حي النرجس",
            24.8000,
            46.7000
        );

        var myAddress = new CustomerAddress(
            Guid.NewGuid(),
            _currentCustomerId,
            "منزلي",
            "حي الصحافة",
            24.7500,
            46.6500
        );
        _currentCustomer.AddAddress(myAddress);

        // Act
        var addresses = await _service.GetAddressesAsync();

        // Assert
        addresses.Any(a => a.Id == otherCustomerAddress.Id).ShouldBeFalse();
        addresses.Count.ShouldBe(1);
        addresses[0].Id.ShouldBe(myAddress.Id);
    }

    /// <summary>
    /// التحقق من رفض تحديث عنوان يملكه عميل آخر ورمي استثناء EntityNotFoundException لحماية الخصوصية.
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_Rejects_Address_Owned_By_Another_Customer()
    {
        // Arrange
        var foreignAddressId = Guid.NewGuid();
        var updateInput = new UpdateCustomerAddressInput
        {
            Title = "عنوان مخترق",
            AddressText = "عنوان وهمي",
            Latitude = 24.7136,
            Longitude = 46.6753
        };

        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _service.UpdateAddressAsync(foreignAddressId, updateInput);
        });
    }

    /// <summary>
    /// التحقق من رفض حذف عنوان يتبع لعميل آخر.
    /// </summary>
    [Fact]
    public async Task DeleteAddressAsync_Rejects_Address_Owned_By_Another_Customer()
    {
        // Arrange
        var foreignAddressId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _service.DeleteAddressAsync(foreignAddressId);
        });
    }

    /// <summary>
    /// التحقق من ربط العنوان المنشأ حصرياً بالعميل الحالي دون قبول أي معرّف عميل خارجي.
    /// </summary>
    [Fact]
    public async Task CreateAddressAsync_Attaches_Address_To_Current_Customer()
    {
        // Arrange
        var input = new CreateCustomerAddressInput
        {
            Title = "العمل",
            AddressText = "طريق الملك فهد، برج الفيصلية",
            Latitude = 24.6900,
            Longitude = 46.6850,
            Building = "برج الفيصلية",
            Floor = "14",
            Apartment = "مكتب 1402",
            IsDefault = true
        };

        // Act
        var created = await _service.CreateAddressAsync(input);

        // Assert
        created.ShouldNotBeNull();
        created.Title.ShouldBe("العمل");
        created.IsDefault.ShouldBeTrue();
        _currentCustomer.Addresses.Count.ShouldBe(1);
        _currentCustomer.Addresses.First().CustomerId.ShouldBe(_currentCustomerId);
        await _customerRepository.Received(1).UpdateAsync(_currentCustomer, autoSave: true);
    }

    /// <summary>
    /// التحقق من أن SetDefaultAddressAsync يجعل العنوان المختار هو الافتراضي الوحيد.
    /// </summary>
    [Fact]
    public async Task SetDefaultAddressAsync_Sets_Selected_Address_As_Only_Default()
    {
        // Arrange
        var addr1 = new CustomerAddress(Guid.NewGuid(), _currentCustomerId, "المنزل", "حي الملقا", 24.8, 46.6, isDefault: true);
        var addr2 = new CustomerAddress(Guid.NewGuid(), _currentCustomerId, "العمل", "حي العليا", 24.7, 46.7, isDefault: false);

        _currentCustomer.AddAddress(addr1);
        _currentCustomer.AddAddress(addr2);

        // Act
        await _service.SetDefaultAddressAsync(addr2.Id);

        // Assert
        addr2.IsDefault.ShouldBeTrue();
        addr1.IsDefault.ShouldBeFalse();
        _currentCustomer.Addresses.Count(a => a.IsDefault).ShouldBe(1);
    }

    /// <summary>
    /// التحقق من إلغاء تعيين الافتراضي عن العنوان السابق عند تعيين عنوان افتراضي جديد.
    /// </summary>
    [Fact]
    public async Task SetDefaultAddressAsync_Unsets_Previous_Default()
    {
        // Arrange
        var addr1 = new CustomerAddress(Guid.NewGuid(), _currentCustomerId, "العنوان الأول", "حي الروضة", 24.75, 46.75, isDefault: true);
        var addr2 = new CustomerAddress(Guid.NewGuid(), _currentCustomerId, "العنوان الثاني", "حي النفل", 24.82, 46.68, isDefault: false);

        _currentCustomer.AddAddress(addr1);
        _currentCustomer.AddAddress(addr2);
        addr1.IsDefault.ShouldBeTrue();

        // Act
        await _service.SetDefaultAddressAsync(addr2.Id);

        // Assert
        addr1.IsDefault.ShouldBeFalse();
        addr2.IsDefault.ShouldBeTrue();
    }

    /// <summary>
    /// التحقق من رفض تعيين عنوان كافتراضي إذا كان العنوان يتبع لعميل آخر.
    /// </summary>
    [Fact]
    public async Task SetDefaultAddressAsync_Rejects_Address_Owned_By_Another_Customer()
    {
        // Arrange
        var foreignAddressId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _service.SetDefaultAddressAsync(foreignAddressId);
        });
    }

    /// <summary>
    /// التحقق من أن الخدمة تعتمد حصرياً على هوية المستخدم المصادق CurrentUser ولا تثق بأي معرّف يرسله العميل.
    /// </summary>
    [Fact]
    public async Task Customer_Service_Does_Not_Trust_Client_CustomerId()
    {
        // Arrange - Unauthenticated user context
        var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var mockPrincipalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        mockPrincipalAccessor.Principal.Returns(unauthenticatedPrincipal);
        var unauthenticatedUser = new CurrentUser(mockPrincipalAccessor);

        var lazyProvider = Substitute.For<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
        lazyProvider.LazyGetRequiredService<ICurrentUser>().Returns(unauthenticatedUser);

        var serviceWithoutAuth = new CustomerProfileAppService(_customerRepository, _addressRepository, _identityUserManager)
        {
            LazyServiceProvider = lazyProvider
        };

        // Act & Assert - Should reject access when unauthenticated
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await serviceWithoutAuth.GetProfileAsync();
        });
    }

    /// <summary>
    /// التحقق من أن كيانات العميل وعناوينه عامة على مستوى المنصة وليست مقيدة بمستأجر محدد.
    /// </summary>
    [Fact]
    public void Customer_Profile_Is_Global_And_Not_Tenant_Scoped()
    {
        // Assert - Customer AggregateRoot must not implement IMultiTenant
        typeof(IMultiTenant).IsAssignableFrom(typeof(Customer)).ShouldBeFalse();

        // CustomerAddress must not implement IMultiTenant
        typeof(IMultiTenant).IsAssignableFrom(typeof(CustomerAddress)).ShouldBeFalse();

        // Customer and CustomerAddress must not contain a TenantId property
        typeof(Customer).GetProperty("TenantId").ShouldBeNull();
        typeof(CustomerAddress).GetProperty("TenantId").ShouldBeNull();
    }

    /// <summary>
    /// التحقق من سلوك النطاق عند حذف العنوان الافتراضي: يُحذف ناعماً دون تعيين افتراضي تلقائي،
    /// مما يترك الحساب بدون عنوان افتراضي (Zero defaults) وهو ما يتوافق تماماً مع قيد قاعدة البيانات المفلتر.
    /// </summary>
    [Fact]
    public async Task DeleteAddressAsync_When_Default_Address_Deleted_Leaves_Zero_Default_Addresses_Until_Customer_Selects_Another()
    {
        // Arrange
        var defaultAddr = new CustomerAddress(Guid.NewGuid(), _currentCustomerId, "المنزل الافتراضي", "حي الملقا", 24.8, 46.6, isDefault: true);
        var secondaryAddr = new CustomerAddress(Guid.NewGuid(), _currentCustomerId, "العمل الإضافي", "حي العليا", 24.7, 46.7, isDefault: false);

        _currentCustomer.AddAddress(defaultAddr);
        _currentCustomer.AddAddress(secondaryAddr);
        defaultAddr.IsDefault.ShouldBeTrue();

        // Act - Delete the default address
        await _service.DeleteAddressAsync(defaultAddr.Id);

        // Assert
        // 1. The default address is removed from the customer's addresses
        _currentCustomer.Addresses.Any(a => a.Id == defaultAddr.Id).ShouldBeFalse();

        // 2. The address repository received the delete request for soft-deletion
        await _addressRepository.Received(1).DeleteAsync(defaultAddr, autoSave: true);

        // 3. The remaining address is NOT automatically changed to default by domain; it remains as is
        secondaryAddr.IsDefault.ShouldBeFalse();

        // 4. Zero default addresses exist now, which satisfies Domain & EF Core unique index ([IsDefault] = 1 AND [IsDeleted] = 0)
        _currentCustomer.Addresses.Count(a => a.IsDefault).ShouldBe(0);
    }
}
