using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using laundry_SaaS.EntityFrameworkCore;
using laundry_SaaS.MultiTenancy;

using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;

namespace laundry_SaaS;

[DependsOn(
    typeof(laundry_SaaSHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(laundry_SaaSApplicationModule),
    typeof(laundry_SaaSEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class laundry_SaaSHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("laundry_SaaS");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        // Disable the ABP Libs folder check.
        // This HttpApiHost uses Angular as the frontend,
        // so wwwroot/libs is not required by the API host.
        Configure<AbpMvcLibsOptions>(options =>
        {
            options.CheckLibs = false;
        });

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
        );

        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });

        Configure<Volo.Abp.AspNetCore.Mvc.Libs.AbpMvcLibsOptions>(options =>
        {
            options.CheckLibs = false;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl =
                configuration["App:SelfUrl"];

            options.RedirectAllowedUrls.AddRange(
                configuration["App:RedirectAllowedUrls"]?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>()
            );

            options.Applications["Angular"].RootUrl =
                configuration["App:ClientUrl"];

            options.Applications["Angular"].Urls[
                AccountUrlNames.PasswordReset
            ] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(
        ServiceConfigurationContext context)
    {
        var hostingEnvironment =
            context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<
                    laundry_SaaSDomainSharedModule>(
                    Path.Combine(
                        hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}laundry_SaaS.Domain.Shared"
                    )
                );

                options.FileSets.ReplaceEmbeddedByPhysical<
                    laundry_SaaSDomainModule>(
                    Path.Combine(
                        hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}laundry_SaaS.Domain"
                    )
                );

                options.FileSets.ReplaceEmbeddedByPhysical<
                    laundry_SaaSApplicationContractsModule>(
                    Path.Combine(
                        hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}laundry_SaaS.Application.Contracts"
                    )
                );

                options.FileSets.ReplaceEmbeddedByPhysical<
                    laundry_SaaSApplicationModule>(
                    Path.Combine(
                        hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}laundry_SaaS.Application"
                    )
                );
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(
                typeof(laundry_SaaSApplicationModule).Assembly
            );
        });
    }

    private static void ConfigureSwaggerServices(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                {
                    "laundry_SaaS",
                    "laundry_SaaS API"
                }
            },
            options =>
            {
                options.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "laundry_SaaS API",
                        Version = "v1"
                    }
                );

                options.DocInclusionPredicate(
                    (docName, description) => true
                );

                options.CustomSchemaIds(
                    type => type.FullName
                );
            }
        );
    }

    private void ConfigureCors(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(
                                ",",
                                StringSplitOptions.RemoveEmptyEntries
                            )
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                        ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(
        ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();

        app.MapAbpStaticAssets();

        app.UseRouting();

        app.UseCors();

        app.UseAuthentication();

        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();

        app.UseDynamicClaims();

        app.UseAuthorization();

        app.UseSwagger();

        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "laundry_SaaS API"
            );

            var configuration =
                context.ServiceProvider
                    .GetRequiredService<IConfiguration>();

            c.OAuthClientId(
                configuration["AuthServer:SwaggerClientId"]
            );

            c.OAuthScopes("laundry_SaaS");
        });

        app.UseAuditing();

        app.UseAbpSerilogEnrichers();

        app.UseConfiguredEndpoints();
    }
}