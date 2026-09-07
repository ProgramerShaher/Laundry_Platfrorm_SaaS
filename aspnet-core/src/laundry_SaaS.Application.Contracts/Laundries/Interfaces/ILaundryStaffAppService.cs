using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Laundries;

/// <summary>
/// واجهة خدمة إدارة طاقم عمل المغسلة (Laundry Staff Management App Service).
/// تُتيح لمدير المغسلة إنشاء حسابات الموظفين، واستعراضهم، وتحديث ملفاتهم، وإلغاء تفعيلهم.
/// </summary>
public interface ILaundryStaffAppService : IApplicationService
{
    /// <summary>
    /// يسترجع قائمة موظفي المغسلة في شكل صفحات قابلة للترتيب والبحث.
    /// </summary>
    /// <param name="input">معايير التصفح والترتيب.</param>
    /// <returns>صفحة من عناصر موظفي المغسلة.</returns>
    Task<PagedResultDto<LaundryStaffListDto>> GetListAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// يسترجع التفاصيل الكاملة لملف موظف محدد بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف ملف الموظف.</param>
    /// <returns>بيانات ملف الموظف الكاملة.</returns>
    Task<LaundryStaffProfileDto> GetAsync(Guid id);

    /// <summary>
    /// يُنشئ مستخدم Identity جديد ويُنشئ ملف موظف مرتبط به في المغسلة الحالية.
    /// </summary>
    /// <param name="input">بيانات المستخدم والمسمى الوظيفي وكلمة المرور.</param>
    /// <returns>ملف الموظف المنشأ.</returns>
    Task<LaundryStaffProfileDto> CreateAsync(CreateLaundryStaffProfileInput input);

    /// <summary>
    /// يحدّث بيانات موظف مغسلة حالي.
    /// </summary>
    /// <param name="id">معرّف ملف الموظف.</param>
    /// <param name="input">البيانات الشخصية والوظيفية الجديدة.</param>
    /// <returns>ملف الموظف بعد التحديث.</returns>
    Task<LaundryStaffProfileDto> UpdateAsync(Guid id, UpdateLaundryStaffProfileInput input);

    /// <summary>
    /// يعطل أو يفعل ملف موظف المغسلة وحسابه في النظام (SetActive).
    /// </summary>
    /// <param name="id">معرّف ملف الموظف.</param>
    /// <param name="isActive">حالة التفعيل الجديدة.</param>
    Task SetActiveAsync(Guid id, bool isActive);
}
