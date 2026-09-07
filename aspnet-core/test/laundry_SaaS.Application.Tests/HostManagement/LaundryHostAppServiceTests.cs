using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;
using laundry_SaaS.Laundries;

namespace laundry_SaaS.HostManagement;

public class LaundryHostAppServiceTests : laundry_SaaSApplicationTestBase<laundry_SaaSApplicationTestModule>
{
    private readonly ILaundryHostAppService _hostAppService;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly IDataFilter _dataFilter;

    public LaundryHostAppServiceTests()
    {
        _hostAppService = GetRequiredService<ILaundryHostAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
        _tenantRepository = GetRequiredService<ITenantRepository>();
        _laundryRepository = GetRequiredService<IRepository<Laundry, Guid>>();
        _dataFilter = GetRequiredService<IDataFilter>();
    }

    [Fact]
    public async Task Operations_Within_Tenant_Context_Should_Throw_BusinessException()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.GetPlatformStatisticsAsync();
            });

            ex.Code.ShouldBe("laundry_SaaS:HostOnly");

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.CreateLaundryTenantAsync(new CreateLaundryTenantInput
                {
                    TenantName = "tenant1",
                    LaundryName = "Laundry 1",
                    AdminEmail = "admin@tenant1.com",
                    AdminPassword = "Password123!",
                    PhoneNumber = "0501112233"
                });
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.GetAsync(Guid.NewGuid());
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.ActivateLaundryTenantAsync(Guid.NewGuid());
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.SuspendLaundryTenantAsync(Guid.NewGuid());
            });
        }
    }

    [Fact]
    public async Task CreateLaundryTenantAsync_When_Duplicate_TenantName_Should_Throw_BusinessException()
    {
        var tenantName = "dup_tenant_" + Guid.NewGuid().ToString("N")[..8];

        using (_currentTenant.Change(null))
        {
            await _hostAppService.CreateLaundryTenantAsync(new CreateLaundryTenantInput
            {
                TenantName = tenantName,
                LaundryName = "Original Laundry",
                AdminEmail = "admin@dup.com",
                AdminPassword = "Password123!",
                PhoneNumber = "0501112233",
                Latitude = 24.7136,
                Longitude = 46.6753
            });

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _hostAppService.CreateLaundryTenantAsync(new CreateLaundryTenantInput
                {
                    TenantName = tenantName,
                    LaundryName = "Duplicate Laundry",
                    AdminEmail = "admin2@dup.com",
                    AdminPassword = "Password123!",
                    PhoneNumber = "0501112244",
                    Latitude = 24.7136,
                    Longitude = 46.6753
                });
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantAlreadyExists");
        }
    }

    [Fact]
    public async Task GetAsync_When_NotFound_Should_Throw_EntityNotFoundException()
    {
        using (_currentTenant.Change(null))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _hostAppService.GetAsync(Guid.NewGuid());
            });
        }
    }

    [Fact]
    public async Task Host_EndToEnd_Lifecycle_Should_Succeed()
    {
        var tenantName = "tenant_" + Guid.NewGuid().ToString("N")[..8];
        var laundryName = "Host Clean Laundry";

        using (_currentTenant.Change(null))
        {
            // 1. Create Laundry Tenant
            var created = await _hostAppService.CreateLaundryTenantAsync(new CreateLaundryTenantInput
            {
                TenantName = tenantName,
                LaundryName = laundryName,
                AdminEmail = "manager@" + tenantName + ".com",
                AdminPassword = "Password123!",
                PhoneNumber = "0509990011",
                Latitude = 24.7136,
                Longitude = 46.6753,
                RadiusKm = 12.0,
                DeliveryFee = 15.0m,
                MinimumOrderAmount = 35.0m,
                EstimatedProcessingHours = 36
            });

            created.ShouldNotBeNull();
            created.TenantName.ShouldBe(tenantName);
            created.Name.ShouldBe(laundryName);
            created.IsActive.ShouldBeTrue();
            created.AcceptingOrders.ShouldBeTrue();
            created.DeliveryFee.ShouldBe(15.0m);
            created.EstimatedProcessingHours.ShouldBe(36);

            // 2. Get Detail
            var detail = await _hostAppService.GetAsync(created.Id);
            detail.ShouldNotBeNull();
            detail.Id.ShouldBe(created.Id);
            detail.TenantId.ShouldBe(created.TenantId);
            detail.TenantName.ShouldBe(tenantName);

            // 3. Get List
            var list = await _hostAppService.GetListAsync(new PagedAndSortedResultRequestDto
            {
                MaxResultCount = 10,
                SkipCount = 0
            });
            list.TotalCount.ShouldBeGreaterThan(0);
            list.Items.ShouldContain(i => i.Id == created.Id && i.TenantName == tenantName);

            // 4. Suspend Laundry
            await _hostAppService.SuspendLaundryTenantAsync(created.Id);
            var suspendedDetail = await _hostAppService.GetAsync(created.Id);
            suspendedDetail.IsActive.ShouldBeFalse();
            suspendedDetail.AcceptingOrders.ShouldBeFalse();

            // 5. Activate Laundry
            await _hostAppService.ActivateLaundryTenantAsync(created.Id);
            var activatedDetail = await _hostAppService.GetAsync(created.Id);
            activatedDetail.IsActive.ShouldBeTrue();
            activatedDetail.AcceptingOrders.ShouldBeTrue();

            // 6. Set Active Status Directly
            await _hostAppService.SetActiveStatusAsync(created.Id, false);
            var statusDetail = await _hostAppService.GetAsync(created.Id);
            statusDetail.IsActive.ShouldBeFalse();

            // 7. Platform Statistics
            var stats = await _hostAppService.GetPlatformStatisticsAsync();
            stats.ShouldNotBeNull();
            stats.TotalLaundriesCount.ShouldBeGreaterThan(0);
        }
    }
}
