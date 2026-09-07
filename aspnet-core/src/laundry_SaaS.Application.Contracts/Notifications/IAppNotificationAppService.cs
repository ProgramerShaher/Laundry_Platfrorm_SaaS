using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Notifications;

/// <summary>
/// واجهة خدمة إشعارات التطبيق للمستخدمين (عملاء، موظفون، سائقون).
/// تتيح استعراض الإشعارات الشخصية، معرفة عدد غير المقروء منها، وتحديث حالة القراءة.
/// </summary>
public interface IAppNotificationAppService : IApplicationService
{
    /// <summary>
    /// استرجاع قائمة الإشعارات الخاصة بالمستخدم الحالي مع إمكانية الفلترة على غير المقروءة فقط والترقيم.
    /// </summary>
    Task<PagedResultDto<AppNotificationListDto>> GetMyNotificationsAsync(PagedAndSortedResultRequestDto input, bool? unreadOnly = null);

    /// <summary>
    /// استرجاع عدد الإشعارات غير المقروءة للمستخدم الحالي لعرض شارة التنبيهات (Badge Count).
    /// </summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>
    /// تمييز إشعار محدد كمقروء للمستخدم الحالي.
    /// </summary>
    Task MarkAsReadAsync(Guid id);

    /// <summary>
    /// تمييز كافة إشعارات المستخدم الحالي غير المقروءة كمقروءة دفعة واحدة.
    /// </summary>
    Task MarkAllAsReadAsync();
}
