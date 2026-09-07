using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات رفض العميل للتعديل المالي الناتج عن محضر الفحص الفني.
/// يؤدي الرفض إلى إلغاء التعديل أو إرجاع الملابس وفق سياسة المغسلة.
/// </summary>
public class RejectOrderAdjustmentInput
{
    /// <summary>
    /// سبب رفض التعديل المالي من قبل العميل.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string RejectionReason { get; set; } = string.Empty;
}
