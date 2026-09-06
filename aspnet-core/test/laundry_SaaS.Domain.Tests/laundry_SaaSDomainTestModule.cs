using Volo.Abp.Modularity;

namespace laundry_SaaS;

[DependsOn(
    typeof(laundry_SaaSDomainModule),
    typeof(laundry_SaaSTestBaseModule)
)]
public class laundry_SaaSDomainTestModule : AbpModule
{

}
