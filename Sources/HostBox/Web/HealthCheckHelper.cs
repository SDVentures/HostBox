using System;
using System.IO;
using Common.Logging;
using HostBox.Configuration.Healthcheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HostBox.Web
{
    internal static class HealthCheckHelper
    {
        private static ILog Logger { get; set; }
        public static bool HealthCheckEnabled => ProbesConfig != null;
        public static ProbesConfig ProbesConfig { get; private set; } = null;
        public static int HealthCheckPort { get; private set; } = 9191;
        public static string HealthCheckUrl { get; private set; } = $"http://+:{HealthCheckPort}";
        public static string HealthCheckRoute { get; private set; } = "/health";

        internal static void Initialize(string componentPath, string sharedLibrariesPath)
        {
            var componentBasePath = Path.GetDirectoryName(componentPath);
            var initialConfiguration = BuildInitialConfiguration(componentBasePath, sharedLibrariesPath);

            ProbesConfig = initialConfiguration.GetSection("probes").Get<ProbesConfig>();

            if (ProbesConfig == null)
            {
                return;
            }

            HealthCheckPort = ProbesConfig.HealthPort;
            HealthCheckRoute = ProbesConfig.HealthRoute;
            HealthCheckUrl = $"http://+:{HealthCheckPort}";
        }

        // Logger initialized after we already read configuration
        public static void ConfigureLogging()
        {
            Logger = LogManager.GetLogger("HostboxWebExtensions");

            if (ProbesConfig == null)
            {
                Logger.Info($"No healthcheck configured");
            }
            else
            {
                Logger.Info($"Healthcheck configured. Route {HealthCheckRoute}. Port {HealthCheckPort}");
            }
        }

        public static void AddHealthChecks(IServiceCollection services)
        {

            var healthCheckBuilder = services.AddHealthChecks();

            foreach (var (className, config) in HealthCheckHelper.ProbesConfig.KnownChecks)
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
}
