using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Inspections;

/// <summary>
/// واجهة خدمة المعاينة والفحص الفني لملابس الطلب عند وصولها للمغسلة.
/// تدير فتح المحاضر، تسجيل الأضرار والعيوب المسبقة، ومطابقة الكميات والأنواع، واعتماد المحاضر رسمياً.
/// </summary>
public interface IInspectionAppService : IApplicationService
{
    /// <summary>
    /// استرجاع محضر الفحص الفني بواسطة المعرف الفريد للطلب.
    /// </summary>
    Task<InspectionDetailDto> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// استرجاع محضر الفحص الفني بواسطة المعرف الفريد للمحضر.
    /// </summary>
    Task<InspectionDetailDto> GetAsync(Guid id);

    /// <summary>
    /// بدء عملية الفحص الفني للطلب رسمياً وفتح محضر فحص تشغيلي جديد.
    /// </summary>
    Task<InspectionDetailDto> StartInspectionAsync(Guid orderId, Guid? bagId);

    /// <summary>
    /// تسجيل وتوثيق ضرر أو عيب مسبق على قطعة ملابس أثناء الفحص مع حفظ مرجع الصورة الفوتوغرافية.
    /// </summary>
    Task<DamageDto> RecordDamageAsync(Guid inspectionId, RecordInspectionDamageInput input);

    /// <summary>
    /// تسجيل أو تحديث نتيجة مطابقة وفحص بند من بنود الطلب (أصلي مطابق، مختلف، مفقود، أو قطعة إضافية).
    /// </summary>
    /// <param name="inspectionId">معرّف محضر الفحص الفني.</param>
    /// <param name="input">النتائج الفعلية للقطعة المفحوصة دون التلاعب بالبيانات المتوقعة.</param>
    /// <returns>تفاصيل سطر الفحص الموثق.</returns>
    Task<InspectionItemDto> UpsertItemAsync(Guid inspectionId, UpsertInspectionItemInput input);

    /// <summary>
    /// إكمال واعتماد محضر الفحص الفني بعد مطابقة كافة القطع وتوثيق الملاحظات.
    /// </summary>
    Task<InspectionDetailDto> CompleteInspectionAsync(Guid id, string? notes);

    /// <summary>
    /// إعادة فتح محضر فحص معتمد مسبقاً بقرار إداري لتعديل الفروقات مع توثيق السبب الإلزامي.
    /// </summary>
    Task<InspectionDetailDto> ReopenInspectionAsync(Guid id, string reason);
}
