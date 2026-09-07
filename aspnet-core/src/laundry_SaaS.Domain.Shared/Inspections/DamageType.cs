namespace laundry_SaaS.Inspections;

/// <summary>
/// يحدد نوع الضرر المسبق (Pre-existing Damage) المكتشف على قطعة الملابس أثناء الفحص وقبل بدء عمليات المعالجة والغسيل.
/// يفيد في حماية المغسلة والعميل من النزاعات وتوثيق حالة القطعة عند الاستلام.
/// </summary>
public enum DamageType
{
    /// <summary>
    /// تمزق في نسيج القماش.
    /// </summary>
    Tear = 1,

    /// <summary>
    /// بقعة مستعصية أو قديمة غير قابلة للإزالة بالغسيل العادي.
    /// </summary>
    Stain = 2,

    /// <summary>
    /// زر مفقود أو تالف في القطعة.
    /// </summary>
    MissingButton = 3,

    /// <summary>
    /// بهتان أو تغير في درجات اللون الأصلي للملابس.
    /// </summary>
    Discoloration = 4,

    /// <summary>
    /// حرق سابق في القماش نتيجة كي خاطئ أو مصدر حراري خارجي.
    /// </summary>
    Burn = 5,

    /// <summary>
    /// سحاب مكسور أو معطل.
    /// </summary>
    ZipperBroken = 6,

    /// <summary>
    /// ضرر آخر غير مصنف يتم توضيحه في حقل الوصف.
    /// </summary>
    Other = 99
}
