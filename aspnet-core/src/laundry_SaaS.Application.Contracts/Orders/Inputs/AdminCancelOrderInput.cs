using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات الإلغاء الإداري للطلب من قبل طاقم أو إدارة المغسلة.
/// مسموح في جميع الحالات التشغيلية قبل التسليم النهائي (Delivered / Completed).
/// </summary>
public class AdminCancelOrderInput
{
    /// <summary>
    /// السبب الإداري أو الفني الموثق لإلغاء الطلب (مثل تعذر الوصول للعميل، انقطاع كهرباء، تلف جسيم).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string CancellationReason { get; set; } = string.Empty;
}
