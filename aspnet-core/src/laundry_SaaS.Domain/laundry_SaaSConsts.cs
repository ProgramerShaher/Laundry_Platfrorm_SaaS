namespace laundry_SaaS;

/// <summary>
/// ثوابت النطاق وقواعد البيانات العامة للمنصة.
/// </summary>
public static class laundry_SaaSConsts
{
    /// <summary>
    /// البادئة الافتراضية الموحدة لجميع جداول نطاق العمل في قاعدة البيانات (App) لتمييزها عن جداول ABP الإطارية.
    /// </summary>
    public const string DbTablePrefix = "App";

    /// <summary>
    /// المخطط الافتراضي لقاعدة البيانات (Schema)؛ القيمة null تعني استخدام المخطط الافتراضي للمزود (مثل dbo في SQL Server).
    /// </summary>
    public const string DbSchema = null;
}
