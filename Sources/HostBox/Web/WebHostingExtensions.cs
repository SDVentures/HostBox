using System;
using System.IO;
using System.Linq;
using Common.Logging;
using HostBox.Configuration.Healthcheck;
using HostBox.Loading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HostBox.Web
{
    internal static class HostboxWebExtensions
    {
        private static ILog Logger { get; set; }
        private static bool HealthCheckEnabled => probesConfig != null;
        private static ProbesConfig probesConfig = null;
        internal static void Initialize(string componentPath, string sharedLibrariesPath)
        {
            var componentBasePath = Path.GetDirectoryName(componentPath);
            var initialConfiguration = BuildInitialConfiguration(componentBasePath, sharedLibrariesPath);

            probesConfig = initialConfiguration.GetSection("probes").Get<ProbesConfig>();

            if (probesConfig == null)
            {
                return;
            }

            HealthCheckPort = probesConfig.HealthPort;
            HealthCheckRoute = probesConfig.HealthRoute;
            HealthCheckUrl = $"http://+:{HealthCheckPort}";
        }

        // Logger initialized after we already read configuration
        public static void ConfigureLogging()
        {
            Logger = LogManager.GetLogger("HostboxWebExtensions");

            if (probesConfig == null)
            {
                Logger.Info($"No healthcheck configured");
            }
            else
            {
                Logger.Info($"Healthcheck configured. Route {HealthCheckRoute}. Port {HealthCheckPort}");
            }
        }

        public static int HealthCheckPort = 9191;
        public static string HealthCheckUrl = $"http://+:{HealthCheckPort}";
        public static string HealthCheckRoute = "/health";

        public static void ConfigureWebHost(IHostBuilder builder, CommandLineArgs args)
        {
            if (args.Web)
            {
                builder
                    .ConfigureWebHostDefaults(b =>
                    {
                        b.UseStartup<Startup>();

                        if (HealthCheckEnabled)
                        {
                            var urlFromEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://+:5000";
                            b.UseUrls(urlFromEnv, HealthCheckUrl);
                        }
                    });
            }
            else
            {
                if (HealthCheckEnabled)
                {
                    builder.ConfigureWebHost(b =>
                    {
                        b.UseKestrel().UseUrls(HealthCheckUrl);
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
            if (HealthCheckEnabled)
                app.UseHealthChecks(HealthCheckRoute, HealthCheckPort);
        }

        internal static void AddHealthChecks(IServiceCollection services)
        {
            if (!HealthCheckEnabled)
                return;

            var healthCheckBuilder = services.AddHealthChecks();

            foreach (var (className, config) in probesConfig.KnownChecks)
            {
                var healthCheckType = Type.GetType(className);
                if (healthCheckType != null)
                {
                    healthCheckBuilder
                      .Add(new HealthCheckRegistration(
                            config.Name,
                            s => (IHealthCheck)ActivatorUtilities.GetServiceOrCreateInstance(s, healthCheckType),
                            failureStatus: null, // null by default, like in AddCheck
                            tags: null));
                }
                else
                {
                    Logger.Error($"Healthcheck type not found: [{className}]");
                }
            }
        }


        private static IConfiguration BuildInitialConfiguration(string componentPath, string sharedLibrariesDir)
        {
            var appSettingsFile = Path.Combine(componentPath, sharedLibrariesDir, "gems.app.settings.json");

            var initialConfiguration = new ConfigurationBuilder()
                        .AddJsonFile(appSettingsFile, optional: true)
                        .Build();

            return initialConfiguration;
        }
    }

    internal class TestExtension
    {
        public int MyProperty { get; set; } = 10;
    }
}
