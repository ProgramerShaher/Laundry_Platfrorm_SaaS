using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace laundry_SaaS.Catalog;

public abstract class LaundryCatalogAppServiceTests<TStartupModule> : laundry_SaaSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ILaundryCatalogAppService _catalogAppService;
    private readonly IRepository<LaundryItemType, Guid> _itemTypeRepository;
    private readonly IRepository<LaundryService, Guid> _serviceRepository;
    private readonly IRepository<ServicePrice, Guid> _servicePriceRepository;
    private readonly ICurrentTenant _currentTenant;

    public LaundryCatalogAppServiceTests()
    {
        _catalogAppService = GetRequiredService<ILaundryCatalogAppService>();
        _itemTypeRepository = GetRequiredService<IRepository<LaundryItemType, Guid>>();
        _serviceRepository = GetRequiredService<IRepository<LaundryService, Guid>>();
        _servicePriceRepository = GetRequiredService<IRepository<ServicePrice, Guid>>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Operations_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.GetItemTypesAsync();
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.GetServicesAsync();
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.GetServicePricesAsync();
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.CreateItemTypeAsync(new CreateLaundryItemTypeInput
                {
                    Name = "Thobe",
                    Code = "THB"
                });
            });
        }
    }

    [Fact]
    public async Task ItemType_CRUD_And_Unique_Code_Validation()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            // 1. Create
            var created = await _catalogAppService.CreateItemTypeAsync(new CreateLaundryItemTypeInput
            {
                Name = "Men Thobe",
                Code = "THB_MEN",
                Description = "Traditional Men Thobe",
                DisplayOrder = 1,
                IsActive = true
            });

            created.ShouldNotBeNull();
            created.Name.ShouldBe("Men Thobe");
            created.Code.ShouldBe("THB_MEN");
            created.IsActive.ShouldBeTrue();

            // 2. Duplicate code check
            var dupEx = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.CreateItemTypeAsync(new CreateLaundryItemTypeInput
                {
                    Name = "Another Thobe",
                    Code = "THB_MEN"
                });
            });
            dupEx.Code.ShouldBe(laundry_SaaSDomainErrorCodes.Prefix + ":DuplicateItemCode");

            // 3. Get
            var fetched = await _catalogAppService.GetItemTypeAsync(created.Id);
            fetched.Id.ShouldBe(created.Id);
            fetched.Name.ShouldBe("Men Thobe");

            // 4. Update
            var updated = await _catalogAppService.UpdateItemTypeAsync(created.Id, new UpdateLaundryItemTypeInput
            {
                Name = "Men Premium Thobe",
                DisplayOrder = 2,
                IsActive = true
            });
            updated.Name.ShouldBe("Men Premium Thobe");

            // 5. Deactivate
            await _catalogAppService.SetItemTypeActiveAsync(created.Id, false);
            var deactivated = await _catalogAppService.GetItemTypeAsync(created.Id);
            deactivated.IsActive.ShouldBeFalse();

            // 6. Delete
            await _catalogAppService.DeleteItemTypeAsync(created.Id);
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _catalogAppService.GetItemTypeAsync(created.Id);
            });
        }
    }

    [Fact]
    public async Task Service_CRUD_And_Unique_Code_Validation()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            // 1. Create
            var created = await _catalogAppService.CreateServiceAsync(new CreateLaundryServiceInput
            {
                Name = "Wash and Iron",
                Code = "WASH_IRON",
                Description = "Complete washing and steam ironing",
                DisplayOrder = 1,
                IsActive = true
            });

            created.ShouldNotBeNull();
            created.Name.ShouldBe("Wash and Iron");
            created.Code.ShouldBe("WASH_IRON");
            created.IsActive.ShouldBeTrue();

            // 2. Duplicate code check
            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.CreateServiceAsync(new CreateLaundryServiceInput
                {
                    Name = "Wash and Iron Duplicate",
                    Code = "WASH_IRON"
                });
            });

            // 3. Get
            var fetched = await _catalogAppService.GetServiceAsync(created.Id);
            fetched.Name.ShouldBe("Wash and Iron");

            // 4. Update
            var updated = await _catalogAppService.UpdateServiceAsync(created.Id, new UpdateLaundryServiceInput
            {
                Name = "Wash & Steam Iron",
                DisplayOrder = 2,
                IsActive = true
            });
            updated.Name.ShouldBe("Wash & Steam Iron");

            // 5. Set Active
            await _catalogAppService.SetServiceActiveAsync(created.Id, false);
            var deactivated = await _catalogAppService.GetServiceAsync(created.Id);
            deactivated.IsActive.ShouldBeFalse();

            // 6. Delete
            await _catalogAppService.DeleteServiceAsync(created.Id);
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _catalogAppService.GetServiceAsync(created.Id);
            });
        }
    }

    [Fact]
    public async Task SetServicePrice_Upsert_And_Validations()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            var item = await _catalogAppService.CreateItemTypeAsync(new CreateLaundryItemTypeInput
            {
                Name = "Shirt",
                Code = "SHRT"
            });

            var service = await _catalogAppService.CreateServiceAsync(new CreateLaundryServiceInput
            {
                Name = "Iron Only",
                Code = "IRON"
            });

            // 1. Negative price validation
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _catalogAppService.SetServicePriceAsync(new SetServicePriceInput
                {
                    LaundryItemTypeId = item.Id,
                    LaundryServiceId = service.Id,
                    Price = -5.0m
                });
            });

            // 2. Initial Set (Insert)
            var priceDto = await _catalogAppService.SetServicePriceAsync(new SetServicePriceInput
            {
                LaundryItemTypeId = item.Id,
                LaundryServiceId = service.Id,
                Price = 8.50m
            });

            priceDto.ShouldNotBeNull();
            priceDto.Price.ShouldBe(8.50m);
            priceDto.ItemTypeName.ShouldBe("Shirt");
            priceDto.ServiceName.ShouldBe("Iron Only");

            // 3. Upsert (Update existing price)
            var updatedPriceDto = await _catalogAppService.SetServicePriceAsync(new SetServicePriceInput
            {
                LaundryItemTypeId = item.Id,
                LaundryServiceId = service.Id,
                Price = 10.00m
            });

            updatedPriceDto.Id.ShouldBe(priceDto.Id);
            updatedPriceDto.Price.ShouldBe(10.00m);

            // 4. Get List
            var prices = await _catalogAppService.GetServicePricesAsync();
            prices.Count.ShouldBe(1);
            prices[0].Price.ShouldBe(10.00m);

            // 5. Prevent deleting ItemType while active price exists
            var delEx = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _catalogAppService.DeleteItemTypeAsync(item.Id);
            });
            delEx.Message.ShouldContain("active prices");

            // 6. Delete price
            await _catalogAppService.DeleteServicePriceAsync(priceDto.Id);
            var emptyPrices = await _catalogAppService.GetServicePricesAsync();
            emptyPrices.Count.ShouldBe(0);

            // 7. Now ItemType can be deleted
            await _catalogAppService.DeleteItemTypeAsync(item.Id);
        }
    }

    [Fact]
    public async Task GetItemTypeLookupAsync_Should_Return_Only_Active_Items()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            await _catalogAppService.CreateItemTypeAsync(new CreateLaundryItemTypeInput
            {
                Name = "Active Item",
                Code = "ACT",
                IsActive = true
            });

            var inactive = await _catalogAppService.CreateItemTypeAsync(new CreateLaundryItemTypeInput
            {
                Name = "Inactive Item",
                Code = "INACT",
                IsActive = false
            });

            var lookup = await _catalogAppService.GetItemTypeLookupAsync();
            lookup.Count.ShouldBe(1);
            lookup[0].Code.ShouldBe("ACT");
        }
    }
}
