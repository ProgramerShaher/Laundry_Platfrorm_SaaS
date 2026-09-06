using Volo.Abp.Modularity;

namespace laundry_SaaS;

[DependsOn(
    typeof(laundry_SaaSApplicationModule),
    typeof(laundry_SaaSDomainTestModule)
)]
public class laundry_SaaSApplicationTestModule : AbpModule
{

}
