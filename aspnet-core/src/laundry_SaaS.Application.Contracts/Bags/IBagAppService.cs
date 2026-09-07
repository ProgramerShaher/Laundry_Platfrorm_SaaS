using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Bags;

/// <summary>
/// واجهة خدمة إدارة حقائب الغسيل وسلسلة الحيازة المادية (Physical Chain of Custody).
/// تدير عمليات مسح الرمز السريع (QR) وتتبع حركات الحقائب دون السماح بتخطي الحالات.
/// </summary>
public interface IBagAppService : IApplicationService
{
    /// <summary>
    /// استرجاع قائمة الحقائب المرتبطة بطلب محدد وسجل حركتها.
    /// </summary>
    Task<List<BagListDto>> GetOrderBagsAsync(Guid orderId);

    /// <summary>
    /// استرجاع بيانات الحقيبة وسجل حركتها بواسطة رمز الاستجابة السريعة (QR Code).
    /// </summary>
    Task<BagListDto> GetByQrCodeAsync(string qrCode);

    /// <summary>
    /// مسح رمز الحقيبة ضوئياً لتسجيل حركة في سلسلة الحيازة (استلام، دخول المغسلة، تغليف، تسليم).
    /// الخادم هو من يحدد الحدث تلقائياً دون إمكانية للقفز اليدوي العشوائي.
    /// </summary>
    Task<BagEventDto> ScanQrCodeAsync(ScanBagQrInput input);

    /// <summary>
    /// ربط وإنشاء حقيبة غسيل للطلب؛ يقوم الخادم (Backend) بتوليد رقم الحقيبة الفيزيائي ورمز الاستجابة السريعة (QR) المشفر تلقائياً لضمان الأمان والتفرد.
    /// </summary>
    /// <param name="orderId">معرّف الطلب المرتبط.</param>
    /// <returns>بيانات الحقيبة المنشأة ورمز الـ QR المولد.</returns>
    Task<BagListDto> AssignBagToOrderAsync(Guid orderId);
}
