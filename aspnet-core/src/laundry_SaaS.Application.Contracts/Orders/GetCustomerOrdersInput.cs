using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات تصفح واسترجاع قائمة طلبات العميل الحالي مع دعم التصفية والتقسيم لصفحات.
/// </summary>
public class GetCustomerOrdersInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// تصفية حسب حالة الطلب الحالية (اختياري).
    /// </summary>
    public OrderStatus? Status { get; set; }
}
