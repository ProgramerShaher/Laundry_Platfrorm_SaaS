using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using laundry_SaaS.Permissions;

namespace laundry_SaaS.Catalog;

/// <summary>
/// تطبيق خدمة إدارة كتالوج الأصناف والخدمات والأسعار للمغسلة (Laundry Catalog Management App Service).
/// يتيح لإدارة المغسلة إدارة أنواع قطع الملابس، وخدمات الغسيل والمعالجة، وجدول تسعير المصفوفة
/// المعتمد لكل صنف وخدمة مع تطبيق مبادئ العزل الصارم للمستأجرين وضمان تفرد الأكواد والأسعار.
/// </summary>
[Authorize(laundry_SaaSPermissions.Catalog.Default)]
public class LaundryCatalogAppService : laundry_SaaSAppService, ILaundryCatalogAppService
{
    private readonly IRepository<LaundryItemType, Guid> _itemTypeRepository;
    private readonly IRepository<LaundryService, Guid> _serviceRepository;
    private readonly IRepository<ServicePrice, Guid> _servicePriceRepository;

    public LaundryCatalogAppService(
        IRepository<LaundryItemType, Guid> itemTypeRepository,
        IRepository<LaundryService, Guid> serviceRepository,
        IRepository<ServicePrice, Guid> servicePriceRepository)
    {
        _itemTypeRepository = itemTypeRepository;
        _serviceRepository = serviceRepository;
        _servicePriceRepository = servicePriceRepository;
    }

    // ==========================================
    // 1. Item Types Operations
    // ==========================================

    /// <summary>
    /// يسترجع قائمة جميع أنواع قطع الملابس المسجلة في كتالوج المغسلة الحالية.
    /// </summary>
    /// <returns>قائمة أنواع قطع الملابس.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<List<LaundryItemTypeDto>> GetItemTypesAsync()
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _itemTypeRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(query.Where(i => i.TenantId == tenantId).OrderBy(i => i.Name));

        return items.Select(i => MapToItemTypeDto(i)).ToList();
    }

    /// <summary>
    /// يسترجع تفاصيل نوع قطعة ملابس محدد بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف نوع القطعة.</param>
    /// <returns>تفاصيل نوع القطعة.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<LaundryItemTypeDto> GetItemTypeAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var item = await _itemTypeRepository.FindAsync(id);
        if (item == null || item.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryItemType), id);
        }

        return MapToItemTypeDto(item);
    }

    /// <summary>
    /// يُنشئ صنف قطعة ملابس جديد في الكتالوج بعد التحقق من عدم تكرار الرمز التعريفي (Code).
    /// </summary>
    /// <param name="input">بيانات الصنف الجديد.</param>
    /// <returns>تفاصيل الصنف المنشأ.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageItemTypes)]
    public virtual async Task<LaundryItemTypeDto> CreateItemTypeAsync(CreateLaundryItemTypeInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var code = input.Code.Trim();
        var existing = await _itemTypeRepository.FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Code == code);
        if (existing != null)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":DuplicateItemCode",
                $"An item type with code '{input.Code}' already exists in this laundry catalog.");
        }

        var item = new LaundryItemType(
            GuidGenerator.Create(),
            tenantId,
            input.Name,
            code,
            input.Description,
            input.IsActive);

        await _itemTypeRepository.InsertAsync(item, autoSave: true);

        return MapToItemTypeDto(item, input.DisplayOrder);
    }

    /// <summary>
    /// يحدّث بيانات صنف ملابس موجود في الكتالوج.
    /// </summary>
    /// <param name="id">معرّف الصنف.</param>
    /// <param name="input">البيانات الجديدة.</param>
    /// <returns>تفاصيل الصنف المحدث.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageItemTypes)]
    public virtual async Task<LaundryItemTypeDto> UpdateItemTypeAsync(Guid id, UpdateLaundryItemTypeInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var item = await _itemTypeRepository.FindAsync(id);
        if (item == null || item.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryItemType), id);
        }

        item.SetName(input.Name);
        item.SetActive(input.IsActive);

        await _itemTypeRepository.UpdateAsync(item, autoSave: true);

        return MapToItemTypeDto(item, input.DisplayOrder);
    }

    /// <summary>
    /// يغير حالة تفعيل نوع قطعة الملابس في الكتالوج (تنشيط أو تعطيل).
    /// </summary>
    /// <param name="id">معرّف نوع القطعة.</param>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageItemTypes)]
    public virtual async Task SetItemTypeActiveAsync(Guid id, bool isActive)
    {
        var tenantId = EnsureCurrentTenant();

        var item = await _itemTypeRepository.FindAsync(id);
        if (item == null || item.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryItemType), id);
        }

        item.SetActive(isActive);
        await _itemTypeRepository.UpdateAsync(item, autoSave: true);
    }

    /// <summary>
    /// يحذف صنف ملابس حذفاً ناعماً (Soft Delete) بعد التأكد من عدم ارتباط أسعار نشطة به.
    /// </summary>
    /// <param name="id">معرّف الصنف.</param>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageItemTypes)]
    public virtual async Task DeleteItemTypeAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var item = await _itemTypeRepository.FindAsync(id);
        if (item == null || item.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryItemType), id);
        }

        var hasActivePrices = await _servicePriceRepository.AnyAsync(p =>
            p.TenantId == tenantId &&
            p.LaundryItemTypeId == id &&
            p.IsActive);

        if (hasActivePrices)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":ActivePricesExist",
                "Cannot delete this item type because it has active prices configured. Please deactivate or delete associated prices first.");
        }

        await _itemTypeRepository.DeleteAsync(item, autoSave: true);
    }

    /// <summary>
    /// يسترجع قائمة خيارات أصناف الملابس النشطة للاستخدام في القوائم المنسدلة وشاشات الإدخال السريع.
    /// </summary>
    /// <returns>قائمة مبسطة بالمعرفات والأسماء والأكواد.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<List<ItemTypeLookupDto>> GetItemTypeLookupAsync()
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _itemTypeRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(query.Where(i => i.TenantId == tenantId && i.IsActive).OrderBy(i => i.Name));

        return items.Select(i => new ItemTypeLookupDto
        {
            Id = i.Id,
            Name = i.Name,
            Code = i.Code
        }).ToList();
    }

    // ==========================================
    // 2. Services Operations
    // ==========================================

    /// <summary>
    /// يسترجع قائمة خدمات المعالجة والغسيل المعرفة في كتالوج المغسلة الحالية.
    /// </summary>
    /// <returns>قائمة خدمات المعالجة.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<List<LaundryServiceDto>> GetServicesAsync()
    {
        var tenantId = EnsureCurrentTenant();

        var query = await _serviceRepository.GetQueryableAsync();
        var services = await AsyncExecuter.ToListAsync(query.Where(s => s.TenantId == tenantId).OrderBy(s => s.Name));

        return services.Select(s => MapToServiceDto(s)).ToList();
    }

    /// <summary>
    /// يسترجع تفاصيل خدمة معالجة محددة بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    /// <returns>تفاصيل الخدمة.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<LaundryServiceDto> GetServiceAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var service = await _serviceRepository.FindAsync(id);
        if (service == null || service.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryService), id);
        }

        return MapToServiceDto(service);
    }

    /// <summary>
    /// يُنشئ خدمة معالجة جديدة في الكتالوج بعد التحقق من عدم تكرار كود الخدمة.
    /// </summary>
    /// <param name="input">بيانات الخدمة الجديدة.</param>
    /// <returns>تفاصيل الخدمة المنشأة.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageServices)]
    public virtual async Task<LaundryServiceDto> CreateServiceAsync(CreateLaundryServiceInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var code = input.Code.Trim();
        var existing = await _serviceRepository.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Code == code);
        if (existing != null)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":DuplicateServiceCode",
                $"A service with code '{input.Code}' already exists in this laundry catalog.");
        }

        var service = new LaundryService(
            GuidGenerator.Create(),
            tenantId,
            input.Name,
            code,
            input.Description,
            input.IsActive);

        await _serviceRepository.InsertAsync(service, autoSave: true);

        return MapToServiceDto(service, input.DisplayOrder);
    }

    /// <summary>
    /// يحدّث بيانات خدمة معالجة موجودة في الكتالوج.
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    /// <param name="input">البيانات الجديدة.</param>
    /// <returns>تفاصيل الخدمة المحدثة.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageServices)]
    public virtual async Task<LaundryServiceDto> UpdateServiceAsync(Guid id, UpdateLaundryServiceInput input)
    {
        var tenantId = EnsureCurrentTenant();

        var service = await _serviceRepository.FindAsync(id);
        if (service == null || service.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryService), id);
        }

        service.SetName(input.Name);
        service.SetActive(input.IsActive);

        await _serviceRepository.UpdateAsync(service, autoSave: true);

        return MapToServiceDto(service, input.DisplayOrder);
    }

    /// <summary>
    /// يغير حالة تفعيل خدمة المعالجة في الكتالوج (تنشيط أو تعطيل).
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageServices)]
    public virtual async Task SetServiceActiveAsync(Guid id, bool isActive)
    {
        var tenantId = EnsureCurrentTenant();

        var service = await _serviceRepository.FindAsync(id);
        if (service == null || service.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryService), id);
        }

        service.SetActive(isActive);
        await _serviceRepository.UpdateAsync(service, autoSave: true);
    }

    /// <summary>
    /// يحذف خدمة معالجة حذفاً ناعماً بعد التأكد من عدم ارتباط أسعار نشطة بها.
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    [Authorize(laundry_SaaSPermissions.Catalog.ManageServices)]
    public virtual async Task DeleteServiceAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var service = await _serviceRepository.FindAsync(id);
        if (service == null || service.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryService), id);
        }

        var hasActivePrices = await _servicePriceRepository.AnyAsync(p =>
            p.TenantId == tenantId &&
            p.LaundryServiceId == id &&
            p.IsActive);

        if (hasActivePrices)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":ActivePricesExist",
                "Cannot delete this service because it has active prices configured. Please deactivate or delete associated prices first.");
        }

        await _serviceRepository.DeleteAsync(service, autoSave: true);
    }

    // ==========================================
    // 3. Matrix Service Pricing Operations
    // ==========================================

    /// <summary>
    /// يسترجع جدول الأسعار الكامل والمعتمد للمغسلة لجميع أصناف وخدمات الكتالوج.
    /// </summary>
    /// <returns>قائمة بسجلات الأسعار المعتمدة مع أسماء الأصناف والخدمات.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<List<ServicePriceDto>> GetServicePricesAsync()
    {
        var tenantId = EnsureCurrentTenant();

        var priceQuery = await _servicePriceRepository.GetQueryableAsync();
        var prices = await AsyncExecuter.ToListAsync(priceQuery.Where(p => p.TenantId == tenantId && p.IsActive));

        if (!prices.Any())
        {
            return new List<ServicePriceDto>();
        }

        var itemTypeIds = prices.Select(p => p.LaundryItemTypeId).Distinct().ToList();
        var serviceIds = prices.Select(p => p.LaundryServiceId).Distinct().ToList();

        var itemQuery = await _itemTypeRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(itemQuery.Where(i => itemTypeIds.Contains(i.Id)));
        var itemDict = items.ToDictionary(i => i.Id, i => i.Name);

        var servQuery = await _serviceRepository.GetQueryableAsync();
        var services = await AsyncExecuter.ToListAsync(servQuery.Where(s => serviceIds.Contains(s.Id)));
        var serviceDict = services.ToDictionary(s => s.Id, s => s.Name);

        return prices.Select(p => new ServicePriceDto
        {
            Id = p.Id,
            LaundryItemTypeId = p.LaundryItemTypeId,
            ItemTypeName = itemDict.TryGetValue(p.LaundryItemTypeId, out var iName) ? iName : string.Empty,
            LaundryServiceId = p.LaundryServiceId,
            ServiceName = serviceDict.TryGetValue(p.LaundryServiceId, out var sName) ? sName : string.Empty,
            Price = p.Price
        }).ToList();
    }

    /// <summary>
    /// يسترجع تفاصيل سجل تسعير محدد بالمعرّف مع أسماء الصنف والخدمة المقترنين به.
    /// </summary>
    /// <param name="id">معرّف سجل التسعير.</param>
    /// <returns>تفاصيل سجل التسعير.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.Default)]
    public virtual async Task<ServicePriceDto> GetServicePriceAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var price = await _servicePriceRepository.FindAsync(id);
        if (price == null || price.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(ServicePrice), id);
        }

        var item = await _itemTypeRepository.FindAsync(price.LaundryItemTypeId);
        var service = await _serviceRepository.FindAsync(price.LaundryServiceId);

        return new ServicePriceDto
        {
            Id = price.Id,
            LaundryItemTypeId = price.LaundryItemTypeId,
            ItemTypeName = item?.Name ?? string.Empty,
            LaundryServiceId = price.LaundryServiceId,
            ServiceName = service?.Name ?? string.Empty,
            Price = price.Price
        };
    }

    /// <summary>
    /// يحدد أو يحدّث سعر خدمة معينة على قطعة ملابس محددة (Upsert Service Price).
    /// </summary>
    /// <param name="input">معرفات الصنف والخدمة والسعر بالريال السعودي.</param>
    /// <returns>سجل التسعير المحدث.</returns>
    [Authorize(laundry_SaaSPermissions.Catalog.ManagePrices)]
    public virtual async Task<ServicePriceDto> SetServicePriceAsync(SetServicePriceInput input)
    {
        var tenantId = EnsureCurrentTenant();

        if (input.Price < 0)
        {
            throw new BusinessException("Price must be greater than or equal to zero.");
        }

        var item = await _itemTypeRepository.FindAsync(input.LaundryItemTypeId);
        if (item == null || item.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryItemType), input.LaundryItemTypeId);
        }

        var service = await _serviceRepository.FindAsync(input.LaundryServiceId);
        if (service == null || service.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(LaundryService), input.LaundryServiceId);
        }

        var existingPrice = await _servicePriceRepository.FirstOrDefaultAsync(p =>
            p.TenantId == tenantId &&
            p.LaundryItemTypeId == input.LaundryItemTypeId &&
            p.LaundryServiceId == input.LaundryServiceId);

        if (existingPrice != null)
        {
            existingPrice.SetPrice(input.Price);
            existingPrice.SetActive(true);
            await _servicePriceRepository.UpdateAsync(existingPrice, autoSave: true);

            return new ServicePriceDto
            {
                Id = existingPrice.Id,
                LaundryItemTypeId = existingPrice.LaundryItemTypeId,
                ItemTypeName = item.Name,
                LaundryServiceId = existingPrice.LaundryServiceId,
                ServiceName = service.Name,
                Price = existingPrice.Price
            };
        }

        var newPrice = new ServicePrice(
            GuidGenerator.Create(),
            tenantId,
            input.LaundryItemTypeId,
            input.LaundryServiceId,
            input.Price,
            isActive: true);

        await _servicePriceRepository.InsertAsync(newPrice, autoSave: true);

        return new ServicePriceDto
        {
            Id = newPrice.Id,
            LaundryItemTypeId = newPrice.LaundryItemTypeId,
            ItemTypeName = item.Name,
            LaundryServiceId = newPrice.LaundryServiceId,
            ServiceName = service.Name,
            Price = newPrice.Price
        };
    }

    /// <summary>
    /// يغير حالة تفعيل سجل التسعير (تنشيط أو تعطيل).
    /// </summary>
    /// <param name="id">معرّف سجل التسعير.</param>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    [Authorize(laundry_SaaSPermissions.Catalog.ManagePrices)]
    public virtual async Task SetServicePriceActiveAsync(Guid id, bool isActive)
    {
        var tenantId = EnsureCurrentTenant();

        var price = await _servicePriceRepository.FindAsync(id);
        if (price == null || price.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(ServicePrice), id);
        }

        price.SetActive(isActive);
        await _servicePriceRepository.UpdateAsync(price, autoSave: true);
    }

    /// <summary>
    /// يحذف سجل تسعير من الكتالوج حذفاً ناعماً.
    /// </summary>
    /// <param name="id">معرّف سجل التسعير.</param>
    [Authorize(laundry_SaaSPermissions.Catalog.ManagePrices)]
    public virtual async Task DeleteServicePriceAsync(Guid id)
    {
        var tenantId = EnsureCurrentTenant();

        var price = await _servicePriceRepository.FindAsync(id);
        if (price == null || price.TenantId != tenantId)
        {
            throw new EntityNotFoundException(typeof(ServicePrice), id);
        }

        await _servicePriceRepository.DeleteAsync(price, autoSave: true);
    }

    // ==========================================
    // Private Helpers
    // ==========================================

    private static LaundryItemTypeDto MapToItemTypeDto(LaundryItemType item)
    {
        return MapToItemTypeDto(item, 0);
    }

    private static LaundryItemTypeDto MapToItemTypeDto(LaundryItemType item, int displayOrder)
    {
        return new LaundryItemTypeDto
        {
            Id = item.Id,
            Name = item.Name,
            Code = item.Code,
            Description = item.Description,
            DisplayOrder = displayOrder,
            IsActive = item.IsActive
        };
    }

    private static LaundryServiceDto MapToServiceDto(LaundryService service)
    {
        return MapToServiceDto(service, 0);
    }

    private static LaundryServiceDto MapToServiceDto(LaundryService service, int displayOrder)
    {
        return new LaundryServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Code = service.Code,
            Description = service.Description,
            DisplayOrder = displayOrder,
            IsActive = service.IsActive
        };
    }

    private Guid EnsureCurrentTenant()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":TenantRequired",
                "Catalog operations require an active tenant context.");
        }

        return CurrentTenant.Id.Value;
    }
}
