namespace laundry_SaaS.Permissions;

/// <summary>
/// ثوابت أسماء ومجموعات الصلاحيات (Permissions) المعتمدة في النظام.
/// <para>
/// تتبع تسمية الصلاحيات صيغة المعيار: laundry_SaaS.ModuleName.Action
/// مثل: laundry_SaaS.Orders.Create أو laundry_SaaS.Drivers.Assign
/// وتُستخدم للتحقق من صلاحيات المستخدمين والأدوار (Roles) في طبقات التطبيق وواجهات برمجة التطبيقات.
/// </para>
/// </summary>
public static class laundry_SaaSPermissions
{
    /// <summary>
    /// اسم المجموعة الرئيسية لصلاحيات المنصة في شجرة إدارة الصلاحيات.
    /// </summary>
    public const string GroupName = "laundry_SaaS";

    /*
     * Permission Naming Convention:
     * Format: laundry_SaaS.<ModuleName>.<Action>
     * 
     * Examples:
     * - laundry_SaaS.Orders.View
     * - laundry_SaaS.Orders.Create
     * - laundry_SaaS.Drivers.Assign
     * 
     * Each module owner will define their specific permissions when starting their module.
     */
}

