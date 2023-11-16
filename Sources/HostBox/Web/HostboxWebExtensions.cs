using System;
using System.Linq;
using HostBox.Loading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostBox.Web
{
    internal static class HostboxWebExtensions
    {
        public static void ConfigureWebHost(IHostBuilder builder, CommandLineArgs args)
        {
            if (args.Web)
            {
                builder
                    .ConfigureWebHostDefaults(b =>
                    {
                        b.UseStartup<Startup>();

                        if (HealthCheckHelper.HealthCheckEnabled)
                        {
                            var urlFromEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://+:5000";
                            b.UseUrls(urlFromEnv, HealthCheckHelper.HealthCheckUrl);
                        }
                    });
            }
            else
            {
                if (HealthCheckHelper.HealthCheckEnabled)
                {
                    builder.ConfigureWebHost(b =>
                    {
                        b.UseKestrel().UseUrls(HealthCheckHelper.HealthCheckUrl);
                        b.UseStartup<StartupHealthCheckOnly>();
                    });
                }
            }
        }

        internal static void ConfigureWebServices(
            ComponentsLoader.LoadComponentsResult loadComponentsResult,
            IServiceCollection services,
            CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.Web)
            {
                var startup = loadComponentsResult?.EntryAssembly?.GetExportedTypes().FirstOrDefault(t => typeof(IStartup).IsAssignableFrom(t));
                if (startup != null)
                {
                    services.AddSingleton(typeof(IStartup), startup);

                }
                else
                {
                    throw new Exception($"Couldn't find a Startup class which is implementing IStartup in entry assembly {loadComponentsResult?.EntryAssembly?.FullName}");
                }
            }
        }

        internal static void UseHealthChecks(IApplicationBuilder app)
        {
            if (HealthCheckHelper.HealthCheckEnabled)
            {
                app.UseHealthChecks(HealthCheckHelper.HealthCheckRoute, HealthCheckHelper.HealthCheckPort);
            }
        }

        internal static void AddHealthChecks(IServiceCollection services)
        {
            if (HealthCheckHelper.HealthCheckEnabled)
            {
                HealthCheckHelper.AddHealthChecks(services);
            }
        }
    }
}
