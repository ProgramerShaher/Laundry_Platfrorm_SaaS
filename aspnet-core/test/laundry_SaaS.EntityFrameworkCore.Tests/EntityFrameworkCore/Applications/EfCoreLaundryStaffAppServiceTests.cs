using laundry_SaaS.Laundries;
using Xunit;

namespace laundry_SaaS.EntityFrameworkCore.Applications;

[Collection(laundry_SaaSTestConsts.CollectionDefinitionName)]
public class EfCoreLaundryStaffAppServiceTests : LaundryStaffAppServiceTests<laundry_SaaSEntityFrameworkCoreTestModule>
{

}
