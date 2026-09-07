using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace laundry_SaaS.Laundries;

/// <summary>
/// واجهة خدمة إدارة الفترات الزمنية المجدولة للمغسلة (Laundry Time Slots App Service).
/// تُتيح لإدارة المغسلة إنشاء وتعديل وتعطيل فترات الاستلام والتوصيل للأيام المفتوحة.
/// </summary>
public interface ILaundryTimeSlotAppService : IApplicationService
{
    /// <summary>
    /// يسترجع جميع الفترات الزمنية المجدولة للمغسلة الحالية، مع إمكانية التصفية حسب يوم الأسبوع أو نوع الفترة.
    /// </summary>
    /// <param name="dayOfWeek">يوم الأسبوع الاختياري للتصفية.</param>
    /// <param name="slotType">نوع الفترة الاختياري للتصفية (استلام أو توصيل).</param>
    /// <returns>قائمة الفترات الزمنية المطابقة.</returns>
    Task<List<LaundryTimeSlotDto>> GetListAsync(DayOfWeek? dayOfWeek = null, SlotType? slotType = null);

    /// <summary>
    /// يسترجع تفاصيل فترة زمنية مجدولة محددة بالمعرّف.
    /// </summary>
    /// <param name="id">معرّف الفترة الزمنية.</param>
    /// <returns>تفاصيل الفترة الزمنية.</returns>
    Task<LaundryTimeSlotDto> GetAsync(Guid id);

    /// <summary>
    /// يُنشئ فترة زمنية مجدولة جديدة للاستلام أو التوصيل بعد التحقق من وقوعها داخل ساعات العمل وعدم تداخلها.
    /// </summary>
    /// <param name="input">بيانات الفترة الجديدة.</param>
    /// <returns>تفاصيل الفترة الزمنية المنشأة.</returns>
    Task<LaundryTimeSlotDto> CreateAsync(CreateLaundryTimeSlotInput input);

    /// <summary>
    /// يحدّث مواعيد فترة زمنية موجودة أو يغير حالة تفعيلها.
    /// </summary>
    /// <param name="id">معرّف الفترة الزمنية.</param>
    /// <param name="input">المواعيد الجديدة وحالة التفعيل.</param>
    /// <returns>تفاصيل الفترة الزمنية بعد التعديل.</returns>
    Task<LaundryTimeSlotDto> UpdateAsync(Guid id, UpdateLaundryTimeSlotInput input);

    /// <summary>
    /// يغير حالة تفعيل الفترة الزمنية (تنشيط أو إيقاف مؤقت).
    /// </summary>
    /// <param name="id">معرّف الفترة الزمنية.</param>
    /// <param name="isActive">القيمة الجديدة للتفعيل.</param>
    Task SetActiveAsync(Guid id, bool isActive);
}
