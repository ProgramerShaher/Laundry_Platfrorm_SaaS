using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// واجهة خدمة المهام اللوجستية للسائقين (مهام الاستلام ومهام التوصيل).
/// تتبع مسار تنفيذ المهام مرحلياً عبر دوال صريحة تمنع القفز العشوائي للحالات وتتحقق من الرمز السري والتحصيل النقدي.
/// ملاحظة معمارية: تعيين وإسناد المهام للسائقين يتبع إدارة المغسلة (IDriverManagementAppService) وليس عقد تطبيق السائق.
/// </summary>
public interface IDriverTaskAppService : IApplicationService
{
    /// <summary>
    /// استرجاع قائمة المهام المسندة للسائق الحالي (استلام وتوصيل) مع دعم الفلترة والترقيم.
    /// </summary>
    Task<PagedResultDto<DriverTaskListDto>> GetMyTasksAsync(PagedAndSortedResultRequestDto input);

    // --- Pickup Task Workflow (Assigned -> Accepted -> OutForPickup -> Arrived -> PickedUp -> Completed) ---

    /// <summary>
    /// استرجاع التفاصيل الكاملة لمهمة استلام محددة.
    /// </summary>
    Task<PickupTaskDetailDto> GetPickupTaskAsync(Guid id);

    /// <summary>
    /// قبول السائق لمهمة الاستلام المسندة إليه.
    /// </summary>
    Task AcceptPickupTaskAsync(Guid id);

    /// <summary>
    /// انطلاق السائق في رحلة التوجه إلى موقع العميل لاستلام الملابس (OutForPickup).
    /// </summary>
    Task StartPickupTripAsync(Guid id);

    /// <summary>
    /// توثيق وصول السائق الفعلي إلى موقع العميل لبدء استلام الملابس.
    /// </summary>
    Task ArriveAtPickupAsync(Guid id);

    /// <summary>
    /// تأكيد استلام الملابس ووضعها في الحقيبة بنجاح من العميل (PickedUp).
    /// </summary>
    Task ConfirmPickupAsync(Guid id);

    /// <summary>
    /// إكمال مهمة الاستلام وتوثيق تسليم الحقيبة إلى مقر المغسلة بنجاح (Completed).
    /// </summary>
    Task CompletePickupAsync(Guid id);

    /// <summary>
    /// تسجيل تعثر أو فشل مهمة الاستلام مع توثيق السبب المانع.
    /// </summary>
    Task FailPickupTaskAsync(Guid id, FailTaskInput input);

    // --- Delivery Task Workflow (Assigned -> Accepted -> PickedUpFromLaundry -> OutForDelivery -> Arrived -> OTP/COD -> Delivered) ---

    /// <summary>
    /// استرجاع التفاصيل الكاملة لمهمة توصيل محددة مع حالة رمز التحقق.
    /// </summary>
    Task<DeliveryTaskDetailDto> GetDeliveryTaskAsync(Guid id);

    /// <summary>
    /// قبول السائق لمهمة التوصيل المسندة إليه.
    /// </summary>
    Task AcceptDeliveryTaskAsync(Guid id);

    /// <summary>
    /// تأكيد استلام السائق للملابس النظيفة المغلفة من المغسلة للانطلاق بها (PickedUpFromLaundry).
    /// </summary>
    Task PickUpFromLaundryAsync(Guid id);

    /// <summary>
    /// انطلاق السائق في رحلة التوصيل نحو موقع العميل لتسليم الطلب (OutForDelivery).
    /// </summary>
    Task StartDeliveryTripAsync(Guid id);

    /// <summary>
    /// توثيق وصول السائق إلى موقع تسليم العميل.
    /// </summary>
    Task ArriveAtDeliveryAsync(Guid id);

    /// <summary>
    /// طلب توليد وإرسال رمز التحقق السري (OTP) إلى هاتف العميل للتسليم بعد وصول السائق.
    /// </summary>
    Task<DeliveryOtpStatusDto> RequestDeliveryOtpAsync(Guid id);

    /// <summary>
    /// التحقق من رمز التسليم السري (OTP) المكون من 6 أرقام المدخل من قبل السائق والمسلم من العميل.
    /// </summary>
    Task<DeliveryOtpStatusDto> VerifyDeliveryOtpAsync(Guid id, VerifyDeliveryOtpInput input);

    /// <summary>
    /// تأكيد تحصيل المبلغ النقدي من العميل ومطابقته على الخادم قبل إتمام التسليم النهائي (COD).
    /// </summary>
    Task ConfirmCashCollectionAsync(Guid id, ConfirmCashCollectionInput input);

    /// <summary>
    /// تأكيد إتمام تسليم الطلب للعميل نهائياً بعد التحقق من الرمز والتحصيل النقدي كاملاً (Delivered).
    /// </summary>
    Task ConfirmDeliveryAsync(Guid id);

    /// <summary>
    /// تسجيل تعثر أو فشل مهمة التوصيل مع توثيق السبب المانع.
    /// </summary>
    Task FailDeliveryTaskAsync(Guid id, FailTaskInput input);
}
