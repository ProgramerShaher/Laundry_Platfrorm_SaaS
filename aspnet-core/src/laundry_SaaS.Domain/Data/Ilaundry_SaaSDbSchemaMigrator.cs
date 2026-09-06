using System.Threading.Tasks;

namespace laundry_SaaS.Data;

public interface Ilaundry_SaaSDbSchemaMigrator
{
    Task MigrateAsync();
}
