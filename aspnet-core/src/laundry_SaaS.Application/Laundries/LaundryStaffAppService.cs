using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using laundry_SaaS.Permissions;

namespace laundry_SaaS.Laundries;

/// <summary>
/// تطبيق خدمة إدارة طاقم عمل المغسلة (Laundry Staff Management App Service).
/// تتيح لإدارة المغسلة إنشاء حسابات مستخدمي الهوية للموظفين، ربط ملفاتهم الوظيفية،
/// استعراضهم، تحديث بياناتهم، والتحكم في تفعيلهم أو تعطيلهم وفق مبدأ العزل الصارم للمستأجرين.
/// </summary>
[Authorize(laundry_SaaSPermissions.Staff.Default)]
public class LaundryStaffAppService : laundry_SaaSAppService, ILaundryStaffAppService
{
    private readonly IRepository<LaundryStaffProfile, Guid> _staffProfileRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager;

    public LaundryStaffAppService(
        IRepository<LaundryStaffProfile, Guid> staffProfileRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IdentityUserManager userManager)
    {
        _staffProfileRepository = staffProfileRepository;
        _userRepository = userRepository;
        _userManager = userManager;
    }

    /// <summary>
    /// يسترجع قائمة موظفي المغسلة في شكل صفحات قابلة للتصفح والترتيب، مع دمج تفاصيل المستخدمين من نظام الهوية.
    /// </summary>
    /// <param name="input">معايير التصفح والترتيب.</param>
    /// <returns>صفحة تحتوي على موظفي المغسلة.</returns>
    [Authorize(laundry_SaaSPermissions.Staff.Default)]
    public virtual async Task<PagedResultDto<LaundryStaffListDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _staffProfileRepository.GetQueryableAsync();
        query = query.Where(s => s.TenantId == tenantId);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = !string.IsNullOrWhiteSpace(input.Sorting)
            ? query.OrderBy(input.Sorting)
            : query.OrderByDescending(s => s.CreationTime);

        query = query.PageBy(input.SkipCount, input.MaxResultCount);
        var staffProfiles = await AsyncExecuter.ToListAsync(query);

        var userIds = staffProfiles.Select(s => s.UserId).Distinct().ToList();
        var userQuery = await _userRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(userQuery.Where(u => userIds.Contains(u.Id)));
        var userDict = users.ToDictionary(u => u.Id, u => u);

        var items = staffProfiles.Select(s =>
        {
            userDict.TryGetValue(s.UserId, out var user);
            var fullName = user != null ? $"{user.Name} {user.Surname}".Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(fullName) && user != null)
            {
                fullName = user.UserName;
            }

            return new LaundryStaffListDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = user?.UserName ?? string.Empty,
                FullName = fullName,
                JobTitle = s.JobTitle,
                IsActive = s.IsActive,
                CreationTime = s.CreationTime
            };
        }).ToList();

        return new PagedResultDto<LaundryStaffListDto>(totalCount, items);
    }

    /// <summary>
    /// يسترجع البيانات التفصيلية الكاملة لملف موظف محدد بالمعرّف مع بيانات حسابه في نظام الهوية الموحد.
    /// </summary>
    /// <param name="id">معرّف ملف الموظف.</param>
    /// <returns>تفاصيل ملف الموظف الكاملة.</returns>
    [Authorize(laundry_SaaSPermissions.Staff.Default)]
    public virtual async Task<LaundryStaffProfileDto> GetAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var profile = await _staffProfileRepository.FindAsync(id);
        if (profile == null || profile.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryStaffProfile), id);
        }

        var user = await _userManager.FindByIdAsync(profile.UserId.ToString());
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), profile.UserId);
        }

        return MapToDto(profile, user);
    }

    /// <summary>
    /// يُنشئ حساب مستخدم جديد في نظام الهوية تحت المستأجر الحالي، ويُنشئ الملف الوظيفي المرتبط به في المغسلة.
    /// </summary>
    /// <param name="input">بيانات الحساب الشخصية وبيانات الدخول والمسمى الوظيفي.</param>
    /// <returns>ملف الموظف الذي تم إنشاؤه.</returns>
    [Authorize(laundry_SaaSPermissions.Staff.Manage)]
    public virtual async Task<LaundryStaffProfileDto> CreateAsync(CreateLaundryStaffProfileInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var existingUser = await _userManager.FindByNameAsync(input.UserName);
        if (existingUser != null)
        {
            throw new BusinessException($"Username '{input.UserName}' is already taken.");
        }

        var existingEmail = await _userManager.FindByEmailAsync(input.Email);
        if (existingEmail != null)
        {
            throw new BusinessException($"Email '{input.Email}' is already registered.");
        }

        var user = new IdentityUser(
            GuidGenerator.Create(),
            input.UserName,
            input.Email,
            tenantId);

        user.Name = input.Name;
        user.Surname = input.Surname;
        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber, confirmed: false);
        }
        user.SetIsActive(true);

        var result = await _userManager.CreateAsync(user, input.Password);
        result.CheckErrors();

        var profile = new LaundryStaffProfile(
            GuidGenerator.Create(),
            tenantId,
            user.Id,
            input.JobTitle,
            isActive: true);

        await _staffProfileRepository.InsertAsync(profile, autoSave: true);

        return MapToDto(profile, user, input.Notes);
    }

    /// <summary>
    /// يحدّث البيانات الشخصية والوظيفية لموظف مغسلة حالي في كل من نظام الهوية وملف المغسلة.
    /// </summary>
    /// <param name="id">معرّف ملف الموظف.</param>
    /// <param name="input">البيانات الجديدة وقفل التزامن.</param>
    /// <returns>بيانات ملف الموظف بعد التحديث.</returns>
    [Authorize(laundry_SaaSPermissions.Staff.Manage)]
    public virtual async Task<LaundryStaffProfileDto> UpdateAsync(Guid id, UpdateLaundryStaffProfileInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var profile = await _staffProfileRepository.FindAsync(id);
        if (profile == null || profile.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryStaffProfile), id);
        }

        var user = await _userManager.FindByIdAsync(profile.UserId.ToString());
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), profile.UserId);
        }

        user.Name = input.Name;
        user.Surname = input.Surname;
        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber, confirmed: false);
        }
        user.SetIsActive(input.IsActive);

        var userUpdateResult = await _userManager.UpdateAsync(user);
        userUpdateResult.CheckErrors();

        profile.SetJobTitle(input.JobTitle);
        profile.SetActive(input.IsActive);

        await _staffProfileRepository.UpdateAsync(profile, autoSave: true);

        return MapToDto(profile, user, input.Notes);
    }

    /// <summary>
    /// يعطل أو يفعل ملف موظف المغسلة وحسابه في نظام الهوية دون حذفه للحفاظ على السجل التاريخي والتدقيق.
    /// </summary>
    /// <param name="id">معرّف ملف الموظف.</param>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    [Authorize(laundry_SaaSPermissions.Staff.Manage)]
    public virtual async Task SetActiveAsync(Guid id, bool isActive)
    {
        var tenantId = EnsureCurrentTenant();

        var profile = await _staffProfileRepository.FindAsync(id);
        if (profile == null || profile.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryStaffProfile), id);
        }

        profile.SetActive(isActive);
        await _staffProfileRepository.UpdateAsync(profile, autoSave: true);

        var user = await _userManager.FindByIdAsync(profile.UserId.ToString());
        if (user != null)
        {
            user.SetIsActive(isActive);
            var updateResult = await _userManager.UpdateAsync(user);
            updateResult.CheckErrors();
        }
    }

    private static LaundryStaffProfileDto MapToDto(LaundryStaffProfile profile, IdentityUser user, string? notes = null)
    {
        var fullName = $"{user.Name} {user.Surname}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = user.UserName;
        }

        return new LaundryStaffProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            UserName = user.UserName,
            FullName = fullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            JobTitle = profile.JobTitle,
            IsActive = profile.IsActive,
            Notes = notes,
            CreationTime = profile.CreationTime
        };
    }

    private Guid EnsureCurrentTenant()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":TenantRequired",
                "Operating on laundry staff requires an active tenant context.");
        }

        return CurrentTenant.Id.Value;
    }
}
