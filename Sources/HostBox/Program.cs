using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common.Logging;
using Common.Logging.Configuration;
using HostBox.Configuration;
using HostBox.Loading;

#if !NETCOREAPP2_1
using Microsoft.AspNetCore.Hosting;
#endif

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostBox
{
    internal class Program
    {
        private const string ConfigurationNameEnvVariable = "configuration";
        private static ILog Logger { get; set; }

        private static async Task Main(string[] args = null)
        {
            Console.WriteLine("Main");
            CommandLineArgs commandLineArgs = null;
            try
            {
                commandLineArgs = CommandLineArgsProvider.Get(args);

                if (commandLineArgs.StartConfirmationRequired)
                {
                    Console.WriteLine("Press enter to start");
                    Console.ReadLine();
                }

                if (commandLineArgs.CommandLineArgsValid)
                {
                    var host = CreateHostBuilder(commandLineArgs)
                        .Build();

                    var manager = host.Services.GetService<HostedComponentsManager>();

                    await manager.RunComponents(CancellationToken.None);

                    await host.RunAsync();
                }
            }
            catch (Exception ex)
            {
                if (Logger != null)
                {
                    Logger.Fatal("Hosting failed.", ex);
                }
                else
                {
                    Console.WriteLine($"Error: {ex}");
                }

                throw;
            }
            finally
            {
                if (commandLineArgs?.FinishConfirmationRequired ?? false)
                {
                    Console.WriteLine("Press enter to finish");
                    Console.ReadLine();
                }
            }
        }
        
        private static IHostBuilder CreateHostBuilder(CommandLineArgs commandLineArgs)
        {
            Console.WriteLine("Create host builder");
            var componentPath = Path.GetFullPath(commandLineArgs.Path, Directory.GetCurrentDirectory());
            
            var builder = new HostBuilder()
                .ConfigureHostConfiguration(
                    config =>
                    {
                        config.AddEnvironmentVariables();

                        config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

                        config.AddJsonFile("hostsettings.json", true, false);

                        ConfigureLogging(config.Build());
                        
                        Logger = LogManager.GetLogger<Program>();

                        Logger.Trace(m => m("Starting hostbox."));
                    })
                .ConfigureAppConfiguration(
                    (ctx, config) =>
                    {
                        LoadConfiguration(ctx.Configuration, config, componentPath, commandLineArgs);
                    })
                .ConfigureServices(
                    (ctx, services) =>
                    {
                        Directory.SetCurrentDirectory(Path.GetDirectoryName(componentPath));

                        var loadComponentsResult = new ComponentsLoader(
                            new ComponentConfig
                                {
                                    Path = componentPath,
                                    SharedLibraryPath = commandLineArgs.SharedLibrariesPath
                                }).LoadComponents(ctx.Configuration);

#if !NETCOREAPP2_1
                        if (commandLineArgs.Web)
                        {
                            var startup = loadComponentsResult?.EntryAssembly?.GetExportedTypes().FirstOrDefault(t => typeof(IStartup).IsAssignableFrom(t));
                            if (startup != null)
                            {
                                services.AddSingleton(typeof(IStartup), startup);
                            }
                            else
                            {
                                Logger.Error(m => m("Couldn't find a Startup class which is implementing IStartup"));
                            }
                        }
#endif
                        services.AddSingleton(ctx.Configuration.GetSection("host:components").Get<HostComponentsConfiguration>()
                                              ?? new HostComponentsConfiguration());

                        services.AddSingleton(new HostedComponentsManager(loadComponentsResult.Components));

                        services.AddHostedService<HostableComponentsFinalizer>();
                        services.AddHostedService<ApplicationLifetimeLogger>();
                    });

#if !NETCOREAPP2_1
            if (commandLineArgs.Web)
            {
                builder
                    .ConfigureWebHostDefaults(b =>
                    {
                        b.UseStartup<Startup>();
                    });
            }
#endif

            return builder;
        }
        
        private static void ConfigureLogging(IConfiguration config)
        {
            var logConfiguration = new LogConfiguration();
            config.GetSection("common:logging").Bind(logConfiguration);
            LogManager.Configure(logConfiguration);
        }
        
        private static void LoadConfiguration(IConfiguration currentConfiguration, IConfigurationBuilder config, string componentPath, CommandLineArgs args)
        {
            Logger.Trace(m => m("Loading hostable component using path [{0}].", componentPath));

            var componentBasePath = Path.GetDirectoryName(componentPath);

            config.SetBasePath(componentBasePath);

            var configName = currentConfiguration[ConfigurationNameEnvVariable];

            Logger.Info(m => m("Application was launched with configuration '{0}'.", configName));

            config.LoadSharedLibraryConfigurationFiles(Logger, componentBasePath, args.SharedLibrariesPath);

            var configProvider = new ConfigFileNamesProvider(configName, componentBasePath, args.SharedLibrariesPath);

            var valuesBuilder = new ConfigurationBuilder();
            foreach (var valuesFile in configProvider.GetTemplateValuesFiles())
            {
                Logger.Trace(m => m($"Loading values file: {valuesFile}"));
                valuesBuilder.AddJsonFile(valuesFile, optional: true, false);
            }

            var valuesConfigProviders = valuesBuilder.Build().Providers;
            foreach (var configFile in configProvider.EnumerateConfigFiles())
            {
                config.AddJsonTemplateFile(
                    configFile,
                    optional: false,
                    reloadOnChange: false,
                    valuesConfigProviders,
                    args.PlaceholderPattern);

                Logger.Trace(m => m("Configuration file [{0}] is loaded.", configFile));
            }

            var reloadOnChangeSettings = config.Build().GetSection("shared-libraries:gems.app:reload-on-change-settings").Get<IReadOnlyCollection<string>>();
            if (reloadOnChangeSettings != null)
            {
                foreach (var source in config.Sources.OfType<JsonTemplateConfigurationSource>())
                {
                    if (reloadOnChangeSettings.Contains(source.Path, StringComparer.OrdinalIgnoreCase))
                    {
                        source.ReloadOnChange = true;
                    }
                }
            }
        }
    }
}
