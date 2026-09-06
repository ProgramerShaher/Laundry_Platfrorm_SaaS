using Microsoft.Extensions.Localization;
using laundry_SaaS.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace laundry_SaaS;

[Dependency(ReplaceServices = true)]
public class laundry_SaaSBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<laundry_SaaSResource> _localizer;

    public laundry_SaaSBrandingProvider(IStringLocalizer<laundry_SaaSResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
