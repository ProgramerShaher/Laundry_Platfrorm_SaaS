namespace laundry_SaaS;

/// <summary>
/// ثوابت أكواد أخطاء الأعمال (Domain Error Codes) المعتمدة في النظام.
/// تتبع صيغة المعيار: laundry_SaaS:ModuleName:CodeNumber
/// وتُستخدم عند رمي استثناءات الأعمال من نوع BusinessException لتقديم رسائل خطأ مترجمة وموحدة للواجهات الأمامية.
/// </summary>
public static class laundry_SaaSDomainErrorCodes
{
    /*
     * Base Pattern Convention for Business Exceptions:
     * Format: laundry_SaaS:<ModuleName>:<CodeNumber>
     * 
     * Examples:
     * - laundry_SaaS:Orders:001
     * - laundry_SaaS:Pickup:001
     * - laundry_SaaS:Processing:001
     * - laundry_SaaS:Customer:001
     * - laundry_SaaS:Laundry:001
     * 
     * Each module owner will declare their specific error codes within their module scope.
     */

    /// <summary>
    /// البادئة الموحدة لجميع أكواد أخطاء النظام، تفيد في تمييز أخطاء نطاق العمل عن أخطاء البنية التحتية.
    /// </summary>
    public const string Prefix = "laundry_SaaS";
}

