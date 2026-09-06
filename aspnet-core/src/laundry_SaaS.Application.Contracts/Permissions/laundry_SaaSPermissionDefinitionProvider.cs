using laundry_SaaS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace laundry_SaaS.Permissions;

public class laundry_SaaSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(laundry_SaaSPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(laundry_SaaSPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<laundry_SaaSResource>(name);
    }
}
