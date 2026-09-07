namespace laundry_SaaS.Complaints;

/// <summary>
/// يحدد تصنيف وطبيعة المشكلة محل الشكوى لتسهيل التوجيه والتحليل الإحصائي لجودة الخدمة.
/// </summary>
public enum ComplaintType
{
    /// <summary>
    /// وجود تلف أو تمزق أو ضرر في قطعة ملابس نتيجة عمليات الغسيل أو النقل.
    /// </summary>
    DamagedItem = 1,

    /// <summary>
    /// فقدان أو نقص في عدد القطع المسلمة للعميل مقارنة بما تم استلامه.
    /// </summary>
    MissingItem = 2,

    /// <summary>
    /// تأخر السائق أو المغسلة في تسليم الطلب عن الموعد المحدد.
    /// </summary>
    LateDelivery = 3,

    /// <summary>
    /// تدني جودة التنظيف أو الكي (مثل عدم إزالة البقع العادية أو تجاعيد الملابس).
    /// </summary>
    BadQuality = 4,

    /// <summary>
    /// استلام قطعة ملابس خاطئة لا تعود للعميل.
    /// </summary>
    WrongItem = 5,

    /// <summary>
    /// نوع شكوى آخر غير مصنف يتم توضيحه في تفاصيل الشكوى.
    /// </summary>
    Other = 99
}
