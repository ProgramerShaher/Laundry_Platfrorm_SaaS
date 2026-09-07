using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Laundries;

/// <summary>
/// واجهة خدمة إدارة الملف التعريفي والتشغيلي للمغسلة (Laundry Profile Management App Service).
/// تُستخدم من قبل مدير المغسلة لإدارة بيانات المغسلة، وساعات العمل، ونطاق التغطية، ورسوم التوصيل.
/// </summary>
public interface ILaundryProfileAppService : IApplicationService
{
    /// <summary>
    /// يسترجع الملف التعريفي والتشغيلي الكامل للمغسلة الحالية (بناءً على جلسة المستأجر CurrentTenant).
    /// </summary>
    /// <returns>بيانات المغسلة وساعات العمل ونطاق التغطية.</returns>
    Task<LaundryDetailDto> GetProfileAsync();

    /// <summary>
    /// يحدّث البيانات العامة للمغسلة والرسوم والحد الأدنى للطلب.
    /// </summary>
    /// <param name="input">البيانات الجديدة وختم التزامن.</param>
    /// <returns>بيانات المغسلة بعد التحديث.</returns>
    Task<LaundryDetailDto> UpdateProfileAsync(UpdateLaundryProfileInput input);

    /// <summary>
    /// يضبط نطاق التغطية الجغرافية والتشغيلية للمغسلة (المركز، نصف القطر، والحد الأدنى).
    /// </summary>
    /// <param name="input">بيانات نطاق التغطية الجديد.</param>
    Task SetCoverageAreaAsync(SetCoverageAreaInput input);

    /// <summary>
    /// يضبط أو يحدّث جدول ساعات العمل ليوم محدد من أيام الأسبوع، مع التحقق من عدم تعارضه مع الفترات النشطة.
    /// </summary>
    /// <param name="input">بيانات ساعات عمل اليوم ومواعيد الفتح والإغلاق.</param>
    /// <returns>كيان ساعات عمل اليوم المحدث.</returns>
    Task<LaundryWorkingHourDto> SetWorkingHourAsync(SetLaundryWorkingHourInput input);

    /// <summary>
    /// يسترجع قائمة ساعات العمل لكافة أيام الأسبوع السبعة للمغسلة الحالية.
    /// </summary>
    /// <returns>قائمة ساعات العمل لجميع الأيام.</returns>
    Task<List<LaundryWorkingHourDto>> GetWorkingHoursAsync();
}
