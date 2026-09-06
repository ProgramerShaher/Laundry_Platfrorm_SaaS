using laundry_SaaS.Samples;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Applications;

[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<laundry_SaaSEntityFrameworkCoreTestModule>
{

}
