using laundry_SaaS.Samples;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Domains;

[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<laundry_SaaSEntityFrameworkCoreTestModule>
{

}
