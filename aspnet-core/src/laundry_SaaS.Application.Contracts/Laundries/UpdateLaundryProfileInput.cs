using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace laundry_SaaS.Laundries;

/// <summary>
/// مدخلات تحديث الملف التعريفي للمغسلة من قبل إدارة المغسلة (Laundry Profile Update).
/// </summary>
public class UpdateLaundryProfileInput : IHasConcurrencyStamp
{
    /// <summary>
    /// الاسم التجاري للمغسلة.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// وصف تفصيلي للمغسلة والخدمات المقدمة.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// رقم الهاتف المعتمد للتواصل.
    /// </summary>
    [Required]
    [Phone]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للمغسلة.
    /// </summary>
    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// اسم مرجع الشعار في التخزين السحابي (Blob Name).
    /// </summary>
    [StringLength(500)]
    public string? LogoBlobName { get; set; }

    /// <summary>
    /// رسوم التوصيل المحددة للطلبات.
    /// </summary>
    [Range(0.0, 10000.0)]
    public decimal DeliveryFee { get; set; }

    /// <summary>
    /// الحد الأدنى لقيمة الطلب.
    /// </summary>
    [Range(0.0, 10000.0)]
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>
    /// الساعات المقدرة لمعالجة الطلب قياسياً.
    /// </summary>
    [Range(1, 720)]
    public int EstimatedProcessingHours { get; set; }

    /// <summary>
    /// ما إذا كانت المغسلة تستقبل طلبات جديدة حالياً.
    /// </summary>
    public bool AcceptingOrders { get; set; }

    /// <summary>
    /// ختم التزامن للتحكم بالتحديث المتزامن المتفائل ومنع تضارب التعديلات.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
