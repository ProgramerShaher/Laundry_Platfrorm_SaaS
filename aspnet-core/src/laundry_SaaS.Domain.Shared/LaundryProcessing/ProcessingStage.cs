namespace laundry_SaaS.LaundryProcessing;

/// <summary>
/// يمثل المراحل التشغيلية الداخلية لمعالجة وتنظيف الملابس داخل مقر المغسلة.
/// تكون هذه القيمة فعالة فقط عندما تكون حالة الطلب العامة <see cref="Orders.OrderStatus.Processing"/>.
/// يعد كيان الطلب (Order) هو المصدر الموثوق والحصري (Source of Truth) لمرحلة المعالجة الحالية.
/// </summary>
public enum ProcessingStage
{
    /// <summary>
    /// مرحلة الفرز والتصنيف حسب نوع الأقمشة والألوان وإرشادات الغسيل.
    /// </summary>
    Sorting = 0,

    /// <summary>
    /// مرحلة الغسيل الفعلي في الغسالات وفق البرنامج والمواد المحددة.
    /// </summary>
    Washing = 1,

    /// <summary>
    /// مرحلة التجفيف الآلي أو الهوائي للملابس.
    /// </summary>
    Drying = 2,

    /// <summary>
    /// مرحلة الكي بالبخار أو المكاوي المخصصة لكل نوع قماش.
    /// </summary>
    Ironing = 3,

    /// <summary>
    /// مرحلة تطبيق الملابس وترتيبها بعناية.
    /// </summary>
    Folding = 4,

    /// <summary>
    /// مرحلة التغليف النهائي بالنايلون أو العلاقات المخصصة لتصبح جاهزة للتسليم.
    /// </summary>
    Packaging = 5
}
