using laundry_SaaS.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace laundry_SaaS.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(laundry_SaaSEntityFrameworkCoreModule),
    typeof(laundry_SaaSApplicationContractsModule)
    )]
public class laundry_SaaSDbMigratorModule : AbpModule
{
}
