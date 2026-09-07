using laundry_SaaS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace laundry_SaaS.Permissions;

/// <summary>
/// مزود تعريف الصلاحيات (Permission Definition Provider) الخاص بمنصة المغاسل.
/// <para>
/// يقوم بتسجيل وإتاحة مجموعات وبنود الصلاحيات لإطار عمل ABP، لتظهر في واجهة إدارة الصلاحيات للمستخدمين والأدوار.
/// </para>
/// </summary>
public class laundry_SaaSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    /// <summary>
    /// يعرف مجموعات وبنود الصلاحيات الخاصة بالمنصة ويسجلها في سياق الصلاحيات.
    /// </summary>
    /// <param name="context">سياق تعريف الصلاحيات في ABP.</param>
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(laundry_SaaSPermissions.GroupName, L("Permission:laundry_SaaS"));
        
        // Base permission group initialized.
        // Each module owner will register module-specific permissions under this group when starting their module.
    }

    /// <summary>
    /// دالة مساعدة لإنشاء نصوص مترجمة لأسماء الصلاحيات باستخدام ملف الموارد المحلي للمنصة.
    /// </summary>
    /// <param name="name">مفتاح الترجمة.</param>
    /// <returns>نص محلي قابل للترجمة.</returns>
    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<laundry_SaaSResource>(name);
    }
}

