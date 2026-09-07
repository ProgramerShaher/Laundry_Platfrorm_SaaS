using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Orders;

/// <summary>
/// واجهة خدمة طلبات العميل (Customer Orders App Service).
/// تُستخدم حصراً من قبل تطبيق العميل لطلب عروض الأسعار، وإنشاء الطلبات، ومتابعة حالتها، وإلغائها، والرد على التعديلات المالية.
/// </summary>
public interface ICustomerOrderAppService : IApplicationService
{
    /// <summary>
    /// يحسب عرض سعر تقديري مسبق للطلب بناءً على أسعار كتالوج المغسلة ورسوم التوصيل والحد الأدنى.
    /// </summary>
    /// <param name="input">معرّف المغسلة وقائمة البنود والكميات.</param>
    /// <returns>عرض السعر المحسوب على الخادم.</returns>
    Task<OrderQuoteDto> CalculateQuoteAsync(CalculateOrderQuoteInput input);

    /// <summary>
    /// يُنشئ ويؤكد طلباً جديداً للعميل بعد التحقق الصارم من فترة الاستلام وعناوين التوصيل واحتساب الأسعار من الكتالوج.
    /// </summary>
    /// <param name="input">بيانات الطلب، العناوين، وموعد الاستلام المجدول، والبنود.</param>
    /// <returns>تفاصيل الطلب المنشأ المخصصة للعميل.</returns>
    Task<CustomerOrderDetailDto> CreateAsync(CreateOrderInput input);

    /// <summary>
    /// يسترجع تفاصيل طلب محدد يخص العميل الحالي مع التحقق من ملكيته للطلب.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <returns>تفاصيل الطلب الكاملة المخصصة للعميل.</returns>
    Task<CustomerOrderDetailDto> GetAsync(Guid id);

    /// <summary>
    /// يسترجع قائمة طلبات العميل الحالي في شكل صفحات قابلة للتصفية حسب الحالة.
    /// </summary>
    /// <param name="input">معايير التصفية والتقسيم لصفحات.</param>
    /// <returns>صفحة من عناصر قائمة طلبات العميل.</returns>
    Task<PagedResultDto<CustomerOrderListDto>> GetMyOrdersAsync(GetCustomerOrdersInput input);

    /// <summary>
    /// يلغي طلباً من قبل العميل وفق سياسة الإلغاء الصارمة (متاح فقط في الحالات: Draft, PendingPickup, PickupAssigned).
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="input">سبب الإلغاء المدون من قبل العميل.</param>
    Task CancelAsync(Guid id, CustomerCancelOrderInput input);

    /// <summary>
    /// يوافق العميل على التعديل المالي الناتج عن محضر الفحص الفني ويحدث إجمالي الطلب.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="adjustmentId">معرّف التعديل المالي.</param>
    Task ApproveAdjustmentAsync(Guid id, Guid adjustmentId);

    /// <summary>
    /// يرفض العميل التعديل المالي الناتج عن محضر الفحص الفني مع تدوين سبب الرفض.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="adjustmentId">معرّف التعديل المالي.</param>
    /// <param name="input">سبب الرفض.</param>
    Task RejectAdjustmentAsync(Guid id, Guid adjustmentId, RejectOrderAdjustmentInput input);
}
