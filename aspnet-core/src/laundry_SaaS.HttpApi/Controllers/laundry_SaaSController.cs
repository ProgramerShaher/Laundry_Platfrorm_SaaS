using laundry_SaaS.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace laundry_SaaS.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class laundry_SaaSController : AbpControllerBase
{
    protected laundry_SaaSController()
    {
        LocalizationResource = typeof(laundry_SaaSResource);
    }
}
