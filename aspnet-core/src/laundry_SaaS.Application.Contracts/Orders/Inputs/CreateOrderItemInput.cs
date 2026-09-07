using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات بند من بنود الطلب الجديد المراد إنشاؤه (Create Order Item Input).
/// لا يحتوي على أسعار؛ حيث تُستخرج الأسعار وتُحسب حصراً في الخادم من الكتالوج الموثوق.
/// </summary>
public class CreateOrderItemInput
{
    /// <summary>
    /// معرّف نوع قطعة الملابس (مثل ثوب، غترة، عباءة).
    /// </summary>
    [Required]
    public Guid LaundryItemTypeId { get; set; }

    /// <summary>
    /// معرّف الخدمة المطلوبة (مثل غسيل وكي، تنظيف جاف).
    /// </summary>
    [Required]
    public Guid LaundryServiceId { get; set; }

    /// <summary>
    /// الكمية المطلوبة من هذا الصنف بهذه الخدمة.
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }

    /// <summary>
    /// ملاحظات العميل الخاصة بهذه القطعة (مثل وجود بقعة حبر، رغبة بكي نشاء خفيف).
    /// </summary>
    [StringLength(500)]
    public string? CustomerNotes { get; set; }
}
