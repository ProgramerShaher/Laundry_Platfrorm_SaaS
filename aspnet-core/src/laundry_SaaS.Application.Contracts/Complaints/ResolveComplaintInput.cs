using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Complaints;

/// <summary>
/// بيانات إدخال تسوية وحل الشكوى من قبل إدارة المغسلة.
/// </summary>
public class ResolveComplaintInput
{
    /// <summary>
    /// الملاحظات والقرارات التفصيلية المعتمدة لحل وتسوية الشكوى.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string ResolutionNotes { get; set; } = null!;

    /// <summary>
    /// مبلغ التعويض المالي المعتمد للعميل إن وجد (افتراضياً 0).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "مبلغ التعويض يجب أن يكون قيمة موجبة أو صفر.")]
    public decimal CompensationAmount { get; set; }
}
