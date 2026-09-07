using laundry_SaaS.Catalog;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Applications;

[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class EfCoreLaundryCatalogAppServiceTests : LaundryCatalogAppServiceTests<laundry_SaaSEntityFrameworkCoreTestModule>
{

}
