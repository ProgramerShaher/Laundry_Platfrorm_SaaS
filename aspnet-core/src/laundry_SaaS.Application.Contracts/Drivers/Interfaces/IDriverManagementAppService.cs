using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using laundry_SaaS.PickupDelivery;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Drivers;

/// <summary>
/// واجهة خدمة إدارة السائقين التابعين للمغسلة (Driver Management App Service).
/// تُتيح لإدارة المغسلة تسجيل السائقين، واستعراضهم، وتحديث ملفاتهم، ومتابعة حالات توفرهم، وإسناد مهام الاستلام والتوصيل اللوجستية إليهم.
/// </summary>
public interface IDriverManagementAppService : IApplicationService
{
    /// <summary>
    /// يسترجع قائمة السائقين في شكل صفحات مفهرسة مع دعم التصفية والبحث.
    /// </summary>
    /// <param name="input">معايير التصفح والترتيب.</param>
    /// <returns>صفحة من عناصر قائمة السائقين.</returns>
    Task<PagedResultDto<DriverListDto>> GetListAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// يسترجع التفاصيل الكاملة لملف سائق محدد بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف ملف السائق.</param>
    /// <returns>بيانات ملف السائق والمركبة والنشاط.</returns>
    Task<DriverProfileDto> GetAsync(Guid id);

    /// <summary>
    /// يُنشئ حساب مستخدم جديد وملف سائق مرتبط به في المغسلة الحالية.
    /// </summary>
    /// <param name="input">بيانات السائق الشخصية وتفاصيل المركبة وكلمة المرور.</param>
    /// <returns>ملف السائق المنشأ.</returns>
    Task<DriverProfileDto> CreateAsync(CreateDriverProfileInput input);

    /// <summary>
    /// يحدّث بيانات ملف السائق والمركبة.
    /// </summary>
    /// <param name="id">معرّف السائق.</param>
    /// <param name="input">البيانات الشخصية والمركبة المحدثة.</param>
    /// <returns>ملف السائق بعد التحديث.</returns>
    Task<DriverProfileDto> UpdateAsync(Guid id, UpdateDriverProfileInput input);

    /// <summary>
    /// يحدّث حالة توفر السائق لاستقبال مهام لوجستية جديدة (تفرغ / انشغال).
    /// </summary>
    /// <param name="id">معرّف السائق.</param>
    /// <param name="input">حالة التوفر الجديدة.</param>
    Task UpdateAvailabilityAsync(Guid id, UpdateDriverAvailabilityInput input);

    /// <summary>
    /// يسترجع قائمة اختيار السائقين المتاحين للتعيين السريع على المهام.
    /// </summary>
    /// <returns>قائمة الخيارات السريعة للسائقين المتاحين.</returns>
    Task<List<DriverLookupDto>> GetDriverLookupAsync();

    /// <summary>
    /// إسناد مهمة استلام لسائق من قبل إدارة المغسلة.
    /// </summary>
    /// <param name="id">معرّف مهمة الاستلام.</param>
    /// <param name="input">معرّف السائق المحدد.</param>
    Task AssignPickupDriverAsync(Guid id, AssignDriverTaskInput input);

    /// <summary>
    /// إسناد مهمة توصيل لسائق من قبل إدارة المغسلة.
    /// </summary>
    /// <param name="id">معرّف مهمة التوصيل.</param>
    /// <param name="input">معرّف السائق المحدد.</param>
    Task AssignDeliveryDriverAsync(Guid id, AssignDriverTaskInput input);
}
