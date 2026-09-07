using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Orders;

/// <summary>
/// مدخلات إنشاء وتأكيد طلب جديد من قبل العميل (Create Order Input).
/// <para>
/// أمان التسعير والبيانات:
/// لا يحتوي هذا الكائن نهائياً على أسعار، أو مجاميع، أو رسوم توصيل، أو خصومات، أو معرّفات مستأجر،
/// حيث يقوم الخادم بحساب الأسعار من كتالوج المغسلة وإنشاء لقطة لجدول الاستلام وعناوين التوصيل.
/// </para>
/// </summary>
public class CreateOrderInput
{
    /// <summary>
    /// معرّف المغسلة المختارة للطلب.
    /// </summary>
    [Required]
    public Guid LaundryId { get; set; }

    /// <summary>
    /// معرّف عنوان استلام الملابس المسجل في حساب العميل.
    /// </summary>
    [Required]
    public Guid PickupAddressId { get; set; }

    /// <summary>
    /// معرّف عنوان توصيل الملابس بعد الانتهاء (اختياري، في حال تركه فارغاً يُعتمد عنوان الاستلام نفسه).
    /// </summary>
    public Guid? DeliveryAddressId { get; set; }

    /// <summary>
    /// تاريخ الاستلام التقويمي المطلوب (Pickup Date).
    /// </summary>
    [Required]
    public DateOnly PickupDate { get; set; }

    /// <summary>
    /// معرّف الفترة الزمنية المجدولة للاستلام (Pickup Slot ID) التابعة للمغسلة.
    /// </summary>
    [Required]
    public Guid PickupSlotId { get; set; }

    /// <summary>
    /// ملاحظات أو تعليمات عامة من العميل لإدارة المغسلة أو المندوب.
    /// </summary>
    [StringLength(1000)]
    public string? CustomerNotes { get; set; }

    /// <summary>
    /// قائمة بنود الملابس والخدمات والكميات المطلوبة في الطلب.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<CreateOrderItemInput> Items { get; set; } = new();
}
