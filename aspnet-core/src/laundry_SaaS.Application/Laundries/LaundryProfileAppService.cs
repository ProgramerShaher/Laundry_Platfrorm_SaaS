using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using laundry_SaaS.Permissions;
using Volo.Abp.Data;

namespace laundry_SaaS.Laundries;

/// <summary>
/// تطبيق خدمة إدارة الملف التعريفي والتشغيلي للمغسلة (Laundry Profile Management App Service).
/// ينسق العمليات التشغيلية وساعات العمل ونطاق التغطية ضمن سياق المستأجر الموثق.
/// </summary>
[Authorize]
public class LaundryProfileAppService : laundry_SaaSAppService, ILaundryProfileAppService
{
    private readonly IRepository<Laundry, Guid> _laundryRepository;

    public LaundryProfileAppService(IRepository<Laundry, Guid> laundryRepository)
    {
        _laundryRepository = laundryRepository;
    }

    /// <summary>
    /// يسترجع الملف التعريفي والتشغيلي الكامل للمغسلة الحالية بناءً على جلسة المستأجر.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ViewProfile)]
    public virtual async Task<LaundryDetailDto> GetProfileAsync()
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        return MapToDetailDto(laundry);
    }

    /// <summary>
    /// يحدّث البيانات العامة للمغسلة والرسوم وحالة استقبال الطلبات والشعار وفق الخصائص المعتمدة في النطاق.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ManageProfile)]
    public virtual async Task<LaundryDetailDto> UpdateProfileAsync(UpdateLaundryProfileInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        if (!string.IsNullOrWhiteSpace(input.ConcurrencyStamp) && laundry.ConcurrencyStamp != input.ConcurrencyStamp)
        {
            throw new AbpDbConcurrencyException();
        }

        laundry.SetName(input.Name);
        laundry.SetPhoneNumber(input.PhoneNumber);
        laundry.SetFees(input.DeliveryFee, input.MinimumOrderAmount);
        laundry.SetAcceptingOrders(input.AcceptingOrders);
        laundry.SetLogo(input.LogoBlobName);

        await _laundryRepository.UpdateAsync(laundry, autoSave: true);

        return MapToDetailDto(laundry);
    }

    /// <summary>
    /// يضبط نطاق التغطية الجغرافية والتشغيلية للمغسلة.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ManageProfile)]
    public virtual async Task SetCoverageAreaAsync(SetCoverageAreaInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var laundry = await _laundryRepository.FirstOrDefaultAsync(l => l.TenantId == tenantId);
        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var coverageArea = new CoverageArea(
            input.CenterLatitude,
            input.CenterLongitude,
            input.RadiusKm,
            input.MinimumOrderAmount);

        laundry.SetCoverageArea(coverageArea);
        await _laundryRepository.UpdateAsync(laundry, autoSave: true);
    }

    /// <summary>
    /// يضبط أو يحدّث جدول ساعات العمل ليوم محدد من أيام الأسبوع.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ManageWorkingHours)]
    public virtual async Task<LaundryWorkingHourDto> SetWorkingHourAsync(SetLaundryWorkingHourInput input)
    {
        var tenantId = EnsureCurrentTenant();

        // Load laundry with both WorkingHours and TimeSlots because Domain invariants check TimeSlots on working hour mutation
        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours, l => l.TimeSlots);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var workingHour = laundry.SetWorkingHour(
            input.DayOfWeek,
            input.IsOpen,
            input.OpenTime,
            input.CloseTime);

        await _laundryRepository.UpdateAsync(laundry, autoSave: true);

        return new LaundryWorkingHourDto
        {
            Id = workingHour.Id,
            LaundryId = workingHour.LaundryId,
            DayOfWeek = workingHour.DayOfWeek,
            IsOpen = workingHour.IsOpen,
            OpenTime = workingHour.OpenTime,
            CloseTime = workingHour.CloseTime
        };
    }

    /// <summary>
    /// يسترجع قائمة ساعات العمل لكافة أيام الأسبوع للمغسلة الحالية.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ViewProfile)]
    public virtual async Task<List<LaundryWorkingHourDto>> GetWorkingHoursAsync()
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        return laundry.WorkingHours
            .OrderBy(w => w.DayOfWeek)
            .Select(w => new LaundryWorkingHourDto
            {
                Id = w.Id,
                LaundryId = w.LaundryId,
                DayOfWeek = w.DayOfWeek,
                IsOpen = w.IsOpen,
                OpenTime = w.OpenTime,
                CloseTime = w.CloseTime
            })
            .ToList();
    }

    private Guid EnsureCurrentTenant()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":TenantRequired",
                "Operating on laundry profile requires an active tenant context.");
        }

        return CurrentTenant.Id.Value;
    }

    private static LaundryDetailDto MapToDetailDto(Laundry laundry)
    {
        return new LaundryDetailDto
        {
            Id = laundry.Id,
            Name = laundry.Name,
            Description = laundry.Description,
            PhoneNumber = laundry.PhoneNumber,
            Email = laundry.Email,
            LogoBlobName = laundry.LogoBlobName,
            Latitude = laundry.Latitude,
            Longitude = laundry.Longitude,
            IsActive = laundry.IsActive,
            AcceptingOrders = laundry.AcceptingOrders,
            DeliveryFee = laundry.DeliveryFee,
            MinimumOrderAmount = laundry.MinimumOrderAmount,
            EstimatedProcessingHours = laundry.EstimatedProcessingHours,
            ConcurrencyStamp = laundry.ConcurrencyStamp,
            CoverageArea = laundry.CoverageArea == null ? null : new CoverageAreaDto
            {
                CenterLatitude = laundry.CoverageArea.CenterLatitude,
                CenterLongitude = laundry.CoverageArea.CenterLongitude,
                RadiusKm = laundry.CoverageArea.DeliveryRadiusKm,
                MinimumOrderAmount = laundry.CoverageArea.MinimumOrderAmount
            },
            WorkingHours = laundry.WorkingHours
                .OrderBy(w => w.DayOfWeek)
                .Select(w => new LaundryWorkingHourDto
                {
                    Id = w.Id,
                    LaundryId = w.LaundryId,
                    DayOfWeek = w.DayOfWeek,
                    IsOpen = w.IsOpen,
                    OpenTime = w.OpenTime,
                    CloseTime = w.CloseTime
                })
                .ToList()
        };
    }
}
