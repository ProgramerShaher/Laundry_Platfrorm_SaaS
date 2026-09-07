using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Catalog;

/// <summary>
/// واجهة خدمة إدارة كتالوج الخدمات والأسعار للمغسلة (Laundry Catalog Management App Service).
/// تُستخدم من قبل إدارة المغسلة لتعريف الأصناف والخدمات وجدول الأسعار المعتمد.
/// </summary>
public interface ILaundryCatalogAppService : IApplicationService
{
    // --- Item Types ---

    /// <summary>
    /// يسترجع قائمة أنواع قطع الملابس المسجلة في كتالوج المغسلة الحالية.
    /// </summary>
    /// <returns>قائمة أنواع قطع الملابس.</returns>
    Task<List<LaundryItemTypeDto>> GetItemTypesAsync();

    /// <summary>
    /// يسترجع تفاصيل نوع قطعة ملابس محدد بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف نوع القطعة.</param>
    /// <returns>تفاصيل نوع القطعة.</returns>
    Task<LaundryItemTypeDto> GetItemTypeAsync(Guid id);

    /// <summary>
    /// يُنشئ نوع قطعة ملابس جديد في الكتالوج.
    /// </summary>
    /// <param name="input">بيانات الصنف الجديد والكود والترتيب.</param>
    /// <returns>تفاصيل الصنف المنشأ.</returns>
    Task<LaundryItemTypeDto> CreateItemTypeAsync(CreateLaundryItemTypeInput input);

    /// <summary>
    /// يحدّث بيانات نوع قطعة ملابس موجود.
    /// </summary>
    /// <param name="id">معرّف نوع القطعة.</param>
    /// <param name="input">البيانات المحدثة.</param>
    /// <returns>تفاصيل الصنف بعد التحديث.</returns>
    Task<LaundryItemTypeDto> UpdateItemTypeAsync(Guid id, UpdateLaundryItemTypeInput input);

    /// <summary>
    /// يغير حالة تفعيل نوع قطعة الملابس في الكتالوج (تنشيط أو تعطيل).
    /// </summary>
    /// <param name="id">معرّف نوع القطعة.</param>
    /// <param name="isActive">القيمة الجديدة للتفعيل.</param>
    Task SetItemTypeActiveAsync(Guid id, bool isActive);

    /// <summary>
    /// يحذف نوع قطعة الملابس من الكتالوج (حذف ناعم Soft Delete).
    /// </summary>
    /// <param name="id">معرّف نوع القطعة.</param>
    Task DeleteItemTypeAsync(Guid id);

    // --- Services ---

    /// <summary>
    /// يسترجع قائمة خدمات الغسيل والمعالجة المعرفة في كتالوج المغسلة الحالية.
    /// </summary>
    /// <returns>قائمة خدمات المعالجة.</returns>
    Task<List<LaundryServiceDto>> GetServicesAsync();

    /// <summary>
    /// يسترجع تفاصيل خدمة معالجة محددة بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    /// <returns>تفاصيل الخدمة.</returns>
    Task<LaundryServiceDto> GetServiceAsync(Guid id);

    /// <summary>
    /// يُنشئ خدمة معالجة جديدة في الكتالوج.
    /// </summary>
    /// <param name="input">بيانات الخدمة والكود والترتيب.</param>
    /// <returns>تفاصيل الخدمة المنشأة.</returns>
    Task<LaundryServiceDto> CreateServiceAsync(CreateLaundryServiceInput input);

    /// <summary>
    /// يحدّث بيانات خدمة معالجة موجودة في الكتالوج.
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    /// <param name="input">البيانات المحدثة.</param>
    /// <returns>تفاصيل الخدمة بعد التحديث.</returns>
    Task<LaundryServiceDto> UpdateServiceAsync(Guid id, UpdateLaundryServiceInput input);

    /// <summary>
    /// يغير حالة تفعيل خدمة المعالجة في الكتالوج (تنشيط أو تعطيل).
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    /// <param name="isActive">القيمة الجديدة للتفعيل.</param>
    Task SetServiceActiveAsync(Guid id, bool isActive);

    /// <summary>
    /// يحذف خدمة المعالجة من الكتالوج (حذف ناعم Soft Delete).
    /// </summary>
    /// <param name="id">معرّف الخدمة.</param>
    Task DeleteServiceAsync(Guid id);

    // --- Pricing ---

    /// <summary>
    /// يسترجع جدول الأسعار الكامل والمعتمد لجميع أصناف وخدمات المغسلة الحالية.
    /// </summary>
    /// <returns>قائمة بسجلات الأسعار الفعالة.</returns>
    Task<List<ServicePriceDto>> GetServicePricesAsync();

    /// <summary>
    /// يسترجع تفاصيل سجل تسعير محدد بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف سجل التسعير.</param>
    /// <returns>تفاصيل سجل التسعير.</returns>
    Task<ServicePriceDto> GetServicePriceAsync(Guid id);

    /// <summary>
    /// يحدد أو يحدّث سعر خدمة معينة لقطعة ملابس محددة.
    /// </summary>
    /// <param name="input">معرّفات الصنف والخدمة والسعر الجديد بالريال السعودي.</param>
    /// <returns>سجل التسعير المحدث.</returns>
    Task<ServicePriceDto> SetServicePriceAsync(SetServicePriceInput input);

    /// <summary>
    /// يغير حالة تفعيل سجل التسعير (تنشيط أو تعطيل).
    /// </summary>
    /// <param name="id">معرّف سجل التسعير.</param>
    /// <param name="isActive">القيمة الجديدة للتفعيل.</param>
    Task SetServicePriceActiveAsync(Guid id, bool isActive);

    /// <summary>
    /// يحذف سجل تسعير من الكتالوج (حذف ناعم Soft Delete).
    /// </summary>
    /// <param name="id">معرّف سجل التسعير.</param>
    Task DeleteServicePriceAsync(Guid id);

    /// <summary>
    /// يسترجع قائمة خيارات أصناف الملابس للاستخدام في القوائم المنسدلة.
    /// </summary>
    /// <returns>قائمة الخيارات السريعة للأصناف.</returns>
    Task<List<ItemTypeLookupDto>> GetItemTypeLookupAsync();
}
