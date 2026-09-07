namespace laundry_SaaS.Drivers;

/// <summary>
/// مدخلات تحديث حالة توفر السائق لاستقبال وإسناد المهام اللوجستية (Driver Availability Input).
/// </summary>
public class UpdateDriverAvailabilityInput
{
    /// <summary>
    /// ما إذا كان السائق متاحاً حالياً لاستقبال وإسناد مهام جديدة (Available).
    /// </summary>
    public bool IsAvailable { get; set; }
}
