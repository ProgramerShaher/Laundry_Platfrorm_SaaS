using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.HostManagement;

/// <summary>
/// واجهة خدمة إدارة المضيف المركزي للمنصة (Host Platform Management App Service).
/// تُتيح لإدارة النظام المركزية إنشاء المستأجرين، واستعراض المغاسل، وإدارة دورة حياة المستأجر والمغسلة، ومتابعة مؤشرات الأداء.
/// </summary>
public interface ILaundryHostAppService : IApplicationService
{
    /// <summary>
    /// يُنشئ مستأجر مغسلة جديد في النظام مع إنشاء المستخدم المدير الأول وإعداد الكيان التشغيلي.
    /// </summary>
    /// <param name="input">بيانات المستأجر والمدير والمغسلة.</param>
    /// <returns>تفاصيل المغسلة المنشأة لإدارة المضيف.</returns>
    Task<LaundryHostDetailDto> CreateLaundryTenantAsync(CreateLaundryTenantInput input);

    /// <summary>
    /// يسترجع تفاصيل مغسلة محددة لإدارة المضيف بالمعرّف.
    /// </summary>
    /// <param name="id">المعرّف الفريد لكيان المغسلة.</param>
    /// <returns>بيانات المغسلة والمستأجر التشغيلية والإدارية.</returns>
    Task<LaundryHostDetailDto> GetAsync(Guid id);

    /// <summary>
    /// يسترجع قائمة مفهرسة ومقسمة لصفحات بجميع المغاسل المسجلة في المنصة.
    /// </summary>
    /// <param name="input">معايير الترتيب والتصفح.</param>
    /// <returns>صفحة من عناصر قائمة المغاسل.</returns>
    Task<PagedResultDto<LaundryHostListDto>> GetListAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// يغير حالة تفعيل المغسلة في المنصة (تنشيط أو إيقاف إداري).
    /// </summary>
    /// <param name="id">معرّف المغسلة.</param>
    /// <param name="isActive">القيمة الجديدة للتفعيل.</param>
    Task SetActiveStatusAsync(Guid id, bool isActive);

    /// <summary>
    /// ينشط مستأجر المغسلة ومغسلته تشغيلياً على مستوى منصة المضيف (Host Orchestration).
    /// </summary>
    /// <param name="id">معرّف المغسلة.</param>
    Task ActivateLaundryTenantAsync(Guid id);

    /// <summary>
    /// يوقف مؤقتاً مستأجر المغسلة ومغسلته عن العمل من قبل إدارة المضيف (Suspend).
    /// </summary>
    /// <param name="id">معرّف المغسلة.</param>
    Task SuspendLaundryTenantAsync(Guid id);

    /// <summary>
    /// يسترجع الإحصائيات العامة الشاملة للمنصة (أعداد، إيرادات، طلبات نشطة، شكاوى).
    /// </summary>
    /// <returns>كائن المؤشرات والإحصائيات الشاملة للمنصة.</returns>
    Task<PlatformStatisticsDto> GetPlatformStatisticsAsync();
}
