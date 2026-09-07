using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Orders;

/// <summary>
/// واجهة خدمة إدارة طلبات المغسلة لطاقم العمل والإدارة (Laundry Orders Management App Service).
/// تُتيح استعراض الطلبات، وتطبيق التعديلات المالية، والإلغاء الإداري، وقيادة المراحل التشغيلية بأسلوب Command-oriented صريح.
/// </summary>
public interface ILaundryOrderAppService : IApplicationService
{
    /// <summary>
    /// يسترجع قائمة طلبات المغسلة مفهرسة ومقسمة لصفحات وفق معايير التصفية والبحث المحددة.
    /// </summary>
    /// <param name="input">معايير التصفية والبحث والترتيب.</param>
    /// <returns>صفحة من عناصر قائمة طلبات المغسلة.</returns>
    Task<PagedResultDto<LaundryOrderListDto>> GetListAsync(GetLaundryOrdersFilterInput input);

    /// <summary>
    /// يسترجع التفاصيل التشغيلية الكاملة لطلب محدد بالمغسلة الحالية مع التحقق من عزل المستأجر.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <returns>بيانات الطلب التشغيلية الشاملة.</returns>
    Task<LaundryOrderDetailDto> GetAsync(Guid id);

    /// <summary>
    /// إلغاء إداري للطلب من قبل طاقم أو إدارة المغسلة مع توثيق السبب الإلزامي.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="input">سبب الإلغاء الإداري.</param>
    Task CancelAdminAsync(Guid id, AdminCancelOrderInput input);

    /// <summary>
    /// يُنشئ ويطبق تعديلاً مالياً على الطلب استناداً إلى نتائج محضر الفحص الفني المكتمل.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="input">معرّف الفحص وقائمة الفروقات الفعلية وختم التزامن.</param>
    /// <returns>تفاصيل التعديل المالي الناتج.</returns>
    Task<OrderAdjustmentDto> CreateAdjustmentAsync(Guid id, CreateOrderAdjustmentInput input);

    /// <summary>
    /// يبدأ مرحلة المعاينة والفحص الفني للطلب فور استلام الملابس في مقر المغسلة (الانتقال إلى Inspection).
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    Task StartInspectionAsync(Guid id);

    /// <summary>
    /// يبدأ مرحلة المعالجة الفعلية للملابس (الانتقال إلى Processing ومرحلة Sorting).
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    Task StartProcessingAsync(Guid id);

    /// <summary>
    /// ينقل معالجة الملابس إلى المرحلة التشغيلية التالية بالتسلسل الصارم (فرز -> غسيل -> كي -> تغليف).
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="notes">ملاحظات تشغيلية اختيارية من فني المرحلة.</param>
    Task MoveToNextProcessingStageAsync(Guid id, string? notes = null);

    /// <summary>
    /// يعتمد اكتمال معالجة وتغليف الملابس وجاهزيتها للتوصيل (الانتقال إلى ReadyForDelivery).
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    Task MarkReadyForDeliveryAsync(Guid id);
}
