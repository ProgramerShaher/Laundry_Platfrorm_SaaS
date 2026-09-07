using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace laundry_SaaS.Laundries;

public class LaundryProfileAppServiceTests : laundry_SaaSApplicationTestBase<laundry_SaaSApplicationTestModule>
{
    private readonly ILaundryProfileAppService _profileAppService;
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly ICurrentTenant _currentTenant;

    public LaundryProfileAppServiceTests()
    {
        _profileAppService = GetRequiredService<ILaundryProfileAppService>();
        _laundryRepository = GetRequiredService<IRepository<Laundry, Guid>>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task GetProfileAsync_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _profileAppService.GetProfileAsync();
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");
        }
    }

    [Fact]
    public async Task SetCoverageAreaAsync_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _profileAppService.SetCoverageAreaAsync(new SetCoverageAreaInput
                {
                    CenterLatitude = 24.7136,
                    CenterLongitude = 46.6753,
                    RadiusKm = 10,
                    MinimumOrderAmount = 25
                });
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");
        }
    }

    [Fact]
    public async Task SetWorkingHourAsync_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _profileAppService.SetWorkingHourAsync(new SetLaundryWorkingHourInput
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    IsOpen = true,
                    OpenTime = new TimeOnly(8, 0),
                    CloseTime = new TimeOnly(20, 0)
                });
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");
        }
    }

    [Fact]
    public async Task GetWorkingHoursAsync_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _profileAppService.GetWorkingHoursAsync();
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");
        }
    }

    [Fact]
    public async Task UpdateProfileAsync_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _profileAppService.UpdateProfileAsync(new UpdateLaundryProfileInput
                {
                    Name = "Test Laundry",
                    PhoneNumber = "0501234567"
                });
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");
        }
    }

    [Fact]
    public async Task GetProfileAsync_When_Laundry_Not_Found_Should_Throw_EntityNotFoundException()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _profileAppService.GetProfileAsync();
            });
        }
    }

    [Fact]
    public async Task Operations_With_Valid_Tenant_And_Laundry_Should_Succeed()
    {
        var tenantId = Guid.NewGuid();
        var laundryId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantId))
            {
                var area = new CoverageArea(24.7136, 46.6753, 10.0, 20.0m);
                var laundry = new Laundry(
                    laundryId,
                    tenantId,
                    "Premium Clean Laundry",
                    "0501112233",
                    24.7136,
                    46.6753,
                    area,
                    deliveryFee: 15.0m,
                    minimumOrderAmount: 30.0m);

                await _laundryRepository.InsertAsync(laundry, autoSave: true);
            }
        });

        using (_currentTenant.Change(tenantId))
        {
            // 1. Get Profile
            var profile = await _profileAppService.GetProfileAsync();
            profile.ShouldNotBeNull();
            profile.Id.ShouldBe(laundryId);
            profile.Name.ShouldBe("Premium Clean Laundry");
            profile.PhoneNumber.ShouldBe("0501112233");
            profile.DeliveryFee.ShouldBe(15.0m);
            profile.CoverageArea.ShouldNotBeNull();
            profile.CoverageArea!.RadiusKm.ShouldBe(10.0);

            // 2. Set Working Hour
            var workingHour = await _profileAppService.SetWorkingHourAsync(new SetLaundryWorkingHourInput
            {
                DayOfWeek = DayOfWeek.Sunday,
                IsOpen = true,
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(21, 0)
            });
            workingHour.IsOpen.ShouldBeTrue();
            workingHour.DayOfWeek.ShouldBe(DayOfWeek.Sunday);

            // 3. Get Working Hours
            var hours = await _profileAppService.GetWorkingHoursAsync();
            hours.Count.ShouldBeGreaterThanOrEqualTo(1);
            hours.ShouldContain(h => h.DayOfWeek == DayOfWeek.Sunday && h.IsOpen);

            // 4. Set Coverage Area
            await _profileAppService.SetCoverageAreaAsync(new SetCoverageAreaInput
            {
                CenterLatitude = 24.7500,
                CenterLongitude = 46.7000,
                RadiusKm = 15.0,
                MinimumOrderAmount = 40.0m
            });

            // Verify updated coverage area
            var updatedProfile = await _profileAppService.GetProfileAsync();
            updatedProfile.CoverageArea!.RadiusKm.ShouldBe(15.0);
            updatedProfile.CoverageArea.MinimumOrderAmount.ShouldBe(40.0m);

            // 5. Update Profile
            var updatedDetail = await _profileAppService.UpdateProfileAsync(new UpdateLaundryProfileInput
            {
                Name = "Updated Clean Laundry",
                PhoneNumber = "0509998877",
                DeliveryFee = 20.0m,
                MinimumOrderAmount = 50.0m,
                AcceptingOrders = false,
                LogoBlobName = "new_logo.png"
            });
            updatedDetail.Name.ShouldBe("Updated Clean Laundry");
            updatedDetail.PhoneNumber.ShouldBe("0509998877");
            updatedDetail.DeliveryFee.ShouldBe(20.0m);
            updatedDetail.MinimumOrderAmount.ShouldBe(50.0m);
            updatedDetail.AcceptingOrders.ShouldBeFalse();
            updatedDetail.LogoBlobName.ShouldBe("new_logo.png");
        }
    }
}
