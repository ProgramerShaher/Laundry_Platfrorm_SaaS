using System;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// يمثل الحالة التشغيلية لرمز التحقق المؤقت (OTP) الخاص بتسليم الطلب للعميل.
/// لا يعرض أي معلومات تشفير أو هاش أو رمز صريح (Security Boundary).
/// </summary>
public class DeliveryOtpStatusDto
{
    /// <summary>
    /// يشير إلى ما إذا كان قد تم التحقق من الرمز بنجاح.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// تاريخ ووقت انتهاء صلاحية الرمز الحالي إن تم توليده.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// تاريخ ووقت إمكانية طلب إعادة إرسال الرمز للعميل بعد انتهاء فترة الانتظار الإلزامية.
    /// </summary>
    public DateTime? CanResendAt { get; set; }

    /// <summary>
    /// عدد المحاولات المتبقية لإدخال الرمز قبل الحظر.
    /// </summary>
    public int RemainingAttempts { get; set; }

    /// <summary>
    /// رقم هاتف العميل مقنعاً (Masked) لحماية الخصوصية وتأكيد وجهة إرسال الرمز (مثل: +966****1234).
    /// </summary>
    public string? MaskedCustomerPhone { get; set; }
}
