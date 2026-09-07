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

public class LaundryTimeSlotAppServiceTests : laundry_SaaSApplicationTestBase<laundry_SaaSApplicationTestModule>
{
    private readonly ILaundryTimeSlotAppService _timeSlotAppService;
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly ICurrentTenant _currentTenant;

    public LaundryTimeSlotAppServiceTests()
    {
        _timeSlotAppService = GetRequiredService<ILaundryTimeSlotAppService>();
        _laundryRepository = GetRequiredService<IRepository<Laundry, Guid>>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Operations_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _timeSlotAppService.GetListAsync();
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _timeSlotAppService.GetAsync(Guid.NewGuid());
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _timeSlotAppService.CreateAsync(new CreateLaundryTimeSlotInput
                {
                    DayOfWeek = DayOfWeek.Monday,
                    SlotType = SlotType.Pickup,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0)
                });
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _timeSlotAppService.SetActiveAsync(Guid.NewGuid(), false);
            });
        }
    }

    [Fact]
    public async Task CreateAsync_When_Day_Is_Closed_Should_Throw_BusinessException()
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
                    "Slots Test Laundry",
                    "0501112233",
                    24.7136,
                    46.6753,
                    area);

                // Closed on Friday
                laundry.SetWorkingHour(DayOfWeek.Friday, isOpen: false);
                await _laundryRepository.InsertAsync(laundry, autoSave: true);
            }
        });

        using (_currentTenant.Change(tenantId))
        {
            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _timeSlotAppService.CreateAsync(new CreateLaundryTimeSlotInput
                {
                    DayOfWeek = DayOfWeek.Friday,
                    SlotType = SlotType.Pickup,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0)
                });
            });
        }
    }

    [Fact]
    public async Task CreateAsync_When_Overlapping_Active_Slot_Should_Throw_BusinessException()
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
                    "Overlap Test Laundry",
                    "0501112233",
                    24.7136,
                    46.6753,
                    area);

                laundry.SetWorkingHour(DayOfWeek.Sunday, isOpen: true, new TimeOnly(8, 0), new TimeOnly(22, 0));
                await _laundryRepository.InsertAsync(laundry, autoSave: true);
            }
        });

        using (_currentTenant.Change(tenantId))
        {
            // First slot: 10:00 - 12:00
            await _timeSlotAppService.CreateAsync(new CreateLaundryTimeSlotInput
            {
                DayOfWeek = DayOfWeek.Sunday,
                SlotType = SlotType.Pickup,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                IsActive = true
            });

            // Second overlapping slot: 11:00 - 13:00 -> should throw
            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _timeSlotAppService.CreateAsync(new CreateLaundryTimeSlotInput
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    SlotType = SlotType.Pickup,
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(13, 0),
                    IsActive = true
                });
            });
        }
    }

    [Fact]
    public async Task TimeSlot_EndToEnd_Lifecycle_Should_Succeed()
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
                    "Lifecycle Test Laundry",
                    "0501112233",
                    24.7136,
                    46.6753,
                    area);

                laundry.SetWorkingHour(DayOfWeek.Monday, isOpen: true, new TimeOnly(8, 0), new TimeOnly(22, 0));
                await _laundryRepository.InsertAsync(laundry, autoSave: true);
            }
        });

        using (_currentTenant.Change(tenantId))
        {
            // 1. Create Pickup Slot
            var createdPickup = await _timeSlotAppService.CreateAsync(new CreateLaundryTimeSlotInput
            {
                DayOfWeek = DayOfWeek.Monday,
                SlotType = SlotType.Pickup,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                IsActive = true
            });

            createdPickup.ShouldNotBeNull();
            createdPickup.DayOfWeek.ShouldBe(DayOfWeek.Monday);
            createdPickup.SlotType.ShouldBe(SlotType.Pickup);
            createdPickup.StartTime.ShouldBe(new TimeOnly(9, 0));
            createdPickup.EndTime.ShouldBe(new TimeOnly(11, 0));
            createdPickup.IsActive.ShouldBeTrue();

            // 2. Create Delivery Slot
            var createdDelivery = await _timeSlotAppService.CreateAsync(new CreateLaundryTimeSlotInput
            {
                DayOfWeek = DayOfWeek.Monday,
                SlotType = SlotType.Delivery,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(16, 0),
                IsActive = true
            });
            createdDelivery.SlotType.ShouldBe(SlotType.Delivery);

            // 3. Get Detail
            var detail = await _timeSlotAppService.GetAsync(createdPickup.Id);
            detail.Id.ShouldBe(createdPickup.Id);
            detail.StartTime.ShouldBe(new TimeOnly(9, 0));

            // 4. Get List with Filter
            var pickupList = await _timeSlotAppService.GetListAsync(DayOfWeek.Monday, SlotType.Pickup);
            pickupList.Count.ShouldBe(1);
            pickupList.ShouldContain(s => s.Id == createdPickup.Id);

            var deliveryList = await _timeSlotAppService.GetListAsync(DayOfWeek.Monday, SlotType.Delivery);
            deliveryList.Count.ShouldBe(1);
            deliveryList.ShouldContain(s => s.Id == createdDelivery.Id);

            // 5. Update Time Slot
            var updated = await _timeSlotAppService.UpdateAsync(createdPickup.Id, new UpdateLaundryTimeSlotInput
            {
                StartTime = new TimeOnly(9, 30),
                EndTime = new TimeOnly(11, 30),
                IsActive = true
            });
            updated.StartTime.ShouldBe(new TimeOnly(9, 30));
            updated.EndTime.ShouldBe(new TimeOnly(11, 30));

            // 6. Set Inactive
            await _timeSlotAppService.SetActiveAsync(createdPickup.Id, false);
            var inactiveDetail = await _timeSlotAppService.GetAsync(createdPickup.Id);
            inactiveDetail.IsActive.ShouldBeFalse();

            // 7. Set Active
            await _timeSlotAppService.SetActiveAsync(createdPickup.Id, true);
            var activeDetail = await _timeSlotAppService.GetAsync(createdPickup.Id);
            activeDetail.IsActive.ShouldBeTrue();
        }
    }
}
