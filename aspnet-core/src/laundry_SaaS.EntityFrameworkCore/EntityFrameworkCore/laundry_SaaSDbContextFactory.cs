using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace laundry_SaaS.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class laundry_SaaSDbContextFactory : IDesignTimeDbContextFactory<laundry_SaaSDbContext>
{
    public laundry_SaaSDbContext CreateDbContext(string[] args)
    {
        laundry_SaaSEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<laundry_SaaSDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new laundry_SaaSDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../laundry_SaaS.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
