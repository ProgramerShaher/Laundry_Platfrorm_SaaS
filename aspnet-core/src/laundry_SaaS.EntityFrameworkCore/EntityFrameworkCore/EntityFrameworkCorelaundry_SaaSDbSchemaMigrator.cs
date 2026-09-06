using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using laundry_SaaS.Data;
using Volo.Abp.DependencyInjection;

namespace laundry_SaaS.EntityFrameworkCore;

public class EntityFrameworkCorelaundry_SaaSDbSchemaMigrator
    : Ilaundry_SaaSDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCorelaundry_SaaSDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the laundry_SaaSDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<laundry_SaaSDbContext>()
            .Database
            .MigrateAsync();
    }
}
