using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات إلغاء الطلب من قبل العميل (وفق سياسة الإلغاء الصارمة).
/// مسموح فقط في الحالات: Draft, PendingPickup, PickupAssigned.
/// </summary>
public class CustomerCancelOrderInput
{
    /// <summary>
    /// سبب الإلغاء المدون من قبل العميل.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}
