using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// بيانات إدخال تأكيد تحصيل المبلغ النقدي من العميل عند التوصيل (COD).
/// يقوم السائق بإدخال المبلغ المستلم ليطابقه الخادم مع إجمالي الطلب المطلوب تحصيله.
/// </summary>
public class ConfirmCashCollectionInput
{
    /// <summary>
    /// المبلغ النقدي المحصل فعلياً بواسطة السائق.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "المبلغ المحصل يجب أن يكون قيمة موجبة أو صفر.")]
    public decimal CollectedAmount { get; set; }
}
