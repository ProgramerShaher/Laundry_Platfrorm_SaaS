using Volo.Abp.Settings;

namespace laundry_SaaS.Settings;

public class laundry_SaaSSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(laundry_SaaSSettings.MySetting1));
    }
}
