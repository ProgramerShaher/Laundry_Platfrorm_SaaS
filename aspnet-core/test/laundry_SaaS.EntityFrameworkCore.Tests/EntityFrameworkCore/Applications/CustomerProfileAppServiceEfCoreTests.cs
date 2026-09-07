using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using laundry_SaaS.Customers;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Applications;

/// <summary>
/// اختبارات تكاملية حقيقية لخدمة <see cref="CustomerProfileAppService"/> مدعومة بـ EF Core وقاعدة بيانات SQLite في الذاكرة.
/// تثبت هذه الاختبارات أن:
/// 1. يتم تحميل العناوين التابعة للعميل (Addresses navigation) بشكل موثوق عبر EF Core.
/// 2. يسترجع <see cref="CustomerProfileAppService.GetAddressesAsync"/> العناوين المحفوظة فعلياً في قاعدة البيانات.
/// 3. تعيين العنوان الافتراضي <see cref="CustomerProfileAppService.SetDefaultAddressAsync"/> يحافظ على قاعدة العنوان الافتراضي الوحيد ويصمد عبر استعلام جديد في سياق قاعدة بيانات منفصل.
/// </summary>
[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class CustomerProfileAppServiceEfCoreTests : laundry_SaaSEntityFrameworkCoreTestBase
{
    private readonly ICustomerProfileAppService _customerProfileAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public CustomerProfileAppServiceEfCoreTests()
    {
        _customerProfileAppService = GetRequiredService<ICustomerProfileAppService>();
        _customerRepository = GetRequiredService<IRepository<Customer, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Customer_Addresses_Should_Persist_And_Load_Reliably_With_Real_EfCore()
    {
        // Arrange: إعداد معرّف مستخدم وعميل به عنوانان أحدهما افتراضي والآخر عادي
        var testUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var address1Id = Guid.NewGuid();
        var address2Id = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var customer = new Customer(customerId, testUserId, "555123456");
            customer.AddAddress(new CustomerAddress(
                address1Id,
                customerId,
                "المنزل",
                "شارع الملك فهد",
                24.7136,
                46.6753,
                "برج 1",
                "3",
                "12",
                "قريب من المسجد",
                isDefault: true
            ));

            customer.AddAddress(new CustomerAddress(
                address2Id,
                customerId,
                "العمل",
                "طريق العليا",
                24.7000,
                46.6800,
                "مبنى الأعمال",
                "5",
                "501",
                null,
                isDefault: false
            ));

            await _customerRepository.InsertAsync(customer, autoSave: true);
        });

        // تمثيل هوية المستخدم الحالي في سياق الاختبار
        using (_currentPrincipalAccessor.Change(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, testUserId.ToString()),
            new Claim(AbpClaimTypes.UserName, "test_customer"),
            new Claim(AbpClaimTypes.Email, "test_customer@laundry.local")
        }))))
        {
            // Act 1: استدعاء GetAddressesAsync من التطبيق
            var addresses = await _customerProfileAppService.GetAddressesAsync();

            // Assert 1: إثبات أن الخدمة جلبت العناوين من EF Core فعلياً ورتبت العنوان الافتراضي أولاً
            addresses.ShouldNotBeNull();
            addresses.Count.ShouldBe(2);
            addresses[0].Id.ShouldBe(address1Id);
            addresses[0].IsDefault.ShouldBeTrue();
            addresses[0].Title.ShouldBe("المنزل");
            addresses[1].Id.ShouldBe(address2Id);
            addresses[1].IsDefault.ShouldBeFalse();
            addresses[1].Title.ShouldBe("العمل");

            // Act 2: تعيين العنوان الثاني كعنوان افتراضي
            await _customerProfileAppService.SetDefaultAddressAsync(address2Id);

            // Assert 2: التحقق من التغيير عبر الخدمة
            var updatedAddresses = await _customerProfileAppService.GetAddressesAsync();
            updatedAddresses[0].Id.ShouldBe(address2Id);
            updatedAddresses[0].IsDefault.ShouldBeTrue();
            var oldDefault = updatedAddresses.First(a => a.Id == address1Id);
            oldDefault.IsDefault.ShouldBeFalse();
        }

        // Assert 3: التحقق في Unit Of Work منفصل تماماً ومباشرة من قاعدة البيانات (Fresh Read)
        await WithUnitOfWorkAsync(async () =>
        {
            var freshQuery = await _customerRepository.WithDetailsAsync(c => c.Addresses);
            var reloadedCustomer = await freshQuery.FirstOrDefaultAsync(c => c.Id == customerId);

            reloadedCustomer.ShouldNotBeNull();
            reloadedCustomer.Addresses.Count.ShouldBe(2);

            var persistedAddr2 = reloadedCustomer.Addresses.First(a => a.Id == address2Id);
            var persistedAddr1 = reloadedCustomer.Addresses.First(a => a.Id == address1Id);

            persistedAddr2.IsDefault.ShouldBeTrue();
            persistedAddr1.IsDefault.ShouldBeFalse();

            // التحقق من وجود عنوان افتراضي واحد فقط
            reloadedCustomer.Addresses.Count(a => a.IsDefault).ShouldBe(1);
        });
    }
}
