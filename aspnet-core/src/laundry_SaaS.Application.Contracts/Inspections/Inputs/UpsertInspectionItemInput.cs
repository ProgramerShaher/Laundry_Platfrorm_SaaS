using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Inspections;

/// <summary>
/// مدخلات تسجيل أو تعديل نتيجة فحص ومعاينة بند في محضر الفحص الفني (Upsert Inspection Item Input).
/// <para>
/// حماية أمان النطاق والحقيقة التشغيلية:
/// يعبر هذا الكائن عن النتائج الفعلية المرصودة فقط، ولا يسمح للواجهة بتحديد أو تزوير القيم المتوقعة (Expected Snapshot)؛
/// فالخادم هو المصدر الحصري الموثوق لقراءة القيم المتوقعة من بنود الطلب الأصلية (OrderItems).
/// </para>
/// </summary>
public class UpsertInspectionItemInput
{
    /// <summary>
    /// معرّف بند الطلب الأصلي المقابل (OrderItemId).
    /// يكون حاملاً لقيمة للبند الأصلي، ويكون فارغاً (null) حصراً للقطعة الإضافية غير المسجلة في الطلب.
    /// </summary>
    public Guid? OrderItemId { get; set; }

    /// <summary>
    /// معرّف نوع القطعة الفعلي المفحوص (إلزامي للقطعة الإضافية أو عند تغير نوع الصنف، أو يطابق المتوقع).
    /// </summary>
    public Guid? ActualLaundryItemTypeId { get; set; }

    /// <summary>
    /// معرّف نوع الخدمة الفعلية المفحوصة (إلزامي للقطعة الإضافية أو عند تغير نوع الخدمة، أو يطابق المتوقع).
    /// </summary>
    public Guid? ActualLaundryServiceId { get; set; }

    /// <summary>
    /// الكمية الفعلية المستلمة والمفحوصة في المغسلة.
    /// الصفر يعني أن البند مفقود بالكامل (Missing Item)، وللبند الإضافي يجب أن تكون الكمية أكبر من صفر.
    /// </summary>
    [Range(0, 1000)]
    public int ActualQuantity { get; set; }

    /// <summary>
    /// ملاحظات الفني التفصيلية حول أسباب الاختلاف أو حالة القطعة.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
