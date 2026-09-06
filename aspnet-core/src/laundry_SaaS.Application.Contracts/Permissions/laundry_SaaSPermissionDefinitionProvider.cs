using laundry_SaaS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace laundry_SaaS.Permissions;

public class laundry_SaaSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(laundry_SaaSPermissions.GroupName, L("Permission:laundry_SaaS"));
        
        // Base permission group initialized.
        // Each module owner will register module-specific permissions under this group when starting their module.
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<laundry_SaaSResource>(name);
    }
}

