using System;
using System.Collections.Generic;
using System.Text;
using laundry_SaaS.Localization;
using Volo.Abp.Application.Services;

namespace laundry_SaaS;

/* Inherit your application services from this class.
 */
public abstract class laundry_SaaSAppService : ApplicationService
{
    protected laundry_SaaSAppService()
    {
        LocalizationResource = typeof(laundry_SaaSResource);
    }
}
