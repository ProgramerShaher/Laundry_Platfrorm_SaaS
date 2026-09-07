using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// بيانات إدخال تسجيل تعذر أو فشل تنفيذ مهمة لوجستية (استلام أو توصيل) من قبل السائق.
/// </summary>
public class FailTaskInput
{
    /// <summary>
    /// سبب الفشل أو التعذر (مثل: العميل لا يجيب، العنوان غير صحيح، إلخ).
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = null!;
}
