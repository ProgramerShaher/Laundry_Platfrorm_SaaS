using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace laundry_SaaS.Data;

/* This is used if database provider does't define
 * Ilaundry_SaaSDbSchemaMigrator implementation.
 */
public class Nulllaundry_SaaSDbSchemaMigrator : Ilaundry_SaaSDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
