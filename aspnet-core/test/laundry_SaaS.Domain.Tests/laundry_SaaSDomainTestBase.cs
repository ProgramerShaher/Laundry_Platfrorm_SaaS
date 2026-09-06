using Volo.Abp.Modularity;

namespace laundry_SaaS;

/* Inherit from this class for your domain layer tests. */
public abstract class laundry_SaaSDomainTestBase<TStartupModule> : laundry_SaaSTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
