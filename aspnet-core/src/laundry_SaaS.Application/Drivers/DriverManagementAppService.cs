using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using laundry_SaaS.Permissions;
using laundry_SaaS.PickupDelivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace laundry_SaaS.Drivers;

/// <summary>
/// خدمة إدارة ملفات السائقين والعمليات اللوجستية التابعة لإدارة المغسلة (Driver Management App Service).
/// تتيح إدارة السائقين، تسجيل حساباتهم ومركباتهم، تتبع جاهزيتهم الفورية، وإسناد مهام الاستلام والتوصيل.
/// </summary>
[Authorize(laundry_SaaSPermissions.Drivers.Default)]
public class DriverManagementAppService : laundry_SaaSAppService, IDriverManagementAppService
{
    private readonly IRepository<Driver, Guid> _driverRepository;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<PickupTask, Guid> _pickupTaskRepository;
    private readonly IRepository<DeliveryTask, Guid> _deliveryTaskRepository;

    /// <summary>
    /// يُنشئ نسخة جديدة من خدمة إدارة السائقين مع حقن المستودعات والخدمات اللازمة.
    /// </summary>
    public DriverManagementAppService(
        IRepository<Driver, Guid> driverRepository,
        IdentityUserManager identityUserManager,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<PickupTask, Guid> pickupTaskRepository,
        IRepository<DeliveryTask, Guid> deliveryTaskRepository)
    {
        _driverRepository = driverRepository;
        _identityUserManager = identityUserManager;
        _identityUserRepository = identityUserRepository;
        _pickupTaskRepository = pickupTaskRepository;
        _deliveryTaskRepository = deliveryTaskRepository;
    }

    /// <summary>
    /// يسترجع قائمة السائقين التابعين للمغسلة في صفحات مع حساب عدد المهام النشطة لكل سائق.
    /// </summary>
    public virtual async Task<PagedResultDto<DriverListDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        EnsureCurrentTenantId();

        var driverQueryable = await _driverRepository.GetQueryableAsync();
        var totalCount = await AsyncExecuter.CountAsync(driverQueryable);

        var drivers = await AsyncExecuter.ToListAsync(
            driverQueryable
                .OrderBy(d => d.CreationTime)
                .PageBy(input.SkipCount, input.MaxResultCount)
        );

        if (drivers.Count == 0)
        {
            return new PagedResultDto<DriverListDto>(totalCount, new List<DriverListDto>());
        }

        var userIds = drivers.Select(d => d.UserId).Distinct().ToList();
        var usersQueryable = await _identityUserRepository.GetQueryableAsync();
        var users = (await AsyncExecuter.ToListAsync(usersQueryable.Where(u => userIds.Contains(u.Id))))
            .ToDictionary(u => u.Id);

        var activeTaskCounts = await GetActiveTaskCountsAsync(drivers.Select(d => d.Id).ToList());

        var items = drivers.Select(d =>
        {
            users.TryGetValue(d.UserId, out var user);
            activeTaskCounts.TryGetValue(d.Id, out var taskCount);

            return new DriverListDto
            {
                Id = d.Id,
                UserId = d.UserId,
                FullName = user != null ? $"{user.Name} {user.Surname}".Trim() : string.Empty,
                PhoneNumber = user?.PhoneNumber ?? string.Empty,
                IsAvailable = d.IsAvailable,
                IsActive = d.IsActive,
                ActiveTasksCount = taskCount
            };
        }).ToList();

        return new PagedResultDto<DriverListDto>(totalCount, items);
    }

    /// <summary>
    /// يسترجع تفاصيل ملف سائق محدد بالمعرف الفريد.
    /// </summary>
    public virtual async Task<DriverProfileDto> GetAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenantId();
        var driver = await _driverRepository.GetAsync(id);

        if (driver.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(Driver), id);
        }

        var user = await _identityUserManager.FindByIdAsync(driver.UserId.ToString());
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), driver.UserId);
        }

        var activeTaskCounts = await GetActiveTaskCountsAsync(new List<Guid> { driver.Id });
        activeTaskCounts.TryGetValue(driver.Id, out var count);

        return new DriverProfileDto
        {
            Id = driver.Id,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = $"{user.Name} {user.Surname}".Trim(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            VehicleDetails = driver.VehicleDetails,
            IsActive = driver.IsActive,
            IsAvailable = driver.IsAvailable,
            ActiveTasksCount = count,
            CreationTime = driver.CreationTime
        };
    }

    /// <summary>
    /// يُنشئ حساب مستخدم جديد وهوية في ABP Identity ثم يربطه كملف سائق في المغسلة الحالية.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Drivers.Manage)]
    public virtual async Task<DriverProfileDto> CreateAsync(CreateDriverProfileInput input)
    {
        Check.NotNull(input, nameof(input));
        var tenantId = EnsureCurrentTenantId();

        var existingUserByUserName = await _identityUserManager.FindByNameAsync(input.UserName);
        if (existingUserByUserName != null)
        {
            throw new BusinessException("Drivers:UserNameAlreadyExists", $"اسم المستخدم '{input.UserName}' مسجل مسبقاً في النظام.");
        }

        var existingUserByEmail = await _identityUserManager.FindByEmailAsync(input.Email);
        if (existingUserByEmail != null)
        {
            throw new BusinessException("Drivers:EmailAlreadyExists", $"البريد الإلكتروني '{input.Email}' مسجل مسبقاً في النظام.");
        }

        var user = new IdentityUser(
            GuidGenerator.Create(),
            input.UserName,
            input.Email,
            tenantId)
        {
            Name = input.Name,
            Surname = input.Surname
        };

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber, false);
        }

        (await _identityUserManager.CreateAsync(user, input.Password)).CheckErrors();

        var driver = new Driver(
            GuidGenerator.Create(),
            tenantId,
            user.Id,
            input.VehicleDetails,
            isActive: true,
            isAvailable: true);

        await _driverRepository.InsertAsync(driver, autoSave: true);

        return new DriverProfileDto
        {
            Id = driver.Id,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = $"{user.Name} {user.Surname}".Trim(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            VehicleDetails = driver.VehicleDetails,
            IsActive = driver.IsActive,
            IsAvailable = driver.IsAvailable,
            ActiveTasksCount = 0,
            CreationTime = driver.CreationTime
        };
    }

    /// <summary>
    /// يحدّث البيانات الشخصية للسائق وتفاصيل مركبته وحالة تفعيله.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Drivers.Manage)]
    public virtual async Task<DriverProfileDto> UpdateAsync(Guid id, UpdateDriverProfileInput input)
    {
        Check.NotNull(input, nameof(input));
        var tenantId = EnsureCurrentTenantId();

        var driver = await _driverRepository.GetAsync(id);
        if (driver.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(Driver), id);
        }

        var user = await _identityUserManager.GetByIdAsync(driver.UserId);
        user.Name = input.Name;
        user.Surname = input.Surname;

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber) && !string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.OrdinalIgnoreCase))
        {
            user.SetPhoneNumber(input.PhoneNumber, false);
        }

        (await _identityUserManager.UpdateAsync(user)).CheckErrors();

        driver.UpdateVehicleDetails(input.VehicleDetails);
        driver.SetActive(input.IsActive);

        await _driverRepository.UpdateAsync(driver, autoSave: true);

        var activeTaskCounts = await GetActiveTaskCountsAsync(new List<Guid> { driver.Id });
        activeTaskCounts.TryGetValue(driver.Id, out var count);

        return new DriverProfileDto
        {
            Id = driver.Id,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = $"{user.Name} {user.Surname}".Trim(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            VehicleDetails = driver.VehicleDetails,
            IsActive = driver.IsActive,
            IsAvailable = driver.IsAvailable,
            ActiveTasksCount = count,
            CreationTime = driver.CreationTime
        };
    }

    /// <summary>
    /// يحدّث حالة توفر السائق الفورية لتلقي مهام لوجستية جديدة (جاهز / غير متاح).
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Drivers.Manage)]
    public virtual async Task UpdateAvailabilityAsync(Guid id, UpdateDriverAvailabilityInput input)
    {
        Check.NotNull(input, nameof(input));
        var tenantId = EnsureCurrentTenantId();

        var driver = await _driverRepository.GetAsync(id);
        if (driver.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(Driver), id);
        }

        driver.SetAvailability(input.IsAvailable);
        await _driverRepository.UpdateAsync(driver, autoSave: true);
    }

    /// <summary>
    /// يسترجع قائمة مختصرة بالسائقين النشطين والمتاحين فقط لإسناد المهام.
    /// </summary>
    public virtual async Task<List<DriverLookupDto>> GetDriverLookupAsync()
    {
        EnsureCurrentTenantId();

        var driverQueryable = await _driverRepository.GetQueryableAsync();
        var drivers = await AsyncExecuter.ToListAsync(
            driverQueryable.Where(d => d.IsActive && d.IsAvailable)
        );

        if (drivers.Count == 0)
        {
            return new List<DriverLookupDto>();
        }

        var userIds = drivers.Select(d => d.UserId).Distinct().ToList();
        var usersQueryable = await _identityUserRepository.GetQueryableAsync();
        var users = (await AsyncExecuter.ToListAsync(usersQueryable.Where(u => userIds.Contains(u.Id))))
            .ToDictionary(u => u.Id);

        return drivers.Select(d =>
        {
            users.TryGetValue(d.UserId, out var u);
            return new DriverLookupDto
            {
                Id = d.Id,
                FullName = u != null ? $"{u.Name} {u.Surname}".Trim() : string.Empty,
                PhoneNumber = u?.PhoneNumber ?? string.Empty,
                IsAvailable = d.IsAvailable
            };
        }).ToList();
    }

    /// <summary>
    /// إسناد مهمة استلام ملابس لسائق محدد من قبل إدارة المغسلة.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Drivers.AssignTasks)]
    public virtual async Task AssignPickupDriverAsync(Guid id, AssignDriverTaskInput input)
    {
        Check.NotNull(input, nameof(input));
        var tenantId = EnsureCurrentTenantId();

        var driver = await _driverRepository.GetAsync(input.DriverId);
        if (driver.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(Driver), input.DriverId);
        }

        if (!driver.IsActive)
        {
            throw new BusinessException("Drivers:DriverInactive", "لا يمكن إسناد مهمة استلام لسائق حسابه غير مفعل.");
        }

        var task = await _pickupTaskRepository.GetAsync(id);
        if (task.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(PickupTask), id);
        }

        task.Assign(driver.Id, Clock.Now);
        await _pickupTaskRepository.UpdateAsync(task, autoSave: true);
    }

    /// <summary>
    /// إسناد مهمة توصيل ملابس لسائق محدد من قبل إدارة المغسلة.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Drivers.AssignTasks)]
    public virtual async Task AssignDeliveryDriverAsync(Guid id, AssignDriverTaskInput input)
    {
        Check.NotNull(input, nameof(input));
        var tenantId = EnsureCurrentTenantId();

        var driver = await _driverRepository.GetAsync(input.DriverId);
        if (driver.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(Driver), input.DriverId);
        }

        if (!driver.IsActive)
        {
            throw new BusinessException("Drivers:DriverInactive", "لا يمكن إسناد مهمة توصيل لسائق حسابه غير مفعل.");
        }

        var task = await _deliveryTaskRepository.GetAsync(id);
        if (task.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(DeliveryTask), id);
        }

        task.Assign(driver.Id, Clock.Now);
        await _deliveryTaskRepository.UpdateAsync(task, autoSave: true);
    }

    private async Task<Dictionary<Guid, int>> GetActiveTaskCountsAsync(List<Guid> driverIds)
    {
        if (driverIds == null || driverIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var pickupQueryable = await _pickupTaskRepository.GetQueryableAsync();
        var pickupCounts = (await AsyncExecuter.ToListAsync(
            pickupQueryable
                .Where(p => p.DriverId.HasValue && driverIds.Contains(p.DriverId.Value) &&
                            p.Status != PickupTaskStatus.Completed &&
                            p.Status != PickupTaskStatus.Failed &&
                            p.Status != PickupTaskStatus.Cancelled)
                .GroupBy(p => p.DriverId!.Value)
                .Select(g => new { DriverId = g.Key, Count = g.Count() })
        )).ToDictionary(x => x.DriverId, x => x.Count);

        var deliveryQueryable = await _deliveryTaskRepository.GetQueryableAsync();
        var deliveryCounts = (await AsyncExecuter.ToListAsync(
            deliveryQueryable
                .Where(d => d.DriverId.HasValue && driverIds.Contains(d.DriverId.Value) &&
                            d.Status != DeliveryTaskStatus.Delivered &&
                            d.Status != DeliveryTaskStatus.Failed &&
                            d.Status != DeliveryTaskStatus.Cancelled)
                .GroupBy(d => d.DriverId!.Value)
                .Select(g => new { DriverId = g.Key, Count = g.Count() })
        )).ToDictionary(x => x.DriverId, x => x.Count);

        var result = new Dictionary<Guid, int>();
        foreach (var driverId in driverIds)
        {
            var pc = pickupCounts.TryGetValue(driverId, out var pVal) ? pVal : 0;
            var dc = deliveryCounts.TryGetValue(driverId, out var dVal) ? dVal : 0;
            result[driverId] = pc + dc;
        }

        return result;
    }

    private Guid EnsureCurrentTenantId()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new BusinessException("Drivers:TenantRequired", "إدارة السائقين تتطلب سياق مستأجر نشط (Tenant).");
        }

        return CurrentTenant.Id.Value;
    }
}
