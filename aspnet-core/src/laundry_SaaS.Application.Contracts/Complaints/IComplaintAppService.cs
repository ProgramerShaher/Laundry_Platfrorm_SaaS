using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Complaints;

/// <summary>
/// واجهة خدمة إدارة الشكاوى وخدمة العملاء.
/// تتيح للعميل تقديم الشكاوى وتتبعها، وتتيح لإدارة المغسلة مراجعتها وتسويتها وإقرار التعويضات المالية.
/// </summary>
public interface IComplaintAppService : IApplicationService
{
    /// <summary>
    /// استرجاع قائمة الشكاوى الخاصة بالعميل الحالي مع دعم الترقيم والترتيب.
    /// </summary>
    Task<PagedResultDto<ComplaintListDto>> GetMyComplaintsAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// استرجاع قائمة جميع الشكاوى الواردة للمغسلة مع دعم الترقيم والترتيب (مخصصة للمغسلة).
    /// </summary>
    Task<PagedResultDto<ComplaintListDto>> GetLaundryComplaintsAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// استرجاع التفاصيل الكاملة لشكوى محددة مع قرارات التسوية والمرفقات.
    /// </summary>
    Task<ComplaintDetailDto> GetAsync(Guid id);

    /// <summary>
    /// تقديم وإنشاء شكوى جديدة من قبل العميل بخصوص طلب محدد.
    /// </summary>
    Task<ComplaintDetailDto> CreateAsync(CreateComplaintInput input);

    /// <summary>
    /// إضافة مرفق إثباتي جديد (صورة، تقرير، مستند) إلى ملف الشكوى بواسطة المرجع السحابي المعتمد.
    /// </summary>
    /// <param name="complaintId">معرّف الشكوى المرتبطة.</param>
    /// <param name="input">البيانات الوصفية للمرفق ومرجع التخزين السحابي.</param>
    /// <returns>بيانات المرفق المسجل في ملف الشكوى.</returns>
    Task<ComplaintAttachmentDto> AddAttachmentAsync(Guid complaintId, AddComplaintAttachmentInput input);

    /// <summary>
    /// بدء مراجعة الشكوى والتحقيق فيها من قبل موظف خدمة العملاء بالمغسلة (الانتقال إلى InReview).
    /// </summary>
    /// <param name="id">معرّف الشكوى.</param>
    Task ReviewAsync(Guid id);

    /// <summary>
    /// تسوية وحل الشكوى من قبل إدارة المغسلة وتحديد مبلغ التعويض المالي إن وجد (الانتقال إلى Resolved).
    /// </summary>
    Task<ComplaintDetailDto> ResolveAsync(Guid id, ResolveComplaintInput input);

    /// <summary>
    /// إغلاق الشكوى نهائياً بعد إتمام التسوية ورضا العميل (الانتقال إلى Closed).
    /// </summary>
    Task CloseAsync(Guid id);
}
