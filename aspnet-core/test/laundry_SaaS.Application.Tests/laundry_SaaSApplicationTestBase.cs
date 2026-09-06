using Volo.Abp.Modularity;

namespace laundry_SaaS;

public abstract class laundry_SaaSApplicationTestBase<TStartupModule> : laundry_SaaSTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
