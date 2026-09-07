using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using laundry_SaaS.Laundries;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Catalog;

/// <summary>
/// واجهة خدمة تصفح الكتالوج والمغاسل المخصصة لتطبيقات العملاء (Customer Catalog App Service).
/// تُتيح للعميل استكشاف المغاسل القريبة من موقعه، واستعراض الخدمات والأسعار، وفترات الاستلام المتاحة.
/// </summary>
public interface ICustomerCatalogAppService : IApplicationService
{
    /// <summary>
    /// يسترجع قائمة المغاسل النشطة القريبة من موقع العميل الجغرافي ضمن نصف قطر البحث المحدد مع حساب المسافات.
    /// </summary>
    /// <param name="input">إحداثيات العميل والمسافة القصوى للبحث.</param>
    /// <returns>قائمة المغاسل القريبة ورسوم التوصيل لكل منها.</returns>
    Task<List<LaundryNearbyListDto>> GetNearbyLaundriesAsync(GetNearbyLaundriesInput input);

    /// <summary>
    /// يسترجع الكتالوج المعتمد الكامل لمغسلة محددة متضمناً أصناف الملابس والخدمات والأسعار.
    /// </summary>
    /// <param name="laundryId">معرّف المغسلة المراد استعراض كتالوجها.</param>
    /// <returns>كتالوج المغسلة بالأسعار الفعالة.</returns>
    Task<CustomerCatalogDto> GetLaundryCatalogAsync(Guid laundryId);

    /// <summary>
    /// يسترجع الفترات الزمنية المتاحة لجدولة الاستلام (Pickup Slots) لمغسلة محددة في يوم محدد.
    /// </summary>
    /// <param name="laundryId">معرّف المغسلة.</param>
    /// <param name="pickupDate">تاريخ الاستلام المطلوب.</param>
    /// <returns>قائمة الفترات الزمنية المتاحة والصالحة للحجز في ذلك اليوم.</returns>
    Task<List<LaundryTimeSlotDto>> GetAvailablePickupSlotsAsync(Guid laundryId, DateOnly pickupDate);
}
