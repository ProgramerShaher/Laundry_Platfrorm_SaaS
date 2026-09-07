using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace laundry_SaaS.Laundries;

public abstract class LaundryStaffAppServiceTests<TStartupModule> : laundry_SaaSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ILaundryStaffAppService _staffAppService;
    private readonly IRepository<LaundryStaffProfile, Guid> _staffProfileRepository;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentTenant _currentTenant;

    public LaundryStaffAppServiceTests()
    {
        _staffAppService = GetRequiredService<ILaundryStaffAppService>();
        _staffProfileRepository = GetRequiredService<IRepository<LaundryStaffProfile, Guid>>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Operations_Without_Tenant_Context_Should_Throw_BusinessException()
    {
        using (_currentTenant.Change(null))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _staffAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            });

            ex.Code.ShouldBe("laundry_SaaS:TenantRequired");

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _staffAppService.GetAsync(Guid.NewGuid());
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _staffAppService.CreateAsync(new CreateLaundryStaffProfileInput
                {
                    UserName = "teststaff",
                    Email = "teststaff@laundry.local",
                    Password = "Password123!",
                    Name = "Test",
                    Surname = "Staff"
                });
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _staffAppService.SetActiveAsync(Guid.NewGuid(), false);
            });
        }
    }

    [Fact]
    public async Task CreateAsync_Should_Create_IdentityUser_And_StaffProfile()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            var input = new CreateLaundryStaffProfileInput
            {
                UserName = "staff_ahmed",
                Email = "ahmed@laundry.local",
                Password = "Password123!",
                Name = "Ahmed",
                Surname = "Ali",
                PhoneNumber = "0551122334",
                JobTitle = "Washing Specialist"
            };

            var staffDto = await _staffAppService.CreateAsync(input);

            staffDto.ShouldNotBeNull();
            staffDto.UserName.ShouldBe("staff_ahmed");
            staffDto.FullName.ShouldBe("Ahmed Ali");
            staffDto.Email.ShouldBe("ahmed@laundry.local");
            staffDto.PhoneNumber.ShouldBe("0551122334");
            staffDto.JobTitle.ShouldBe("Washing Specialist");
            staffDto.IsActive.ShouldBeTrue();

            var savedProfile = await _staffProfileRepository.FindAsync(staffDto.Id);
            savedProfile.ShouldNotBeNull();
            savedProfile.UserId.ShouldBe(staffDto.UserId);
            savedProfile.TenantId.ShouldBe(tenantId);
            savedProfile.JobTitle.ShouldBe("Washing Specialist");

            var savedUser = await _userManager.FindByIdAsync(staffDto.UserId.ToString());
            savedUser.ShouldNotBeNull();
            savedUser.UserName.ShouldBe("staff_ahmed");
            savedUser.Name.ShouldBe("Ahmed");
            savedUser.Surname.ShouldBe("Ali");
            savedUser.IsActive.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task GetAsync_Should_Return_StaffProfile_With_UserDetails()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            var created = await _staffAppService.CreateAsync(new CreateLaundryStaffProfileInput
            {
                UserName = "staff_khalid",
                Email = "khalid@laundry.local",
                Password = "Password123!",
                Name = "Khalid",
                Surname = "Omar",
                JobTitle = "Supervisor"
            });

            var result = await _staffAppService.GetAsync(created.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
            result.UserName.ShouldBe("staff_khalid");
            result.FullName.ShouldBe("Khalid Omar");
            result.JobTitle.ShouldBe("Supervisor");
            result.IsActive.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task GetListAsync_Should_Return_Paged_Staff_List()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            await _staffAppService.CreateAsync(new CreateLaundryStaffProfileInput
            {
                UserName = "staff_1",
                Email = "staff1@laundry.local",
                Password = "Password123!",
                Name = "Staff",
                Surname = "One",
                JobTitle = "Operator"
            });

            await _staffAppService.CreateAsync(new CreateLaundryStaffProfileInput
            {
                UserName = "staff_2",
                Email = "staff2@laundry.local",
                Password = "Password123!",
                Name = "Staff",
                Surname = "Two",
                JobTitle = "Quality Control"
            });

            var list = await _staffAppService.GetListAsync(new PagedAndSortedResultRequestDto
            {
                MaxResultCount = 10,
                SkipCount = 0
            });

            list.TotalCount.ShouldBe(2);
            list.Items.Count.ShouldBe(2);
            list.Items.ShouldContain(s => s.UserName == "staff_1");
            list.Items.ShouldContain(s => s.UserName == "staff_2");
        }
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Profile_And_User()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            var created = await _staffAppService.CreateAsync(new CreateLaundryStaffProfileInput
            {
                UserName = "staff_update",
                Email = "update@laundry.local",
                Password = "Password123!",
                Name = "OldName",
                Surname = "OldSurname",
                JobTitle = "Junior Washer"
            });

            var updated = await _staffAppService.UpdateAsync(created.Id, new UpdateLaundryStaffProfileInput
            {
                Name = "NewName",
                Surname = "NewSurname",
                PhoneNumber = "0509988776",
                JobTitle = "Senior Washer",
                IsActive = true
            });

            updated.FullName.ShouldBe("NewName NewSurname");
            updated.PhoneNumber.ShouldBe("0509988776");
            updated.JobTitle.ShouldBe("Senior Washer");

            var savedUser = await _userManager.FindByIdAsync(created.UserId.ToString());
            savedUser.ShouldNotBeNull();
            savedUser.Name.ShouldBe("NewName");
            savedUser.Surname.ShouldBe("NewSurname");
        }
    }

    [Fact]
    public async Task SetActiveAsync_Should_Toggle_Status_On_Profile_And_User()
    {
        var tenantId = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            var created = await _staffAppService.CreateAsync(new CreateLaundryStaffProfileInput
            {
                UserName = "staff_active_toggle",
                Email = "toggle@laundry.local",
                Password = "Password123!",
                Name = "Toggle",
                Surname = "Test",
                JobTitle = "Tester"
            });

            // Deactivate
            await _staffAppService.SetActiveAsync(created.Id, false);

            var profileDeactivated = await _staffProfileRepository.GetAsync(created.Id);
            profileDeactivated.IsActive.ShouldBeFalse();

            var userDeactivated = await _userManager.FindByIdAsync(created.UserId.ToString());
            userDeactivated.ShouldNotBeNull();
            userDeactivated.IsActive.ShouldBeFalse();

            // Reactivate
            await _staffAppService.SetActiveAsync(created.Id, true);

            var profileReactivated = await _staffProfileRepository.GetAsync(created.Id);
            profileReactivated.IsActive.ShouldBeTrue();

            var userReactivated = await _userManager.FindByIdAsync(created.UserId.ToString());
            userReactivated.ShouldNotBeNull();
            userReactivated.IsActive.ShouldBeTrue();
        }
    }
}
