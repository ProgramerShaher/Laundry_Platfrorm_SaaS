using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// بيانات إدخال إسناد مهمة (استلام أو توصيل) لسائق محدد من قبل إدارة المغسلة.
/// </summary>
public class AssignDriverTaskInput
{
    /// <summary>
    /// المعرف الفريد للسائق المراد إسناد المهمة إليه.
    /// </summary>
    [Required]
    public Guid DriverId { get; set; }
}
