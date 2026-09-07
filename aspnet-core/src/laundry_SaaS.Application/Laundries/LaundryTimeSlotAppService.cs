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

namespace laundry_SaaS.Laundries;

/// <summary>
/// تطبيق خدمة إدارة الفترات الزمنية المجدولة للمغسلة (Laundry Time Slots App Service).
/// يدير فترات الاستلام والتوصيل للأيام المفتوحة مع التحقق من عدم التداخل ومطابقة ساعات العمل.
/// </summary>
[Authorize]
public class LaundryTimeSlotAppService : laundry_SaaSAppService, ILaundryTimeSlotAppService
{
    private readonly IRepository<Laundry, Guid> _laundryRepository;

    public LaundryTimeSlotAppService(IRepository<Laundry, Guid> laundryRepository)
    {
        _laundryRepository = laundryRepository;
    }

    /// <summary>
    /// يسترجع جميع الفترات الزمنية المجدولة للمغسلة الحالية، مع إمكانية التصفية حسب يوم الأسبوع أو نوع الفترة.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ViewProfile)]
    public virtual async Task<List<LaundryTimeSlotDto>> GetListAsync(DayOfWeek? dayOfWeek = null, SlotType? slotType = null)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.TimeSlots);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var slotsQuery = laundry.TimeSlots.AsEnumerable();

        if (dayOfWeek.HasValue)
        {
            slotsQuery = slotsQuery.Where(s => s.DayOfWeek == dayOfWeek.Value);
        }

        if (slotType.HasValue)
        {
            slotsQuery = slotsQuery.Where(s => s.SlotType == slotType.Value);
        }

        return slotsQuery
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>
    /// يسترجع تفاصيل فترة زمنية مجدولة محددة بالمعرّف.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ViewProfile)]
    public virtual async Task<LaundryTimeSlotDto> GetAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.TimeSlots);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var slot = laundry.TimeSlots.FirstOrDefault(s => s.Id == id);
        if (slot == null)
        {
            throw new EntityNotFoundException(typeof(LaundryTimeSlot), id);
        }

        return MapToDto(slot);
    }

    /// <summary>
    /// يُنشئ فترة زمنية مجدولة جديدة للاستلام أو التوصيل بعد التحقق من وقوعها داخل ساعات العمل وعدم تداخلها.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ManageTimeSlots)]
    public virtual async Task<LaundryTimeSlotDto> CreateAsync(CreateLaundryTimeSlotInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours, l => l.TimeSlots);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var slotId = GuidGenerator.Create();
        var slot = laundry.AddTimeSlot(
            slotId,
            input.DayOfWeek,
            input.SlotType,
            input.StartTime,
            input.EndTime,
            input.IsActive);

        await _laundryRepository.UpdateAsync(laundry, autoSave: true);

        return MapToDto(slot);
    }

    /// <summary>
    /// يحدّث مواعيد فترة زمنية موجودة أو يغير حالة تفعيلها مع مراعاة قيود ساعات العمل والتداخل.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ManageTimeSlots)]
    public virtual async Task<LaundryTimeSlotDto> UpdateAsync(Guid id, UpdateLaundryTimeSlotInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours, l => l.TimeSlots);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var slot = laundry.TimeSlots.FirstOrDefault(s => s.Id == id);
        if (slot == null)
        {
            throw new EntityNotFoundException(typeof(LaundryTimeSlot), id);
        }

        if (input.StartTime >= input.EndTime)
        {
            throw new BusinessException("StartTime must be earlier than EndTime.");
        }

        var workingHour = laundry.WorkingHours.FirstOrDefault(w => w.DayOfWeek == slot.DayOfWeek);
        if (workingHour == null || !workingHour.IsOpen)
        {
            throw new BusinessException($"Cannot update time slot on {slot.DayOfWeek} because the laundry is closed or working hours are not defined for this day.");
        }

        if (input.StartTime < workingHour.OpenTime!.Value || input.EndTime > workingHour.CloseTime!.Value)
        {
            throw new BusinessException($"Time slot [{input.StartTime}-{input.EndTime}] is outside the laundry working hours [{workingHour.OpenTime!.Value}-{workingHour.CloseTime!.Value}] on {slot.DayOfWeek}.");
        }

        if (input.IsActive)
        {
            var hasOverlap = laundry.TimeSlots.Any(s =>
                s.Id != id &&
                s.DayOfWeek == slot.DayOfWeek &&
                s.SlotType == slot.SlotType &&
                s.IsActive &&
                input.StartTime < s.EndTime &&
                input.EndTime > s.StartTime);

            if (hasOverlap)
            {
                throw new BusinessException($"Active time slot [{input.StartTime}-{input.EndTime}] for {slot.SlotType} on {slot.DayOfWeek} overlaps with an existing active time slot.");
            }
        }

        slot.SetTime(input.StartTime, input.EndTime);
        slot.SetActive(input.IsActive);

        await _laundryRepository.UpdateAsync(laundry, autoSave: true);

        return MapToDto(slot);
    }

    /// <summary>
    /// يغير حالة تفعيل الفترة الزمنية (تنشيط أو إيقاف مؤقت).
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Laundry.ManageTimeSlots)]
    public virtual async Task SetActiveAsync(Guid id, bool isActive)
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _laundryRepository.WithDetailsAsync(l => l.WorkingHours, l => l.TimeSlots);
        var laundry = await AsyncExecuter.FirstOrDefaultAsync(query, l => l.TenantId == tenantId);

        if (laundry == null)
        {
            throw new EntityNotFoundException(typeof(Laundry), tenantId);
        }

        var slot = laundry.TimeSlots.FirstOrDefault(s => s.Id == id);
        if (slot == null)
        {
            throw new EntityNotFoundException(typeof(LaundryTimeSlot), id);
        }

        if (isActive)
        {
            var workingHour = laundry.WorkingHours.FirstOrDefault(w => w.DayOfWeek == slot.DayOfWeek);
            if (workingHour == null || !workingHour.IsOpen)
            {
                throw new BusinessException($"Cannot activate time slot on {slot.DayOfWeek} because the laundry is closed on this day.");
            }

            if (slot.StartTime < workingHour.OpenTime!.Value || slot.EndTime > workingHour.CloseTime!.Value)
            {
                throw new BusinessException($"Time slot [{slot.StartTime}-{slot.EndTime}] falls outside operating hours on {slot.DayOfWeek}.");
            }

            var hasOverlap = laundry.TimeSlots.Any(s =>
                s.Id != id &&
                s.DayOfWeek == slot.DayOfWeek &&
                s.SlotType == slot.SlotType &&
                s.IsActive &&
                slot.StartTime < s.EndTime &&
                slot.EndTime > s.StartTime);

            if (hasOverlap)
            {
                throw new BusinessException($"Activating time slot [{slot.StartTime}-{slot.EndTime}] for {slot.SlotType} on {slot.DayOfWeek} overlaps with an existing active time slot.");
            }
        }

        slot.SetActive(isActive);
        await _laundryRepository.UpdateAsync(laundry, autoSave: true);
    }

    private Guid EnsureCurrentTenant()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":TenantRequired",
                "Operating on laundry time slots requires an active tenant context.");
        }

        return CurrentTenant.Id.Value;
    }

    private static LaundryTimeSlotDto MapToDto(LaundryTimeSlot slot)
    {
        return new LaundryTimeSlotDto
        {
            Id = slot.Id,
            LaundryId = slot.LaundryId,
            DayOfWeek = slot.DayOfWeek,
            SlotType = slot.SlotType,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            IsActive = slot.IsActive
        };
    }
}
