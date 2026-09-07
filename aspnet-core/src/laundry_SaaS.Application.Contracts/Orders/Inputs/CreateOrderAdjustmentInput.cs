using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات إنشاء واعتماد تعديل مالي على الطلب ناتج عن محضر الفحص الفني (Create Order Adjustment Input).
/// <para>
/// حماية النطاق والبيانات المالية:
/// محضر الفحص الفني المكتمل (Inspection) هو المصدر الحصري الموثوق (Single Source of Truth) لكافة الفروقات،
/// ولا يقبل الأمر أي تكرار لبنود أو تفاصيل الفروقات من الواجهة؛ بل يقوم التطبيق بتحميل الفحص المعتمد
/// واستدعاء خدمة النطاق OrderAdjustmentDomainService لحساب الفوارق بالاعتماد على أسعار الكتالوج الموثوقة واللقطات السعرية.
/// </para>
/// </summary>
public class CreateOrderAdjustmentInput : IHasConcurrencyStamp
{
    /// <summary>
    /// معرّف محضر الفحص الفني المكتمل المرتبط بهذا التعديل.
    /// </summary>
    [Required]
    public Guid InspectionId { get; set; }

    /// <summary>
    /// سبب التعديل الموثق (مثل: وجود قطع إضافية في الحقيبة، اختلاف نوع القماش، نقصان قطعة).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// ختم التزامن لمنع تعديل الطلب بشكل متزامن متضارب.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
