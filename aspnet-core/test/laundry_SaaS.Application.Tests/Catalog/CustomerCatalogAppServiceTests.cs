using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using laundry_SaaS.Laundries;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using Xunit;

namespace laundry_SaaS.Catalog;

/// <summary>
/// اختبارات خدمة كتالوج العميل (CustomerCatalogAppService Unit Tests).
/// تتحقق من استكشاف المغاسل القريبة وحساب المسافات وحدود التغطية الجغرافية،
/// وعزل بيانات المستأجر، وفلترة الكتالوج النشط فقط، وحصانة سلطة الأسعار في الخادم.
/// </summary>
public class CustomerCatalogAppServiceTests
{
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly IRepository<LaundryItemType, Guid> _itemTypeRepository;
    private readonly IRepository<LaundryService, Guid> _serviceRepository;
    private readonly IRepository<ServicePrice, Guid> _servicePriceRepository;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly IDataFilter _dataFilter;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly CustomerCatalogAppService _service;

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _laundryAId = Guid.NewGuid();
    private readonly Guid _laundryBId = Guid.NewGuid();
    private readonly List<Laundry> _laundriesList;
    private readonly List<LaundryItemType> _itemTypesList;
    private readonly List<LaundryService> _servicesList;
    private readonly List<ServicePrice> _pricesList;

    public CustomerCatalogAppServiceTests()
    {
        _laundryRepository = Substitute.For<IRepository<Laundry, Guid>>();
        _itemTypeRepository = Substitute.For<IRepository<LaundryItemType, Guid>>();
        _serviceRepository = Substitute.For<IRepository<LaundryService, Guid>>();
        _servicePriceRepository = Substitute.For<IRepository<ServicePrice, Guid>>();
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(new DateTime(2026, 9, 7, 8, 0, 0)); // Monday 08:00 AM

        // إعداد مغسلتين بنطاقات تغطية محددة
        // مغسلة أ في الرياض (24.7136, 46.6753) بنصف قطر 10 كم
        var laundryA = new Laundry(
            _laundryAId,
            _tenantA,
            "مغسلة النقاء",
            "0501111111",
            24.7136,
            46.6753,
            new CoverageArea(24.7136, 46.6753, 10.0, 30.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 30.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );

        // مغسلة ب في موقع أبعد (24.8500, 46.7500) بنصف قطر 5 كم
        var laundryB = new Laundry(
            _laundryBId,
            _tenantB,
            "مغسلة الصفاء",
            "0502222222",
            24.8500,
            46.7500,
            new CoverageArea(24.8500, 46.7500, 5.0, 25.0m),
            deliveryFee: 10.0m,
            minimumOrderAmount: 25.0m,
            estimatedProcessingHours: 48,
            isActive: true,
            acceptingOrders: true
        );

        _laundriesList = new List<Laundry> { laundryA, laundryB };
        _itemTypesList = new List<LaundryItemType>();
        _servicesList = new List<LaundryService>();
        _pricesList = new List<ServicePrice>();

        SetupRepositories();

        // إعداد المستخدم المصادق
        _principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        var claims = new List<Claim>
        {
            new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
            new Claim(AbpClaimTypes.UserName, "test_customer"),
            new Claim(AbpClaimTypes.Email, "customer@laundry.local")
        };
        _principalAccessor.Principal.Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")));
        _currentUser = new CurrentUser(_principalAccessor);

        // إعداد IDataFilter
        _dataFilter = Substitute.For<IDataFilter>();
        _dataFilter.Disable<IMultiTenant>().Returns(Substitute.For<IDisposable>());

        // إعداد AsyncExecuter لدعم ToListAsync و FirstOrDefaultAsync
        _asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();
        _asyncExecuter.ToListAsync(Arg.Any<IQueryable<Laundry>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<IQueryable<Laundry>>().ToList()));
        _asyncExecuter.ToListAsync(Arg.Any<IQueryable<LaundryItemType>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<IQueryable<LaundryItemType>>().ToList()));
        _asyncExecuter.ToListAsync(Arg.Any<IQueryable<LaundryService>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<IQueryable<LaundryService>>().ToList()));
        _asyncExecuter.ToListAsync(Arg.Any<IQueryable<ServicePrice>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<IQueryable<ServicePrice>>().ToList()));
        _asyncExecuter.FirstOrDefaultAsync(Arg.Any<IQueryable<Laundry>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<IQueryable<Laundry>>().FirstOrDefault()));

        var lazyServiceProvider = Substitute.For<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
        lazyServiceProvider.LazyGetRequiredService<ICurrentUser>().Returns(_currentUser);
        lazyServiceProvider.LazyGetRequiredService<IDataFilter>().Returns(_dataFilter);
        lazyServiceProvider.LazyGetService<IDataFilter>().Returns(_dataFilter);
        lazyServiceProvider.LazyGetService<IDataFilter>(Arg.Any<IDataFilter>()).Returns(_dataFilter);
        lazyServiceProvider.LazyGetRequiredService<IAsyncQueryableExecuter>().Returns(_asyncExecuter);
        lazyServiceProvider.LazyGetService<IAsyncQueryableExecuter>().Returns(_asyncExecuter);
        lazyServiceProvider.LazyGetService<IAsyncQueryableExecuter>(Arg.Any<IAsyncQueryableExecuter>()).Returns(_asyncExecuter);

        _service = new CustomerCatalogAppService(
            _laundryRepository,
            _itemTypeRepository,
            _serviceRepository,
            _servicePriceRepository,
            _clock
        )
        {
            LazyServiceProvider = lazyServiceProvider
        };
    }

    private void SetupRepositories()
    {
        _laundryRepository.GetQueryableAsync().Returns(_ => Task.FromResult(_laundriesList.AsQueryable()));
        _laundryRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(_laundriesList.FirstOrDefault(l => l.Id == ci.Arg<Guid>())));
        _laundryRepository.WithDetailsAsync(Arg.Any<Expression<Func<Laundry, object>>[]>())
            .Returns(_ => Task.FromResult(_laundriesList.AsQueryable()));

        _itemTypeRepository.GetQueryableAsync().Returns(_ => Task.FromResult(_itemTypesList.AsQueryable()));
        _serviceRepository.GetQueryableAsync().Returns(_ => Task.FromResult(_servicesList.AsQueryable()));
        _servicePriceRepository.GetQueryableAsync().Returns(_ => Task.FromResult(_pricesList.AsQueryable()));
    }

    /// <summary>
    /// 1. التحقق من أن البحث عن المغاسل القريبة يسترجع المغاسل المفعلة والمستقبلة للطلبات فقط ويستبعد المعطلة أو الموقوفة مؤقتاً.
    /// </summary>
    [Fact]
    public async Task NearbyLaundries_Returns_Only_Active_And_Accepting_Laundries()
    {
        // Arrange: إيقاف استقبال الطلبات في مغسلة أ، وتعطيل مغسلة ب
        _laundriesList[0].SetAcceptingOrders(false);
        _laundriesList[1].SetActive(false);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 20.0
        };

        // Act
        var result = await _service.GetNearbyLaundriesAsync(input);

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 2. التحقق من استبعاد المغسلة التي يقع موقع العميل خارج نصف قطر تغطيتها الجغرافية (CoverageArea.DeliveryRadiusKm).
    /// </summary>
    [Fact]
    public async Task NearbyLaundries_Excludes_Laundry_Outside_Coverage_Radius()
    {
        // Arrange: موقع العميل على بعد ~18 كم من مغسلة أ (نصف قطرها 10 كم)
        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.5500,
            Longitude = 46.6753,
            MaxDistanceKm = 50.0
        };

        // Act
        var result = await _service.GetNearbyLaundriesAsync(input);

        // Assert
        result.ShouldNotContain(l => l.Id == _laundryAId);
    }

    /// <summary>
    /// 3. التحقق من تضمين المغسلة عندما يقع موقع العميل داخل نصف قطر تغطيتها وضمن مسافة البحث.
    /// </summary>
    [Fact]
    public async Task NearbyLaundries_Includes_Laundry_Inside_Coverage_Radius()
    {
        // Arrange: موقع العميل على بعد ~2 كم من مغسلة أ (داخل نصف قطر 10 كم)
        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7200,
            Longitude = 46.6800,
            MaxDistanceKm = 10.0
        };

        // Act
        var result = await _service.GetNearbyLaundriesAsync(input);

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain(l => l.Id == _laundryAId);
        result.First(l => l.Id == _laundryAId).Name.ShouldBe("مغسلة النقاء");
        result.First(l => l.Id == _laundryAId).DeliveryFee.ShouldBe(15.0m);
    }

    /// <summary>
    /// 4. التحقق من فرز المغاسل القريبة تصاعدياً حسب المسافة الأقرب للعميل (DistanceKm ascending).
    /// </summary>
    [Fact]
    public async Task NearbyLaundries_Sorts_By_Distance_When_Contract_Requires_It()
    {
        // Arrange: إضافة مغسلة ثالثة قريبة جداً بنصف قطر كبير
        var laundryCId = Guid.NewGuid();
        var laundryC = new Laundry(
            laundryCId,
            Guid.NewGuid(),
            "مغسلة الأقرب",
            "0503333333",
            24.7140,
            46.6760,
            new CoverageArea(24.7140, 46.6760, 15.0, 20.0m),
            deliveryFee: 8.0m,
            minimumOrderAmount: 20.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Add(laundryC);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 20.0
        };

        // Act
        var result = await _service.GetNearbyLaundriesAsync(input);

        // Assert
        result.Count.ShouldBeGreaterThanOrEqualTo(2);
        for (int i = 0; i < result.Count - 1; i++)
        {
            result[i].DistanceKm.ShouldBeLessThanOrEqualTo(result[i + 1].DistanceKm);
        }
    }

    /// <summary>
    /// 5. التحقق من أن GetNearbyLaundriesInput لا يحتوي على TenantId ولا يثق بأي مدخلات مستأجر من العميل.
    /// </summary>
    [Fact]
    public void NearbyLaundries_Does_Not_Trust_Client_TenantId()
    {
        // Assert: التأكد أن مدخلات البحث لا تحتوي على أي خاصية باسم TenantId
        var properties = typeof(GetNearbyLaundriesInput).GetProperties();
        properties.ShouldNotContain(p => p.Name.Equals("TenantId", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 6. التحقق من أن GetLaundryCatalogAsync يعيد بيانات المغسلة المحددة وأصنافها فقط.
    /// </summary>
    [Fact]
    public async Task GetCatalog_Returns_Only_Selected_Laundry_Data()
    {
        // Arrange: إضافة صنف وخدمة وسعر لمغسلة أ
        var itemTypeA = new LaundryItemType(Guid.NewGuid(), _tenantA, "ثوب رجالي", "THOB", isActive: true);
        var serviceA = new LaundryService(Guid.NewGuid(), _tenantA, "غسيل وكي", "WASH_IRON", isActive: true);
        var priceA = new ServicePrice(Guid.NewGuid(), _tenantA, itemTypeA.Id, serviceA.Id, 12.50m, isActive: true);

        _itemTypesList.Add(itemTypeA);
        _servicesList.Add(serviceA);
        _pricesList.Add(priceA);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        catalog.ShouldNotBeNull();
        catalog.LaundryId.ShouldBe(_laundryAId);
        catalog.LaundryName.ShouldBe("مغسلة النقاء");
        catalog.Items.Count.ShouldBe(1);
        catalog.Items[0].ItemTypeName.ShouldBe("ثوب رجالي");
        catalog.Items[0].AvailableServices.Count.ShouldBe(1);
        catalog.Items[0].AvailableServices[0].ServiceName.ShouldBe("غسيل وكي");
        catalog.Items[0].AvailableServices[0].Price.ShouldBe(12.50m);
    }

    /// <summary>
    /// 7. التحقق من عزل المستأجرين: طلب كتالوج مغسلة أ لا يسرب أي صنف أو خدمة أو سعر لمغسلة ب (مستأجر ب).
    /// </summary>
    [Fact]
    public async Task GetCatalog_Does_Not_Leak_Another_Tenant_Catalog()
    {
        // Arrange: أصناف لمستأجر أ وأصناف لمستأجر ب
        var itemA = new LaundryItemType(Guid.NewGuid(), _tenantA, "ثوب أ", "THOB_A", isActive: true);
        var srvA = new LaundryService(Guid.NewGuid(), _tenantA, "خدمة أ", "SRV_A", isActive: true);
        var prcA = new ServicePrice(Guid.NewGuid(), _tenantA, itemA.Id, srvA.Id, 10.0m, isActive: true);

        var itemB = new LaundryItemType(Guid.NewGuid(), _tenantB, "ثوب ب تسريب", "THOB_B", isActive: true);
        var srvB = new LaundryService(Guid.NewGuid(), _tenantB, "خدمة ب تسريب", "SRV_B", isActive: true);
        var prcB = new ServicePrice(Guid.NewGuid(), _tenantB, itemB.Id, srvB.Id, 50.0m, isActive: true);

        _itemTypesList.AddRange(new[] { itemA, itemB });
        _servicesList.AddRange(new[] { srvA, srvB });
        _pricesList.AddRange(new[] { prcA, prcB });

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        catalog.Items.ShouldNotContain(i => i.ItemTypeName == "ثوب ب تسريب");
        catalog.Items.SelectMany(i => i.AvailableServices).ShouldNotContain(s => s.ServiceName == "خدمة ب تسريب");
    }

    /// <summary>
    /// 8. التحقق من أن الكتالوج يستبعد أصناف الملابس المعطلة (IsActive == false).
    /// </summary>
    [Fact]
    public async Task GetCatalog_Returns_Only_Active_ItemTypes()
    {
        // Arrange: صنف معطل
        var itemInactive = new LaundryItemType(Guid.NewGuid(), _tenantA, "صنف معطل", "INACTIVE_ITEM", isActive: false);
        var service = new LaundryService(Guid.NewGuid(), _tenantA, "كي", "IRON", isActive: true);
        var price = new ServicePrice(Guid.NewGuid(), _tenantA, itemInactive.Id, service.Id, 5.0m, isActive: true);

        _itemTypesList.Add(itemInactive);
        _servicesList.Add(service);
        _pricesList.Add(price);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        catalog.Items.ShouldNotContain(i => i.ItemTypeId == itemInactive.Id);
    }

    /// <summary>
    /// 9. التحقق من أن الكتالوج يستبعد الخدمات المعطلة (IsActive == false).
    /// </summary>
    [Fact]
    public async Task GetCatalog_Returns_Only_Active_Services()
    {
        // Arrange: خدمة معطلة
        var item = new LaundryItemType(Guid.NewGuid(), _tenantA, "قميص", "SHIRT", isActive: true);
        var srvInactive = new LaundryService(Guid.NewGuid(), _tenantA, "خدمة معطلة", "INACTIVE_SRV", isActive: false);
        var price = new ServicePrice(Guid.NewGuid(), _tenantA, item.Id, srvInactive.Id, 7.0m, isActive: true);

        _itemTypesList.Add(item);
        _servicesList.Add(srvInactive);
        _pricesList.Add(price);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        catalog.Items.ShouldNotContain(i => i.AvailableServices.Any(s => s.ServiceId == srvInactive.Id));
    }

    /// <summary>
    /// 10. التحقق من أن الكتالوج يستبعد التسعيرات المعطلة (IsActive == false).
    /// </summary>
    [Fact]
    public async Task GetCatalog_Returns_Only_Active_ServicePrices()
    {
        // Arrange: سعر معطل
        var item = new LaundryItemType(Guid.NewGuid(), _tenantA, "فستان", "DRESS", isActive: true);
        var service = new LaundryService(Guid.NewGuid(), _tenantA, "تنظيف جاف", "DRY_CLEAN", isActive: true);
        var priceInactive = new ServicePrice(Guid.NewGuid(), _tenantA, item.Id, service.Id, 30.0m, isActive: false);

        _itemTypesList.Add(item);
        _servicesList.Add(service);
        _pricesList.Add(priceInactive);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        catalog.Items.ShouldNotContain(i => i.ItemTypeId == item.Id);
    }

    /// <summary>
    /// 11. التحقق من أن الكتالوج لا يُنشئ أو يعيد أي توليفة (ItemType × Service) لا تمتلك تسعيراً مسجلاً.
    /// </summary>
    [Fact]
    public async Task GetCatalog_Does_Not_Return_Service_Combinations_Without_ServicePrice()
    {
        // Arrange: صنف وخدمتان، لكن التسعير مسجل لخدمة واحدة فقط
        var item = new LaundryItemType(Guid.NewGuid(), _tenantA, "بشت", "BISHT", isActive: true);
        var pricedService = new LaundryService(Guid.NewGuid(), _tenantA, "تنظيف بخار", "STEAM", isActive: true);
        var unpricedService = new LaundryService(Guid.NewGuid(), _tenantA, "غسيل ماء", "WATER", isActive: true);

        var price = new ServicePrice(Guid.NewGuid(), _tenantA, item.Id, pricedService.Id, 45.0m, isActive: true);

        _itemTypesList.Add(item);
        _servicesList.AddRange(new[] { pricedService, unpricedService });
        _pricesList.Add(price);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        var catalogItem = catalog.Items.FirstOrDefault(i => i.ItemTypeId == item.Id);
        catalogItem.ShouldNotBeNull();
        catalogItem.AvailableServices.Count.ShouldBe(1);
        catalogItem.AvailableServices[0].ServiceId.ShouldBe(pricedService.Id);
        catalogItem.AvailableServices.ShouldNotContain(s => s.ServiceId == unpricedService.Id);
    }

    /// <summary>
    /// 12. التحقق من أن السعر المعروض في الكتالوج مستمد تماماً من كيان ServicePrice الموثوق في الخادم.
    /// </summary>
    [Fact]
    public async Task CustomerCatalog_Price_Comes_From_Backend_ServicePrice()
    {
        // Arrange: ضبط سعر محدد في الخادم
        var item = new LaundryItemType(Guid.NewGuid(), _tenantA, "سجادة", "CARPET", isActive: true);
        var service = new LaundryService(Guid.NewGuid(), _tenantA, "غسيل عميق", "DEEP_WASH", isActive: true);
        const decimal authoritativePrice = 85.75m;
        var price = new ServicePrice(Guid.NewGuid(), _tenantA, item.Id, service.Id, authoritativePrice, isActive: true);

        _itemTypesList.Add(item);
        _servicesList.Add(service);
        _pricesList.Add(price);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert
        var serviceDto = catalog.Items
            .First(i => i.ItemTypeId == item.Id)
            .AvailableServices
            .First(s => s.ServiceId == service.Id);

        serviceDto.Price.ShouldBe(authoritativePrice);
    }

    /// <summary>
    /// 13. التحقق من أن الكتالوج لا يسرب الكيانات المحذوفة ناعماً (Soft Deleted).
    /// </summary>
    [Fact]
    public async Task CustomerCatalog_Does_Not_Expose_Deleted_Catalog_Data()
    {
        // Arrange: صنف محذوف ناعماً
        var itemDeleted = new LaundryItemType(Guid.NewGuid(), _tenantA, "صنف محذوف", "DELETED_ITEM", isActive: true);
        typeof(LaundryItemType).GetProperty("IsDeleted")?.SetValue(itemDeleted, true);

        var service = new LaundryService(Guid.NewGuid(), _tenantA, "كي", "IRON", isActive: true);
        var price = new ServicePrice(Guid.NewGuid(), _tenantA, itemDeleted.Id, service.Id, 10.0m, isActive: true);

        // في بيئة LINQ الحقيقية يتم ترشيح IsDeleted عبر ABP DataFilter، وهنا نحاكي ذلك بتوفير القائمة المفلترة
        _itemTypesList.Add(itemDeleted);
        _servicesList.Add(service);
        _pricesList.Add(price);

        // Act: استعلام العناصر غير المحذوفة
        var catalog = await _service.GetLaundryCatalogAsync(_laundryAId);

        // Assert: الصنف المحذوف لا يظهر
        catalog.Items.ShouldNotContain(i => i.ItemTypeName == "صنف محذوف");
    }

    /// <summary>
    /// 14. التحقق من أن خدمة CustomerCatalogAppService محمية برمز المصادقة [Authorize].
    /// </summary>
    [Fact]
    public void CustomerCatalog_Requires_Authentication()
    {
        // Assert: التأكد أن فئة الخدمة تحمل السمة [Authorize]
        var authorizeAttribute = typeof(CustomerCatalogAppService).GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.ShouldNotBeNull();
    }

    /// <summary>
    /// 15. التحقق من رفض قيم خط العرض غير القانونية (خارج النطاق -90 إلى 90).
    /// </summary>
    [Fact]
    public async Task Invalid_Latitude_Is_Rejected()
    {
        var input = new GetNearbyLaundriesInput
        {
            Latitude = 95.0, // غير صالح
            Longitude = 46.0,
            MaxDistanceKm = 10.0
        };

        // Act & Assert
        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _service.GetNearbyLaundriesAsync(input);
        });
    }

    /// <summary>
    /// 16. التحقق من رفض قيم خط الطول غير القانونية (خارج النطاق -180 إلى 180).
    /// </summary>
    [Fact]
    public async Task Invalid_Longitude_Is_Rejected()
    {
        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.0,
            Longitude = 190.0, // غير صالح
            MaxDistanceKm = 10.0
        };

        // Act & Assert
        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _service.GetNearbyLaundriesAsync(input);
        });
    }

    /// <summary>
    /// 17. التحقق من أن GetAvailablePickupSlotsAsync يسترجع فقط الفترات النشطة من نوع Pickup لليوم المطابق.
    /// </summary>
    [Fact]
    public async Task GetAvailablePickupSlots_Returns_Only_Active_Pickup_Slots_On_Matching_Day()
    {
        // Arrange: إضافة فترات زمنية للمغسلة
        var laundry = _laundriesList[0];
        var targetDate = new DateOnly(2026, 9, 7); // Monday
        var dayOfWeek = targetDate.DayOfWeek;

        // ساعات عمل مفتوحة ليوم الاثنين
        laundry.SetWorkingHour(dayOfWeek, isOpen: true, openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(22, 0));

        // فترة استلام نشطة
        var activePickupSlot = laundry.AddTimeSlot(Guid.NewGuid(), dayOfWeek, SlotType.Pickup, new TimeOnly(9, 0), new TimeOnly(12, 0), isActive: true);
        // فترة توصيل (لا يجب أن تظهر في pickup slots)
        laundry.AddTimeSlot(Guid.NewGuid(), dayOfWeek, SlotType.Delivery, new TimeOnly(14, 0), new TimeOnly(17, 0), isActive: true);
        // فترة استلام معطلة
        laundry.AddTimeSlot(Guid.NewGuid(), dayOfWeek, SlotType.Pickup, new TimeOnly(18, 0), new TimeOnly(21, 0), isActive: false);

        // Act
        var slots = await _service.GetAvailablePickupSlotsAsync(_laundryAId, targetDate);

        // Assert
        slots.ShouldNotBeNull();
        slots.Count.ShouldBe(1);
        slots[0].Id.ShouldBe(activePickupSlot.Id);
        slots[0].SlotType.ShouldBe(SlotType.Pickup);
        slots[0].IsActive.ShouldBeTrue();
    }

    /// <summary>
    /// 18. التحقق من أن GetAvailablePickupSlotsAsync يعيد قائمة فارغة عندما تكون المغسلة مغلقة في هذا اليوم.
    /// </summary>
    [Fact]
    public async Task GetAvailablePickupSlots_Returns_Empty_When_Laundry_Is_Closed_On_Day()
    {
        // Arrange: إغلاق المغسلة في يوم الجمعة
        var laundry = _laundriesList[0];
        var fridayDate = new DateOnly(2026, 9, 11); // Friday
        laundry.SetWorkingHour(DayOfWeek.Friday, isOpen: false);

        // Act
        var slots = await _service.GetAvailablePickupSlotsAsync(_laundryAId, fridayDate);

        // Assert
        slots.ShouldBeEmpty();
    }

    /// <summary>
    /// 19. AvailableSlots_Rejects_Past_Date: التحقق من رفض تاريخ الاستلام إذا كان في الماضي ومطابقة كود الخطأ.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Rejects_Past_Date()
    {
        // Arrange: تاريخ الأمس بالنسبة للتوقيت الحالي (2026-09-07)
        _clock.Now.Returns(new DateTime(2026, 9, 7, 10, 0, 0));
        var pastDate = new DateOnly(2026, 9, 6);

        // Act & Assert
        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _service.GetAvailablePickupSlotsAsync(_laundryAId, pastDate);
        });
        ex.Code.ShouldBe(laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupDateInPast);
    }

    /// <summary>
    /// 20. AvailableSlots_Excludes_Already_Ended_Slot_For_Today: استبعاد الفترات المنتهية لليوم الحالي.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Excludes_Already_Ended_Slot_For_Today()
    {
        // Arrange: الساعة الآن 14:00 في تاريخ اليوم الاثنين 2026-09-07
        _clock.Now.Returns(new DateTime(2026, 9, 7, 14, 0, 0));
        var today = new DateOnly(2026, 9, 7);

        var laundry = _laundriesList[0];
        laundry.SetWorkingHour(DayOfWeek.Monday, isOpen: true, openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(22, 0));

        // فترة انتهت (09:00 - 12:00)
        laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(9, 0), new TimeOnly(12, 0), isActive: true);
        // فترة قادمة (15:00 - 18:00)
        var futureSlot = laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(15, 0), new TimeOnly(18, 0), isActive: true);

        // Act
        var slots = await _service.GetAvailablePickupSlotsAsync(_laundryAId, today);

        // Assert: يجب استبعاد الفترة المنتهية وتضمين الفترة القادمة فقط
        slots.ShouldNotContain(s => s.EndTime <= new TimeOnly(14, 0));
        slots.ShouldContain(s => s.Id == futureSlot.Id);
    }

    /// <summary>
    /// 21. AvailableSlots_Includes_Future_Slot_For_Today: تضمين الفترات المستقبلية لليوم الحالي التي لم ينتهِ وقتها بعد.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Includes_Future_Slot_For_Today()
    {
        // Arrange: الساعة الآن 10:00 صباحاً في تاريخ اليوم الاثنين 2026-09-07
        _clock.Now.Returns(new DateTime(2026, 9, 7, 10, 0, 0));
        var today = new DateOnly(2026, 9, 7);

        var laundry = _laundriesList[0];
        laundry.SetWorkingHour(DayOfWeek.Monday, isOpen: true, openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(22, 0));

        // فترة مستقبلية اليوم (14:00 - 17:00)
        var futureSlot = laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(14, 0), new TimeOnly(17, 0), isActive: true);

        // Act
        var slots = await _service.GetAvailablePickupSlotsAsync(_laundryAId, today);

        // Assert
        slots.ShouldContain(s => s.Id == futureSlot.Id);
    }

    /// <summary>
    /// 22. AvailableSlots_Excludes_Slot_Outside_Working_Hours: استبعاد الفترات التي تقع خارج ساعات العمل الرسمية.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Excludes_Slot_Outside_Working_Hours()
    {
        // Arrange: ساعات العمل من 08:00 إلى 18:00
        _clock.Now.Returns(new DateTime(2026, 9, 7, 8, 0, 0));
        var nextMonday = new DateOnly(2026, 9, 14);

        var laundry = _laundriesList[0];
        laundry.SetWorkingHour(DayOfWeek.Monday, isOpen: true, openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(18, 0));

        // فترة داخل ساعات العمل (10:00 - 13:00)
        var validSlot = laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(10, 0), new TimeOnly(13, 0), isActive: true);
        // فترة تتجاوز وقت الإغلاق (19:00 - 21:00) تُضاف مباشرة للمجموعة لمحاكاة بيانات سابقة خارج ساعات العمل
        var outsideSlot = new LaundryTimeSlot(Guid.NewGuid(), laundry.Id, DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(19, 0), new TimeOnly(21, 0), isActive: true);
        laundry.TimeSlots.Add(outsideSlot);

        // Act
        var slots = await _service.GetAvailablePickupSlotsAsync(_laundryAId, nextMonday);

        // Assert: الفترة الخارجة عن أوقات العمل تُستبعد
        slots.ShouldContain(s => s.Id == validSlot.Id);
        slots.ShouldNotContain(s => s.Id == outsideSlot.Id);
    }

    /// <summary>
    /// 23. AvailableSlots_Returns_Empty_Or_Rejects_When_Laundry_Is_Inactive: رفض الاستعلام إذا كانت المغسلة معطلة.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Returns_Empty_Or_Rejects_When_Laundry_Is_Inactive()
    {
        // Arrange: تعطيل المغسلة
        var laundry = _laundriesList[0];
        laundry.SetActive(false);
        var futureDate = new DateOnly(2026, 9, 14);

        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _service.GetAvailablePickupSlotsAsync(_laundryAId, futureDate);
        });
    }

    /// <summary>
    /// 24. AvailableSlots_Returns_Empty_Or_Rejects_When_Laundry_Not_Accepting_Orders: رفض الاستعلام إذا كانت المغسلة لا تقبل طلبات.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Returns_Empty_Or_Rejects_When_Laundry_Not_Accepting_Orders()
    {
        // Arrange: المغسلة لا تقبل طلبات
        var laundry = _laundriesList[0];
        laundry.SetAcceptingOrders(false);
        var futureDate = new DateOnly(2026, 9, 14);

        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _service.GetAvailablePickupSlotsAsync(_laundryAId, futureDate);
        });
    }

    /// <summary>
    /// 25. AvailableSlots_Uses_Same_Business_Eligibility_As_Order_Pickup_Validation:
    /// التحقق التام من مطابقة معايير الأهلية بين استعراض الفترات واعتمادها عند إنشاء الطلب عبر Laundry.ValidateAndGetPickupSlot.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_Uses_Same_Business_Eligibility_As_Order_Pickup_Validation()
    {
        // Arrange: إعداد عدة فترات (منها الصحيح، المنتهي، خارج ساعات العمل، غير النشط، ونوع التوصيل)
        var currentDateTime = new DateTime(2026, 9, 7, 11, 30, 0); // Monday 11:30
        _clock.Now.Returns(currentDateTime);
        var today = new DateOnly(2026, 9, 7);

        var laundry = _laundriesList[0];
        laundry.SetWorkingHour(DayOfWeek.Monday, isOpen: true, openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(20, 0));

        // 1. فترة منتهية لليوم (08:00 - 11:00)
        laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(8, 0), new TimeOnly(11, 0), isActive: true);
        // 2. فترة صالحة حالية ومستقبلية لليوم (12:00 - 15:00)
        var validSlot1 = laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(12, 0), new TimeOnly(15, 0), isActive: true);
        // 3. فترة صالحة لاحقة لليوم (16:00 - 19:00)
        var validSlot2 = laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(16, 0), new TimeOnly(19, 0), isActive: true);
        // 4. فترة خارج ساعات العمل (20:30 - 22:00)
        var outsideSlot = new LaundryTimeSlot(Guid.NewGuid(), laundry.Id, DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(20, 30), new TimeOnly(22, 0), isActive: true);
        laundry.TimeSlots.Add(outsideSlot);
        // 5. فترة معطلة
        laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Pickup, new TimeOnly(13, 0), new TimeOnly(14, 0), isActive: false);
        // 6. فترة توصيل
        laundry.AddTimeSlot(Guid.NewGuid(), DayOfWeek.Monday, SlotType.Delivery, new TimeOnly(15, 0), new TimeOnly(17, 0), isActive: true);

        // Act: جلب الفترات المتاحة عبر CustomerCatalogAppService
        var availableSlots = await _service.GetAvailablePickupSlotsAsync(_laundryAId, today);

        // Assert:
        availableSlots.ShouldNotBeEmpty();
        availableSlots.Count.ShouldBe(2);

        // كل فترة أعادها GetAvailablePickupSlotsAsync يجب أن تقبلها دالة Domain الأساسية Laundry.ValidateAndGetPickupSlot دون أي استثناء
        foreach (var slotDto in availableSlots)
        {
            var validatedSlot = laundry.ValidateAndGetPickupSlot(slotDto.Id, today, currentDateTime);
            validatedSlot.ShouldNotBeNull();
            validatedSlot.Id.ShouldBe(slotDto.Id);
        }
    }

    /// <summary>
    /// 26. NearbyLaundry_Uses_Laundry_MinimumOrderAmount_As_Authoritative_Value:
    /// التحقق من أن القيمة المعادة في LaundryNearbyListDto.MinimumOrderAmount مستمدة حصرياً من Laundry.MinimumOrderAmount (5000)
    /// ولا يتم استخدام أو استبدالها بـ CoverageArea.MinimumOrderAmount (1000).
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_Uses_Laundry_MinimumOrderAmount_As_Authoritative_Value()
    {
        // Arrange
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة القرار المعماري",
            "0555555555",
            24.7136,
            46.6753,
            new CoverageArea(24.7136, 46.6753, 15.0, 1000.0m),
            deliveryFee: 20.0m,
            minimumOrderAmount: 5000.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 10.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert
        results.Count.ShouldBe(1);
        results[0].MinimumOrderAmount.ShouldBe(5000.0m);
    }

    /// <summary>
    /// 27. CustomerCatalog_Uses_Laundry_MinimumOrderAmount_As_Authoritative_Value:
    /// التحقق من أن CustomerCatalogDto.MinimumOrderAmount يطابق Laundry.MinimumOrderAmount
    /// وأن قيمة CoverageArea.MinimumOrderAmount لا تطغى عليه.
    /// </summary>
    [Fact]
    public async Task CustomerCatalog_Uses_Laundry_MinimumOrderAmount_As_Authoritative_Value()
    {
        // Arrange
        var laundryId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            tenantId,
            "مغسلة الكتالوج المعتمد",
            "0566666666",
            24.7136,
            46.6753,
            new CoverageArea(24.7136, 46.6753, 10.0, 50.0m), // CoverageArea min order: 50
            deliveryFee: 10.0m,
            minimumOrderAmount: 200.0m, // Laundry min order: 200
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        // Act
        var catalog = await _service.GetLaundryCatalogAsync(laundryId);

        // Assert
        catalog.ShouldNotBeNull();
        catalog.MinimumOrderAmount.ShouldBe(200.0m);
    }

    /// <summary>
    /// 28. NearbyLaundry_DistanceKm_Uses_Physical_Laundry_Location:
    /// التحقق من أن DistanceKm تحسب المسافة من موقع العميل إلى الموقع الفعلي للمغسلة
    /// وليس إلى مركز نطاق التغطية.
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_DistanceKm_Uses_Physical_Laundry_Location()
    {
        // Arrange:
        // موقع المغسلة الفعلي: (24.7100, 46.6700)
        // مركز نطاق التغطية: (24.7500, 46.7000)
        // موقع العميل: (24.7200, 46.6800)
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة المسافة الفعلية",
            "0577777777",
            24.7100,
            46.6700,
            new CoverageArea(24.7500, 46.7000, 15.0, 30.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 30.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var customerLat = 24.7200;
        var customerLon = 46.6800;

        var expectedPhysicalDistance = HaversineDistanceCalculator.CalculateDistanceKm(
            customerLat, customerLon, 24.7100, 46.6700);
        var expectedCoverageDistance = HaversineDistanceCalculator.CalculateDistanceKm(
            customerLat, customerLon, 24.7500, 46.7000);

        // التأكد من اختلاف المسافتين في السيناريو
        Math.Abs(expectedPhysicalDistance - expectedCoverageDistance).ShouldBeGreaterThan(1.0);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = customerLat,
            Longitude = customerLon,
            MaxDistanceKm = 20.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert: DistanceKm يمثل المسافة الفعلية وليس مسافة مركز التغطية
        results.Count.ShouldBe(1);
        results[0].DistanceKm.ShouldBe(Math.Round(expectedPhysicalDistance, 2));
        results[0].DistanceKm.ShouldNotBe(Math.Round(expectedCoverageDistance, 2));
    }

    /// <summary>
    /// 29. NearbyLaundry_CoverageEligibility_Uses_CoverageArea_Center:
    /// التحقق من أن أهلية التغطية (<= DeliveryRadiusKm) تُحدد بمسافة العميل عن مركز التغطية فقط
    /// حتى لو كان الموقع الفعلي للمغسلة بعيداً جداً.
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_CoverageEligibility_Uses_CoverageArea_Center()
    {
        // Arrange:
        // موقع المغسلة الفعلي بعيد جداً (24.2000, 46.2000) ~70 كم عن العميل
        // مركز نطاق التغطية قريب جداً (24.7136, 46.6753) ونصف قطره 10 كم
        // موقع العميل: (24.7140, 46.6760) ~0.1 كم من مركز التغطية
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة نطاق مركزي",
            "0588888888",
            24.2000,
            46.2000,
            new CoverageArea(24.7136, 46.6753, 10.0, 30.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 30.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7140,
            Longitude = 46.6760,
            MaxDistanceKm = 100.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert: المغسلة مؤهلة للظهور لأن العميل داخل نطاق مركز التغطية
        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(laundryId);
        // والمسافة المعروضة هي المسافة الفعلية البعيدة
        results[0].DistanceKm.ShouldBeGreaterThan(50.0);
    }

    /// <summary>
    /// 30. NearbyLaundry_Does_Not_Treat_ZeroCoordinates_As_Missing:
    /// التحقق من أن إحداثيات (0,0) لا تُعامل كقيمة مفقودة ولا يتم تطبيق أي Fallback لمركز التغطية.
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_Does_Not_Treat_ZeroCoordinates_As_Missing()
    {
        // Arrange:
        // موقع المغسلة الفعلي: (0.0, 0.0) وهو مسموح به في النطاق الجغرافي القانوني (-90..90, -180..180)
        // مركز التغطية في الرياض: (24.7136, 46.6753)
        // العميل في الرياض عند مركز التغطية: (24.7136, 46.6753)
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة الصفر المئوي",
            "0599999999",
            0.0,
            0.0,
            new CoverageArea(24.7136, 46.6753, 10.0, 25.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 25.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 10000.0 // مسافة بحث كافية
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert:
        results.Count.ShouldBe(1);
        // لو كان هناك fallback إلى مركز التغطية لكانت المسافة 0 كم
        // وبما أنه لا يوجد fallback فالمسافة هي المسافة الحقيقية إلى (0,0) وهي آلاف الكيلومترات (~5388 كم)
        results[0].DistanceKm.ShouldBeGreaterThan(5000.0);
    }

    /// <summary>
    /// 31. NearbyLaundries_Excludes_Inactive_Laundry_As_Tenant_Suspension_Boundary:
    /// التحقق من حدود عقد العميل: المغسلة المعطلة (Laundry.IsActive == false) تُستبعد تماماً من اكتشاف العملاء،
    /// وهو ما يمثل مصدر الإتاحة للعميل عند تعليق المستأجر من قبل HostManagement.
    /// </summary>
    [Fact]
    public async Task NearbyLaundries_Excludes_Inactive_Laundry_As_Tenant_Suspension_Boundary()
    {
        // Arrange: مغسلة غير نشطة تمثل مستأجراً موقوفاً
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة مستأجر موقوف",
            "0500000000",
            24.7136,
            46.6753,
            new CoverageArea(24.7136, 46.6753, 10.0, 20.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 20.0m,
            estimatedProcessingHours: 24,
            isActive: false, // موقوف
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 10.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert
        results.ShouldBeEmpty();
    }

    /// <summary>
    /// 32. NearbyLaundry_Outside_MaxPhysicalDistance_Is_Excluded_Even_When_CoverageCenter_Is_Close:
    /// التحقق من استبعاد المغسلة إذا تجاوزت المسافة الفعلية لها أقصى مسافة بحث للعميل (MaxDistanceKm)،
    /// حتى لو كان مركز نطاق التغطية قريباً جداً من العميل والتغطية صالحة.
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_Outside_MaxPhysicalDistance_Is_Excluded_Even_When_CoverageCenter_Is_Close()
    {
        // Arrange:
        // العميل عند (24.7136, 46.6753)
        // مركز نطاق التغطية مطابق للعميل (24.7136, 46.6753) ونصف قطره 15 كم (التغطية صالحة تماماً: 0 كم <= 15 كم)
        // الموقع الفعلي للمغسلة بعيد (24.9500, 46.9500) على بعد ~38 كم
        // مسافة بحث العميل القصوى MaxDistanceKm = 20 كم
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة مركزها قريب وموقعها بعيد",
            "0511122233",
            24.9500,
            46.9500,
            new CoverageArea(24.7136, 46.6753, 15.0, 30.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 30.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 20.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert: تُستبعد المغسلة لأن المسافة الفعلية تتجاوز MaxDistanceKm
        results.ShouldBeEmpty();
    }

    /// <summary>
    /// 33. NearbyLaundry_Inside_MaxPhysicalDistance_Is_Included_When_CoverageIsValid:
    /// التحقق من تضمين المغسلة عندما تكون التغطية صالحة والمسافة الفعلية ضمن أقصى مسافة بحث للعميل.
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_Inside_MaxPhysicalDistance_Is_Included_When_CoverageIsValid()
    {
        // Arrange:
        // العميل عند (24.7136, 46.6753)
        // مركز التغطية مطابق للعميل (24.7136, 46.6753) بنصف قطر 10 كم (التغطية صالحة)
        // موقع المغسلة الفعلي قريب (24.7200, 46.6800) ~1 كم
        // مسافة البحث القصوى 20 كم
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة مطابقة للشروط",
            "0511122244",
            24.7200,
            46.6800,
            new CoverageArea(24.7136, 46.6753, 10.0, 30.0m),
            deliveryFee: 12.0m,
            minimumOrderAmount: 30.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 20.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert: تظهر المغسلة بنجاح
        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(laundryId);
    }

    /// <summary>
    /// 34. NearbyLaundry_MaxDistance_Does_Not_Use_CoverageCenterDistance:
    /// إثبات أن MaxDistanceKm لا يُقاس بمسافة مركز التغطية:
    /// إذا كان مركز التغطية يبعد ~1 كم، ولكن الموقع الفعلي للمغسلة يبعد ~30 كم، وبحث العميل MaxDistanceKm = 10 كم:
    /// النتيجة المتوقعة هي الاستبعاد التام، لأن MaxDistanceKm يُقاس حصرياً بالمسافة الفعلية للمغسلة.
    /// </summary>
    [Fact]
    public async Task NearbyLaundry_MaxDistance_Does_Not_Use_CoverageCenterDistance()
    {
        // Arrange:
        // العميل عند (24.7136, 46.6753)
        // مركز نطاق التغطية يبعد ~1 كم عند (24.7200, 46.6800)، بنصف قطر 20 كم (التغطية صالحة)
        // موقع المغسلة الفعلي يبعد ~30 كم عند (24.9000, 46.8500)
        // أقصى مسافة بحث MaxDistanceKm = 10 كم
        var laundryId = Guid.NewGuid();
        var laundry = new Laundry(
            laundryId,
            Guid.NewGuid(),
            "مغسلة اختبار فصل قياس أقصى مسافة",
            "0511122255",
            24.9000,
            46.8500,
            new CoverageArea(24.7200, 46.6800, 20.0, 30.0m),
            deliveryFee: 15.0m,
            minimumOrderAmount: 30.0m,
            estimatedProcessingHours: 24,
            isActive: true,
            acceptingOrders: true
        );
        _laundriesList.Clear();
        _laundriesList.Add(laundry);

        var input = new GetNearbyLaundriesInput
        {
            Latitude = 24.7136,
            Longitude = 46.6753,
            MaxDistanceKm = 10.0
        };

        // Act
        var results = await _service.GetNearbyLaundriesAsync(input);

        // Assert: يجب أن تُستبعد لأن المسافة الفعلية 30 كم > 10 كم، حتى لو كانت مسافة مركز التغطية 1 كم <= 10 كم
        results.ShouldBeEmpty();
    }
}
