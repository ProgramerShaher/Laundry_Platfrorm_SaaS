using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using laundry_SaaS.Drivers;
using laundry_SaaS.PickupDelivery;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Applications;

/// <summary>
/// اختبارات تكاملية شاملة لخدمة إدارة السائقين <see cref="DriverManagementAppService"/>
/// مدعومة بقاعدة بيانات SQLite في الذاكرة عبر EF Core.
/// </summary>
[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class EfCoreDriverManagementAppServiceTests : laundry_SaaSEntityFrameworkCoreTestBase
{
    private readonly IDriverManagementAppService _driverAppService;
    private readonly IRepository<Driver, Guid> _driverRepository;
    private readonly IRepository<PickupTask, Guid> _pickupTaskRepository;
    private readonly IRepository<DeliveryTask, Guid> _deliveryTaskRepository;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public EfCoreDriverManagementAppServiceTests()
    {
        _driverAppService = GetRequiredService<IDriverManagementAppService>();
        _driverRepository = GetRequiredService<IRepository<Driver, Guid>>();
        _pickupTaskRepository = GetRequiredService<IRepository<PickupTask, Guid>>();
        _deliveryTaskRepository = GetRequiredService<IRepository<DeliveryTask, Guid>>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private IDisposable AuthenticateStaff(Guid tenantId, string userName = "staff_user")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.TenantId, tenantId.ToString()),
            new Claim(AbpClaimTypes.Email, $"{userName}@laundry.local")
        };
        return _principalAccessor.Change(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")));
    }

    [Fact]
    public async Task CreateAsync_Should_Create_IdentityUser_And_DriverAggregate_Under_Current_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var input = new CreateDriverProfileInput
            {
                UserName = "driver_saleh",
                Email = "saleh@laundry.local",
                Password = "Password123!",
                Name = "Saleh",
                Surname = "Al-Otaibi",
                PhoneNumber = "0551122334",
                VehicleDetails = "Toyota Hilux 2024 - Plate: 1234 KSA"
            };

            // Act
            var result = await _driverAppService.CreateAsync(input);

            // Assert
            result.ShouldNotBeNull();
            result.UserName.ShouldBe("driver_saleh");
            result.FullName.ShouldBe("Saleh Al-Otaibi");
            result.Email.ShouldBe("saleh@laundry.local");
            result.PhoneNumber.ShouldBe("0551122334");
            result.VehicleDetails.ShouldBe("Toyota Hilux 2024 - Plate: 1234 KSA");
            result.IsActive.ShouldBeTrue();
            result.IsAvailable.ShouldBeTrue();
            result.ActiveTasksCount.ShouldBe(0);

            // Verify in database
            var driverInDb = await _driverRepository.FindAsync(result.Id);
            driverInDb.ShouldNotBeNull();
            driverInDb.TenantId.ShouldBe(tenantId);
            driverInDb.UserId.ShouldBe(result.UserId);

            var userInDb = await _userManager.FindByIdAsync(result.UserId.ToString());
            userInDb.ShouldNotBeNull();
            userInDb.UserName.ShouldBe("driver_saleh");
        }
    }

    [Fact]
    public async Task CreateAsync_Duplicate_UserName_Or_Email_Should_Throw_BusinessException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var input1 = new CreateDriverProfileInput
            {
                UserName = "driver_duplicate",
                Email = "duplicate@laundry.local",
                Password = "Password123!",
                Name = "First",
                Surname = "Driver",
                PhoneNumber = "0550000001"
            };

            await _driverAppService.CreateAsync(input1);

            var inputDuplicate = new CreateDriverProfileInput
            {
                UserName = "driver_duplicate",
                Email = "other@laundry.local",
                Password = "Password123!",
                Name = "Second",
                Surname = "Driver",
                PhoneNumber = "0550000002"
            };

            // Act & Assert
            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _driverAppService.CreateAsync(inputDuplicate);
            });
        }
    }

    [Fact]
    public async Task GetListAsync_And_GetAsync_Should_Return_Driver_With_ActiveTasksCount()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var driverDto = await _driverAppService.CreateAsync(new CreateDriverProfileInput
            {
                UserName = "driver_active_tasks",
                Email = "active_tasks@laundry.local",
                Password = "Password123!",
                Name = "Tariq",
                Surname = "Mansour",
                PhoneNumber = "0559988776",
                VehicleDetails = "Hyundai H1"
            });

            var order1Id = Guid.NewGuid();
            var order2Id = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Create 1 pickup task assigned to this driver
            var pickupTask = new PickupTask(
                Guid.NewGuid(),
                tenantId,
                order1Id,
                attemptNumber: 1,
                scheduledFrom: now,
                scheduledTo: now.AddHours(2),
                driverId: driverDto.Id
            );
            await _pickupTaskRepository.InsertAsync(pickupTask, autoSave: true);

            // Create 1 delivery task assigned to this driver
            var deliveryTask = new DeliveryTask(
                Guid.NewGuid(),
                tenantId,
                order2Id,
                attemptNumber: 1,
                scheduledFrom: now.AddHours(3),
                scheduledTo: now.AddHours(5),
                cashAmountToCollect: 75.50m,
                driverId: driverDto.Id
            );
            await _deliveryTaskRepository.InsertAsync(deliveryTask, autoSave: true);

            // Act - GetListAsync
            var listResult = await _driverAppService.GetListAsync(new PagedAndSortedResultRequestDto());

            // Assert
            listResult.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
            var listItem = listResult.Items.FirstOrDefault(d => d.Id == driverDto.Id);
            listItem.ShouldNotBeNull();
            listItem.FullName.ShouldBe("Tariq Mansour");
            listItem.ActiveTasksCount.ShouldBe(2);

            // Act - GetAsync
            var profile = await _driverAppService.GetAsync(driverDto.Id);

            // Assert
            profile.ShouldNotBeNull();
            profile.Id.ShouldBe(driverDto.Id);
            profile.FullName.ShouldBe("Tariq Mansour");
            profile.ActiveTasksCount.ShouldBe(2);
            profile.VehicleDetails.ShouldBe("Hyundai H1");
        }
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_IdentityUser_And_VehicleDetails()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var driverDto = await _driverAppService.CreateAsync(new CreateDriverProfileInput
            {
                UserName = "driver_update_test",
                Email = "update_test@laundry.local",
                Password = "Password123!",
                Name = "Original",
                Surname = "Name",
                PhoneNumber = "0551111111",
                VehicleDetails = "Old Van"
            });

            var updateInput = new UpdateDriverProfileInput
            {
                Name = "UpdatedName",
                Surname = "UpdatedSurname",
                PhoneNumber = "0559999999",
                VehicleDetails = "New Toyota Hilux 2025",
                IsActive = true
            };

            // Act
            var updated = await _driverAppService.UpdateAsync(driverDto.Id, updateInput);

            // Assert
            updated.FullName.ShouldBe("UpdatedName UpdatedSurname");
            updated.PhoneNumber.ShouldBe("0559999999");
            updated.VehicleDetails.ShouldBe("New Toyota Hilux 2025");

            var userInDb = await _userManager.FindByIdAsync(driverDto.UserId.ToString());
            userInDb.Name.ShouldBe("UpdatedName");
            userInDb.PhoneNumber.ShouldBe("0559999999");
        }
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_And_GetDriverLookupAsync_Should_Filter_Unavailable_Drivers()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var driver1 = await _driverAppService.CreateAsync(new CreateDriverProfileInput
            {
                UserName = "driver_lookup_1",
                Email = "lookup1@laundry.local",
                Password = "Password123!",
                Name = "Driver",
                Surname = "Available",
                PhoneNumber = "0551000001"
            });

            var driver2 = await _driverAppService.CreateAsync(new CreateDriverProfileInput
            {
                UserName = "driver_lookup_2",
                Email = "lookup2@laundry.local",
                Password = "Password123!",
                Name = "Driver",
                Surname = "Busy",
                PhoneNumber = "0551000002"
            });

            // Mark driver2 as unavailable
            await _driverAppService.UpdateAvailabilityAsync(driver2.Id, new UpdateDriverAvailabilityInput { IsAvailable = false });

            // Act
            var lookup = await _driverAppService.GetDriverLookupAsync();

            // Assert
            lookup.Any(d => d.Id == driver1.Id).ShouldBeTrue();
            lookup.Any(d => d.Id == driver2.Id).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task AssignPickupDriverAsync_And_AssignDeliveryDriverAsync_Should_Assign_Active_Driver()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var driverDto = await _driverAppService.CreateAsync(new CreateDriverProfileInput
            {
                UserName = "driver_assigned",
                Email = "assigned@laundry.local",
                Password = "Password123!",
                Name = "Karim",
                Surname = "Logistics",
                PhoneNumber = "0557778899"
            });

            var orderId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var pickupTask = new PickupTask(
                Guid.NewGuid(),
                tenantId,
                orderId,
                attemptNumber: 1,
                scheduledFrom: now,
                scheduledTo: now.AddHours(2)
            );
            await _pickupTaskRepository.InsertAsync(pickupTask, autoSave: true);

            var deliveryTask = new DeliveryTask(
                Guid.NewGuid(),
                tenantId,
                orderId,
                attemptNumber: 1,
                scheduledFrom: now.AddHours(4),
                scheduledTo: now.AddHours(6),
                cashAmountToCollect: 50.0m
            );
            await _deliveryTaskRepository.InsertAsync(deliveryTask, autoSave: true);

            // Act - Assign Pickup
            await _driverAppService.AssignPickupDriverAsync(pickupTask.Id, new AssignDriverTaskInput { DriverId = driverDto.Id });

            // Act - Assign Delivery
            await _driverAppService.AssignDeliveryDriverAsync(deliveryTask.Id, new AssignDriverTaskInput { DriverId = driverDto.Id });

            // Assert
            var updatedPickup = await _pickupTaskRepository.GetAsync(pickupTask.Id);
            updatedPickup.DriverId.ShouldBe(driverDto.Id);
            updatedPickup.Status.ShouldBe(PickupTaskStatus.Assigned);

            var updatedDelivery = await _deliveryTaskRepository.GetAsync(deliveryTask.Id);
            updatedDelivery.DriverId.ShouldBe(driverDto.Id);
            updatedDelivery.Status.ShouldBe(DeliveryTaskStatus.Assigned);
        }
    }

    [Fact]
    public async Task AssignDriver_When_Driver_Is_Inactive_Should_Throw_BusinessException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using (_currentTenant.Change(tenantId))
        using (AuthenticateStaff(tenantId))
        {
            var driverDto = await _driverAppService.CreateAsync(new CreateDriverProfileInput
            {
                UserName = "driver_inactive_test",
                Email = "inactive@laundry.local",
                Password = "Password123!",
                Name = "Inactive",
                Surname = "Driver",
                PhoneNumber = "0550009999"
            });

            // Deactivate driver
            await _driverAppService.UpdateAsync(driverDto.Id, new UpdateDriverProfileInput
            {
                Name = "Inactive",
                Surname = "Driver",
                PhoneNumber = "0550009999",
                IsActive = false
            });

            var now = DateTime.UtcNow;
            var pickupTask = new PickupTask(Guid.NewGuid(), tenantId, Guid.NewGuid(), 1, now, now.AddHours(2));
            await _pickupTaskRepository.InsertAsync(pickupTask, autoSave: true);

            // Act & Assert
            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _driverAppService.AssignPickupDriverAsync(pickupTask.Id, new AssignDriverTaskInput { DriverId = driverDto.Id });
            });
        }
    }
}
