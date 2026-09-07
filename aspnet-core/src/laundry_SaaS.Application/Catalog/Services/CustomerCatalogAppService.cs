using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using laundry_SaaS.Common;
using laundry_SaaS.Laundries;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;

namespace laundry_SaaS.Catalog;

/// <summary>
/// خدمة تصفح الكتالوج والمغاسل المخصصة لتطبيقات العملاء (Customer Catalog Application Service).
/// <para>
/// تتيح للعميل المسجل استكشاف المغاسل القريبة من موقعه الجغرافي ضمن نطاقات التغطية المعتمدة،
/// واستعراض كتالوج الأصناف والخدمات والأسعار المعتمدة لمغسلة محددة،
/// والاطلاع على الفترات الزمنية المتاحة لجدولة الاستلام (Pickup Slots).
/// </para>
/// <para>
/// قواعد الأمان والعزل:
/// <list type="bullet">
/// <item><description>تخضع الخدمة لشرط المصادقة ([Authorize]).</description></item>
/// <item><description>قراءة بيانات المغاسل تتم عبر تعطيل ضيق ومحدد لفلتر المستأجرين (IMultiTenant) لاكتشاف المغاسل عبر المستأجرين المتعددين، دون تسريب أي بيانات تشغيلية داخلية للمستأجر.</description></item>
/// <item><description>يتم اشتقاق معرّف المستأجر (TenantId) حصرياً من سجل المغسلة الموثوق في الخادم، ويُحظر تماماً قبول أي TenantId من طرف العميل.</description></item>
/// <item><description>الأسعار المعروضة هنا هي استرشادية للعرض فقط، ولا تشكل أي سلطة تسعيرية قادمة من العميل، حيث يُعاد احتسابها وتأكيدها بدقة عند إنشاء الطلب.</description></item>
/// </list>
/// </para>
/// </summary>
[Authorize]
public class CustomerCatalogAppService : laundry_SaaSAppService, ICustomerCatalogAppService
{
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly IRepository<LaundryItemType, Guid> _itemTypeRepository;
    private readonly IRepository<LaundryService, Guid> _serviceRepository;
    private readonly IRepository<ServicePrice, Guid> _servicePriceRepository;
    private readonly IClock _clock;

    /// <summary>
    /// يُنشئ نسخة جديدة من خدمة كتالوج العميل مع حقن المستودعات المطلوبة ومزود الوقت الموثوق.
    /// </summary>
    /// <param name="laundryRepository">مستودع الجذر التجميعي للمغاسل.</param>
    /// <param name="itemTypeRepository">مستودع أصناف الملابس.</param>
    /// <param name="serviceRepository">مستودع أنواع خدمات الغسيل والكي.</param>
    /// <param name="servicePriceRepository">مستودع جدول أسعار الخدمات للأصناف.</param>
    /// <param name="clock">مزود الوقت المعتمد لتحديد التواريخ والأوقات بدقة وتجنب DateTime.Now غير المتحكم بها.</param>
    public CustomerCatalogAppService(
        IRepository<Laundry, Guid> laundryRepository,
        IRepository<LaundryItemType, Guid> itemTypeRepository,
        IRepository<LaundryService, Guid> serviceRepository,
        IRepository<ServicePrice, Guid> servicePriceRepository,
        IClock clock)
    {
        _laundryRepository = laundryRepository;
        _itemTypeRepository = itemTypeRepository;
        _serviceRepository = serviceRepository;
        _servicePriceRepository = servicePriceRepository;
        _clock = clock;
    }

    /// <summary>
    /// يسترجع قائمة المغاسل النشطة والمستقبلة للطلبات التي يقع موقع العميل المحدد ضمن نطاق تغطيتها الجغرافية (<see cref="CoverageArea"/>).
    /// تُفرز النتائج تصاعدياً حسب المسافة الأقرب بالكيلومتر.
    /// </summary>
    /// <param name="input">بيانات موقع العميل الجغرافي والمسافة القصوى للبحث.</param>
    /// <returns>قائمة بالمغاسل القريبة المؤهلة مع المسافة المقدرة ورسوم التوصيل والحد الأدنى للطلب.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا كانت إحداثيات العميل أو مسافة البحث غير صحيحة.</exception>
    public virtual async Task<List<LaundryNearbyListDto>> GetNearbyLaundriesAsync(GetNearbyLaundriesInput input)
    {
        Check.NotNull(input, nameof(input));
        GeoLocationValidator.ValidateCoordinates(input.Latitude, input.Longitude);

        if (input.MaxDistanceKm <= 0)
        {
            throw new BusinessException("MaxDistanceKm must be greater than zero.");
        }

        // استعلام عبر المستأجرين لاسترجاع المغاسل النشطة والمستقبلة للطلبات وغير المحذوفة فقط
        List<Laundry> candidateLaundries;
        using (DataFilter.Disable<IMultiTenant>())
        {
            var query = await _laundryRepository.GetQueryableAsync();
            candidateLaundries = await AsyncExecuter.ToListAsync(
                query.Where(l => l.IsActive && l.AcceptingOrders && !l.IsDeleted)
            );
        }

        var results = new List<LaundryNearbyListDto>();

        foreach (var laundry in candidateLaundries)
        {
            if (laundry.CoverageArea == null)
            {
                continue;
            }

            // حساب المسافة بين موقع العميل ومركز نطاق تغطية المغسلة للتحقق الحصري من أهلية التوصيل
            var coverageDistanceKm = HaversineDistanceCalculator.CalculateDistanceKm(
                input.Latitude,
                input.Longitude,
                laundry.CoverageArea.CenterLatitude,
                laundry.CoverageArea.CenterLongitude
            );

            // استبعاد المغسلة إذا كان العميل خارج نصف قطر التغطية المعتمد للمغسلة
            if (coverageDistanceKm > laundry.CoverageArea.DeliveryRadiusKm)
            {
                continue;
            }

            // حساب المسافة الفعلية بين موقع العميل والموقع الجغرافي الحقيقي للمغسلة
            var physicalLaundryDistanceKm = HaversineDistanceCalculator.CalculateDistanceKm(
                input.Latitude,
                input.Longitude,
                laundry.Latitude,
                laundry.Longitude
            );

            // تطبيق قيد أقصى مسافة بحث للعميل (MaxDistanceKm) على المسافة الفعلية للمغسلة
            if (physicalLaundryDistanceKm > input.MaxDistanceKm)
            {
                continue;
            }

            results.Add(new LaundryNearbyListDto
            {
                Id = laundry.Id,
                Name = laundry.Name,
                Description = laundry.Description,
                LogoBlobName = laundry.LogoBlobName,
                DistanceKm = Math.Round(physicalLaundryDistanceKm, 2),
                DeliveryFee = laundry.DeliveryFee,
                // الحد الأدنى للطلب المعتمد حصرياً لقرارات الأعمال والواجهة في MVP هو Laundry.MinimumOrderAmount
                MinimumOrderAmount = laundry.MinimumOrderAmount,
                EstimatedProcessingHours = laundry.EstimatedProcessingHours,
                AcceptingOrders = laundry.AcceptingOrders
            });
        }

        // فرز المغاسل حسب المسافة تصاعدياً (الأقرب أولاً)
        return results.OrderBy(r => r.DistanceKm).ToList();
    }

    /// <summary>
    /// يسترجع كتالوج الأصناف والخدمات والأسعار المعتمدة لمغسلة معينة بعد التحقق من نشاطها واستقبالها للطلبات،
    /// مع عزل تام لبيانات المستأجر وضمان إرجاع التوليفات التي تمتلك سعراً نشطاً فقط.
    /// </summary>
    /// <param name="laundryId">معرّف المغسلة المراد جلب كتالوجها.</param>
    /// <returns>كائن بيانات كتالوج المغسلة <see cref="CustomerCatalogDto"/>.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف المغسلة فارغاً.</exception>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يتم العثور على المغسلة أو كانت معطلة أو لا تستقبل طلبات.</exception>
    public virtual async Task<CustomerCatalogDto> GetLaundryCatalogAsync(Guid laundryId)
    {
        if (laundryId == Guid.Empty)
        {
            throw new BusinessException("LaundryId must not be empty.");
        }

        Laundry? laundry;
        using (DataFilter.Disable<IMultiTenant>())
        {
            laundry = await _laundryRepository.FindAsync(laundryId);
        }

        if (laundry == null || !laundry.IsActive || !laundry.AcceptingOrders)
        {
            throw new EntityNotFoundException(typeof(Laundry), laundryId);
        }

        var tenantId = laundry.TenantId;

        // تنفيذ استعلامات محددة ومحدودة العدد (3 استعلامات بدلاً من N+1) ومحصورة بدقة بالمستأجر المشتق
        List<LaundryItemType> itemTypes;
        List<LaundryService> services;
        List<ServicePrice> servicePrices;

        using (DataFilter.Disable<IMultiTenant>())
        {
            var itemTypesQuery = await _itemTypeRepository.GetQueryableAsync();
            itemTypes = await AsyncExecuter.ToListAsync(
                itemTypesQuery.Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            );

            var servicesQuery = await _serviceRepository.GetQueryableAsync();
            services = await AsyncExecuter.ToListAsync(
                servicesQuery.Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            );

            var pricesQuery = await _servicePriceRepository.GetQueryableAsync();
            servicePrices = await AsyncExecuter.ToListAsync(
                pricesQuery.Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            );
        }

        var serviceDict = services.ToDictionary(s => s.Id);
        var pricesByItemType = servicePrices
            .GroupBy(p => p.LaundryItemTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var catalogItems = new List<CatalogItemDto>();

        foreach (var itemType in itemTypes.OrderBy(i => i.Name))
        {
            if (!pricesByItemType.TryGetValue(itemType.Id, out var itemPrices))
            {
                continue;
            }

            var availableServices = new List<CatalogServicePriceDto>();

            foreach (var price in itemPrices.OrderBy(p => p.Price))
            {
                if (serviceDict.TryGetValue(price.LaundryServiceId, out var service))
                {
                    availableServices.Add(new CatalogServicePriceDto
                    {
                        ServiceId = service.Id,
                        ServiceName = service.Name,
                        ServiceCode = service.Code,
                        Price = price.Price
                    });
                }
            }

            // لا يُعرض الصنف إلا إذا كان لديه خدمة واحدة على الأقل مسعرة ونشطة
            if (availableServices.Count > 0)
            {
                catalogItems.Add(new CatalogItemDto
                {
                    ItemTypeId = itemType.Id,
                    ItemTypeName = itemType.Name,
                    ItemTypeCode = itemType.Code,
                    Description = itemType.Description,
                    AvailableServices = availableServices
                });
            }
        }

        return new CustomerCatalogDto
        {
            LaundryId = laundry.Id,
            LaundryName = laundry.Name,
            Description = laundry.Description,
            DeliveryFee = laundry.DeliveryFee,
            // الحد الأدنى للطلب المعتمد حصرياً لقرارات الأعمال والواجهة في MVP هو Laundry.MinimumOrderAmount
            MinimumOrderAmount = laundry.MinimumOrderAmount,
            EstimatedProcessingHours = laundry.EstimatedProcessingHours,
            Items = catalogItems
        };
    }

    /// <summary>
    /// يسترجع الفترات الزمنية المتاحة لجدولة الاستلام (Pickup Slots) لمغسلة محددة في تاريخ معين،
    /// مع التحقق من كون اليوم مفتوحاً وضمن ساعات العمل الرسمية للمغسلة، ومطابقة قيود الأهلية لقواعد النطاق ValidateAndGetPickupSlot.
    /// </summary>
    /// <param name="laundryId">معرّف المغسلة.</param>
    /// <param name="pickupDate">تاريخ الاستلام المراد الاستعلام عن فتراته.</param>
    /// <returns>قائمة الفترات الزمنية المتاحة والصالحة للحجز في هذا التاريخ.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا كان معرّف المغسلة فارغاً أو كان تاريخ الاستلام في الماضي.</exception>
    /// <exception cref="EntityNotFoundException">يتم رميها إذا لم يتم العثور على المغسلة أو كانت معطلة أو لا تستقبل طلبات.</exception>
    public virtual async Task<List<LaundryTimeSlotDto>> GetAvailablePickupSlotsAsync(Guid laundryId, DateOnly pickupDate)
    {
        if (laundryId == Guid.Empty)
        {
            throw new BusinessException("LaundryId must not be empty.");
        }

        Laundry? laundry;
        using (DataFilter.Disable<IMultiTenant>())
        {
            var query = await _laundryRepository.WithDetailsAsync(l => l.TimeSlots, l => l.WorkingHours);
            laundry = await AsyncExecuter.FirstOrDefaultAsync(query.Where(l => l.Id == laundryId));
        }

        if (laundry == null || !laundry.IsActive || !laundry.AcceptingOrders)
        {
            throw new EntityNotFoundException(typeof(Laundry), laundryId);
        }

        var currentDate = DateOnly.FromDateTime(_clock.Now);
        if (pickupDate < currentDate)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupDateInPast,
                $"Pickup date '{pickupDate}' is in the past. Current date is '{currentDate}'.");
        }

        var targetDay = pickupDate.DayOfWeek;
        var workingHour = laundry.WorkingHours.FirstOrDefault(w => w.DayOfWeek == targetDay);

        // إذا كانت المغسلة مغلقة رسمياً في هذا اليوم أو بدون ساعات عمل، يتم إرجاع قائمة فارغة
        if (workingHour == null || !workingHour.IsOpen || !workingHour.OpenTime.HasValue || !workingHour.CloseTime.HasValue)
        {
            return new List<LaundryTimeSlotDto>();
        }

        // استرجاع فترات الاستلام النشطة لليوم المستهدف والتي تقع بالكامل ضمن ساعات العمل الرسمية للمغسلة
        var candidateSlots = laundry.TimeSlots
            .Where(s => s.DayOfWeek == targetDay && s.SlotType == SlotType.Pickup && s.IsActive)
            .Where(s => s.StartTime >= workingHour.OpenTime.Value && s.EndTime <= workingHour.CloseTime.Value);

        // التحقق لليوم نفسه: استبعاد الفترات التي انتهى وقتها بالفعل (نفس قاعدة Laundry.ValidateAndGetPickupSlot)
        if (pickupDate == currentDate)
        {
            var currentTime = TimeOnly.FromDateTime(_clock.Now);
            candidateSlots = candidateSlots.Where(s => currentTime < s.EndTime);
        }

        return candidateSlots
            .OrderBy(s => s.StartTime)
            .Select(s => new LaundryTimeSlotDto
            {
                Id = s.Id,
                LaundryId = s.LaundryId,
                DayOfWeek = s.DayOfWeek,
                SlotType = s.SlotType,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsActive = s.IsActive
            })
            .ToList();
    }
}
