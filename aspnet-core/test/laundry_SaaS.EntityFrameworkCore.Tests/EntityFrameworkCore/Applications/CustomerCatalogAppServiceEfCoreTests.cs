using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using laundry_SaaS.Catalog;
using laundry_SaaS.Laundries;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Applications;

/// <summary>
/// اختبارات تكاملية حقيقية لخدمة <see cref="CustomerCatalogAppService"/> مدعومة بـ EF Core وقاعدة بيانات SQLite.
/// تثبت هذه الاختبارات:
/// 1. استكشاف المغاسل عبر مستأجرين متعددين (Cross-Tenant Discovery) ضمن نطاق التغطية الجغرافية المحفوظ في قاعدة البيانات.
/// 2. عزل بيانات الكتالوج لكل مستأجر وعدم تسريب أصناف أو خدمات مغسلة لمغسلة أخرى.
/// 3. استبعاد الكيانات المعطلة أو المحذوفة ناعماً (Soft Deleted) من الكتالوج.
/// 4. التحقق من علاقات التسعير (ServicePrice) وإرجاع التوليفات المسعرة فقط.
/// 5. التحقق من حساب المسافات الجغرافية ومطابقتها لحدود التغطية المحفوظة.
/// </summary>
[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class CustomerCatalogAppServiceEfCoreTests : laundry_SaaSEntityFrameworkCoreTestBase
{
    private readonly ICustomerCatalogAppService _catalogAppService;
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly IRepository<LaundryItemType, Guid> _itemTypeRepository;
    private readonly IRepository<LaundryService, Guid> _serviceRepository;
    private readonly IRepository<ServicePrice, Guid> _servicePriceRepository;
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly ICurrentTenant _currentTenant;

    public CustomerCatalogAppServiceEfCoreTests()
    {
        _catalogAppService = GetRequiredService<ICustomerCatalogAppService>();
        _laundryRepository = GetRequiredService<IRepository<Laundry, Guid>>();
        _itemTypeRepository = GetRequiredService<IRepository<LaundryItemType, Guid>>();
        _serviceRepository = GetRequiredService<IRepository<LaundryService, Guid>>();
        _servicePriceRepository = GetRequiredService<IRepository<ServicePrice, Guid>>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    private IDisposable AuthenticateCustomer(string userName = "customer_ef_test")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.Email, $"{userName}@laundry.local")
        };
        return _principalAccessor.Change(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")));
    }

    /// <summary>
    /// A. التحقق من اكتشاف المغاسل عبر مستأجرين متعددين: مغسلة مستأجر أ ومغسلة مستأجر ب يظهران معاً للعميل إذا كان ضمن نطاق تغطيتهما.
    /// </summary>
    [Fact]
    public async Task Cross_Tenant_Discovery_Should_Find_Multiple_Laundries_In_Coverage_Area()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var laundryAId = Guid.NewGuid();
        var laundryBId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantAId))
            {
                var laundryA = new Laundry(
                    laundryAId,
                    tenantAId,
                    "مغسلة النور - مستأجر أ",
                    "0511111111",
                    24.7136,
                    46.6753,
                    new CoverageArea(24.7136, 46.6753, 10.0, 20.0m),
                    deliveryFee: 12.0m,
                    minimumOrderAmount: 20.0m,
                    estimatedProcessingHours: 24,
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundryA, autoSave: true);
            }

            using (_currentTenant.Change(tenantBId))
            {
                var laundryB = new Laundry(
                    laundryBId,
                    tenantBId,
                    "مغسلة الأمل - مستأجر ب",
                    "0522222222",
                    24.7200,
                    46.6800,
                    new CoverageArea(24.7200, 46.6800, 10.0, 25.0m),
                    deliveryFee: 15.0m,
                    minimumOrderAmount: 25.0m,
                    estimatedProcessingHours: 48,
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundryB, autoSave: true);
            }
        });

        using (AuthenticateCustomer("discovery_customer"))
        {
            // استعلام بموقع قريب من كلتا المغسلتين (24.7150, 46.6760)
            var input = new GetNearbyLaundriesInput
            {
                Latitude = 24.7150,
                Longitude = 46.6760,
                MaxDistanceKm = 15.0
            };

            var laundries = await _catalogAppService.GetNearbyLaundriesAsync(input);

            laundries.ShouldNotBeNull();
            laundries.ShouldContain(l => l.Id == laundryAId);
            laundries.ShouldContain(l => l.Id == laundryBId);
            laundries.First(l => l.Id == laundryAId).Name.ShouldBe("مغسلة النور - مستأجر أ");
            laundries.First(l => l.Id == laundryBId).Name.ShouldBe("مغسلة الأمل - مستأجر ب");
        }
    }

    /// <summary>
    /// B. التحقق من عزل الكتالوج: استعراض كتالوج مغسلة أ يعيد أصنافها وخدماتها فقط دون تسريب أي صنف من مستأجر ب.
    /// </summary>
    [Fact]
    public async Task Catalog_Isolation_Should_Return_Only_Selected_Laundry_Catalog_Without_Leaking_Other_Tenants()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var laundryAId = Guid.NewGuid();
        var laundryBId = Guid.NewGuid();

        var itemTypeAId = Guid.NewGuid();
        var itemTypeBId = Guid.NewGuid();
        var serviceAId = Guid.NewGuid();
        var serviceBId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantAId))
            {
                var laundryA = new Laundry(
                    laundryAId,
                    tenantAId,
                    "مغسلة الرياض - أ",
                    "0533333333",
                    24.7136,
                    46.6753,
                    new CoverageArea(24.7136, 46.6753, 10.0, 30.0m),
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundryA, autoSave: true);

                var itemTypeA = new LaundryItemType(itemTypeAId, tenantAId, "ثوب أبيض فاخر", "THOB_WHITE", isActive: true);
                await _itemTypeRepository.InsertAsync(itemTypeA, autoSave: true);

                var serviceA = new LaundryService(serviceAId, tenantAId, "غسيل بخار ونشا", "STEAM_STARCH", isActive: true);
                await _serviceRepository.InsertAsync(serviceA, autoSave: true);

                var priceA = new ServicePrice(Guid.NewGuid(), tenantAId, itemTypeAId, serviceAId, 15.0m, isActive: true);
                await _servicePriceRepository.InsertAsync(priceA, autoSave: true);
            }

            using (_currentTenant.Change(tenantBId))
            {
                var laundryB = new Laundry(
                    laundryBId,
                    tenantBId,
                    "مغسلة جدة - ب",
                    "0544444444",
                    21.5433,
                    39.1728,
                    new CoverageArea(21.5433, 39.1728, 10.0, 30.0m),
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundryB, autoSave: true);

                var itemTypeB = new LaundryItemType(itemTypeBId, tenantBId, "عباءة حرير خاصة", "ABAYA_SILK", isActive: true);
                await _itemTypeRepository.InsertAsync(itemTypeB, autoSave: true);

                var serviceB = new LaundryService(serviceBId, tenantBId, "تنظيف جاف عضوي", "ORGANIC_DRY", isActive: true);
                await _serviceRepository.InsertAsync(serviceB, autoSave: true);

                var priceB = new ServicePrice(Guid.NewGuid(), tenantBId, itemTypeBId, serviceBId, 60.0m, isActive: true);
                await _servicePriceRepository.InsertAsync(priceB, autoSave: true);
            }
        });

        using (AuthenticateCustomer("catalog_customer"))
        {
            // استعراض كتالوج مغسلة أ
            var catalogA = await _catalogAppService.GetLaundryCatalogAsync(laundryAId);

            catalogA.ShouldNotBeNull();
            catalogA.LaundryId.ShouldBe(laundryAId);
            catalogA.Items.ShouldContain(i => i.ItemTypeId == itemTypeAId);
            catalogA.Items.ShouldNotContain(i => i.ItemTypeId == itemTypeBId);

            var servicesInA = catalogA.Items.SelectMany(i => i.AvailableServices).ToList();
            servicesInA.ShouldContain(s => s.ServiceId == serviceAId);
            servicesInA.ShouldNotContain(s => s.ServiceId == serviceBId);
        }
    }

    /// <summary>
    /// C. التحقق من استبعاد الأصناف والخدمات المحذوفة ناعماً (Soft Deleted) والمعطلة من الكتالوج في قاعدة البيانات الفعلية.
    /// </summary>
    [Fact]
    public async Task Soft_Delete_And_Inactive_Items_Should_Be_Excluded_From_Catalog()
    {
        var tenantId = Guid.NewGuid();
        var laundryId = Guid.NewGuid();
        var activeItemId = Guid.NewGuid();
        var deletedItemId = Guid.NewGuid();
        var inactiveItemId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantId))
            {
                var laundry = new Laundry(
                    laundryId,
                    tenantId,
                    "مغسلة الفلترة",
                    "0555555555",
                    24.7136,
                    46.6753,
                    new CoverageArea(24.7136, 46.6753, 10.0),
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundry, autoSave: true);

                var service = new LaundryService(serviceId, tenantId, "كي بالبخار", "STEAM_PRESS", isActive: true);
                await _serviceRepository.InsertAsync(service, autoSave: true);

                // صنف نشط
                var activeItem = new LaundryItemType(activeItemId, tenantId, "ثوب نشط", "ACT_01", isActive: true);
                await _itemTypeRepository.InsertAsync(activeItem, autoSave: true);
                await _servicePriceRepository.InsertAsync(new ServicePrice(Guid.NewGuid(), tenantId, activeItemId, serviceId, 10m, isActive: true), autoSave: true);

                // صنف معطل
                var inactiveItem = new LaundryItemType(inactiveItemId, tenantId, "صنف معطل", "INACT_01", isActive: false);
                await _itemTypeRepository.InsertAsync(inactiveItem, autoSave: true);
                await _servicePriceRepository.InsertAsync(new ServicePrice(Guid.NewGuid(), tenantId, inactiveItemId, serviceId, 10m, isActive: true), autoSave: true);

                // صنف محذوف ناعماً
                var deletedItem = new LaundryItemType(deletedItemId, tenantId, "صنف محذوف ناعماً", "DEL_01", isActive: true);
                await _itemTypeRepository.InsertAsync(deletedItem, autoSave: true);
                await _servicePriceRepository.InsertAsync(new ServicePrice(Guid.NewGuid(), tenantId, deletedItemId, serviceId, 10m, isActive: true), autoSave: true);
                await _itemTypeRepository.DeleteAsync(deletedItem, autoSave: true);
            }
        });

        using (AuthenticateCustomer("filter_customer"))
        {
            var catalog = await _catalogAppService.GetLaundryCatalogAsync(laundryId);

            catalog.Items.ShouldContain(i => i.ItemTypeId == activeItemId);
            catalog.Items.ShouldNotContain(i => i.ItemTypeId == inactiveItemId);
            catalog.Items.ShouldNotContain(i => i.ItemTypeId == deletedItemId);
        }
    }

    /// <summary>
    /// D. التحقق من أن الكتالوج يعيد فقط توليفات الأصناف والخدمات التي تمتلك تسعيراً فعلياً محفوظاً في جدول ServicePrices.
    /// </summary>
    [Fact]
    public async Task ServicePrice_Relation_Should_Only_Include_Combinations_With_Active_ServicePrice()
    {
        var tenantId = Guid.NewGuid();
        var laundryId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var pricedServiceId = Guid.NewGuid();
        var unpricedServiceId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantId))
            {
                var laundry = new Laundry(
                    laundryId,
                    tenantId,
                    "مغسلة الأسعار",
                    "0566666666",
                    24.7136,
                    46.6753,
                    new CoverageArea(24.7136, 46.6753, 10.0),
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundry, autoSave: true);

                var item = new LaundryItemType(itemId, tenantId, "بطانية شتوية", "BLANKET", isActive: true);
                await _itemTypeRepository.InsertAsync(item, autoSave: true);

                var pricedService = new LaundryService(pricedServiceId, tenantId, "غسيل وتطهير", "WASH_SAN", isActive: true);
                var unpricedService = new LaundryService(unpricedServiceId, tenantId, "كي فقط", "IRON_ONLY", isActive: true);
                await _serviceRepository.InsertAsync(pricedService, autoSave: true);
                await _serviceRepository.InsertAsync(unpricedService, autoSave: true);

                // حفظ سعر للخدمة الأولى فقط
                var price = new ServicePrice(Guid.NewGuid(), tenantId, itemId, pricedServiceId, 40.0m, isActive: true);
                await _servicePriceRepository.InsertAsync(price, autoSave: true);
            }
        });

        using (AuthenticateCustomer("price_customer"))
        {
            var catalog = await _catalogAppService.GetLaundryCatalogAsync(laundryId);

            var itemDto = catalog.Items.FirstOrDefault(i => i.ItemTypeId == itemId);
            itemDto.ShouldNotBeNull();
            itemDto.AvailableServices.Count.ShouldBe(1);
            itemDto.AvailableServices[0].ServiceId.ShouldBe(pricedServiceId);
            itemDto.AvailableServices[0].Price.ShouldBe(40.0m);
            itemDto.AvailableServices.ShouldNotContain(s => s.ServiceId == unpricedServiceId);
        }
    }

    /// <summary>
    /// E. التحقق من احترام حدود التغطية الجغرافية المحفوظة في قاعدة البيانات (CoverageArea.DeliveryRadiusKm).
    /// </summary>
    [Fact]
    public async Task Distance_Coverage_Behavior_Should_Respect_Persisted_CoverageArea()
    {
        var tenantId = Guid.NewGuid();
        var laundryId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantId))
            {
                // مغسلة في الرياض بنصف قطر تغطية 5 كم فقط
                var laundry = new Laundry(
                    laundryId,
                    tenantId,
                    "مغسلة النطاق المحدود",
                    "0577777777",
                    24.7136,
                    46.6753,
                    new CoverageArea(24.7136, 46.6753, 5.0, 15.0m),
                    isActive: true,
                    acceptingOrders: true
                );
                await _laundryRepository.InsertAsync(laundry, autoSave: true);
            }
        });

        using (AuthenticateCustomer("distance_customer"))
        {
            // استعلام من عميل يقع على بعد ~20 كم من المغسلة (خارج نطاق 5 كم)
            var inputFar = new GetNearbyLaundriesInput
            {
                Latitude = 24.5300,
                Longitude = 46.6753,
                MaxDistanceKm = 50.0
            };

            var farResult = await _catalogAppService.GetNearbyLaundriesAsync(inputFar);
            farResult.ShouldNotContain(l => l.Id == laundryId);

            // استعلام من عميل يقع على بعد ~1.5 كم من المغسلة (داخل نطاق 5 كم)
            var inputNear = new GetNearbyLaundriesInput
            {
                Latitude = 24.7200,
                Longitude = 46.6800,
                MaxDistanceKm = 10.0
            };

            var nearResult = await _catalogAppService.GetNearbyLaundriesAsync(inputNear);
            nearResult.ShouldContain(l => l.Id == laundryId);
        }
    }
}
