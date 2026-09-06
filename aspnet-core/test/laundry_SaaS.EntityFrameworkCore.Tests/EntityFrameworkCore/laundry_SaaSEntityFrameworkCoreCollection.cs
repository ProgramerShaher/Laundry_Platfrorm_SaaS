using Xunit;

namespace laundry_SaaS.EntityFrameworkCore;

[CollectionDefinition(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class laundry_SaaSEntityFrameworkCoreCollection : ICollectionFixture<laundry_SaaSEntityFrameworkCoreFixture>
{

}
