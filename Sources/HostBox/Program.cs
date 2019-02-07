using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Common.Logging;
using Common.Logging.Configuration;

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostBox
{
    internal class Program
    {
        private static ILog Logger { get; set; }

        private static async Task Main(string[] args = null)
        {
            var commandLineArgs = GetCommandLineArgs(args);

            if (commandLineArgs == null)
            {
                return;
            }

            var componentPath = Path.GetFullPath(commandLineArgs.Path, Directory.GetCurrentDirectory());

            var builder = new HostBuilder()
                .ConfigureHostConfiguration(
                    config =>
                        {
                            config.AddEnvironmentVariables();

                            config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

                            config.AddJsonFile("hostsettings.json", true, true);

                            ConfigureLogging(config.Build());

                            Logger = LogManager.GetLogger<Program>();

                            Logger.Trace(m => m("Starting hostbox."));
                        })
                .ConfigureAppConfiguration(
                    (ctx, config) =>
                        {
                            Logger.Trace(m => m("Loading hostable component using path [{0}].", componentPath));

                            var componentBasePath = Path.GetDirectoryName(componentPath);

                            config.SetBasePath(componentBasePath);

                            var configProvider = new ConfigFileNamesProvider(ctx.HostingEnvironment, componentBasePath);
                            var configFiles = configProvider.EnumerateConfigFiles();

                            foreach (var configFile in configFiles)
                            {
                                config.AddJsonFile(configFile, false, true);

                                Logger.Trace(m => m("Configuration file [{0}] is loaded.", configFile));
                            }
                        })
                .ConfigureServices(
                    (ctx, services) =>
                        {
                            services
                                .AddSingleton(provider => new ComponentConfig
                                                              {
                                                                  Path = componentPath,
                                                                  LoggerFactory = LogManager.GetLogger
                                                              });

                            services.AddSingleton<IHostedService, Application>();
                        });

            using (var host = builder.Build())
            {
                try
                {
                    await host.StartAsync();
                }
                catch (Exception e)
                {
                    Logger.Fatal("Unable to start host due exception.", e);
                    return;
                }

                try
                {
                    await host.WaitForShutdownAsync();
                }
                catch (Exception e)
                {
                    Logger.Fatal("Unable to stop host graceful due exception.", e);
                }
            }
        }

        private static CommandLineArgs GetCommandLineArgs(string[] source)
        {
            var cmdLnApp = new CommandLineApplication { Name = "host", FullName = "HostBox" };

            cmdLnApp.ShortVersionGetter = cmdLnApp.LongVersionGetter = () =>
                {
                    var executingAssembly = Assembly.GetExecutingAssembly();

                    var attr = executingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                    return attr?.InformationalVersion ?? "1.0.0.0";
                };

            var pathOpt = cmdLnApp.Option(
                "-p|--path <path>",
                "Path to hostable component",
                CommandOptionType.SingleValue);

            cmdLnApp.VersionOption("-v|--version", cmdLnApp.ShortVersionGetter, cmdLnApp.LongVersionGetter);

            cmdLnApp.HelpOption("-?|-h|--help");

            cmdLnApp.OnExecute(
                () =>
                    {
                        if (!pathOpt.HasValue())
                        {
                            cmdLnApp.ShowHelp();
                        }

                        return 0;
                    });

            try
            {
                cmdLnApp.Execute(source);
            }
            catch (Exception)
            {
                cmdLnApp.ShowHelp();
                return null;
            }

            return cmdLnApp.IsShowingInformation 
                       ? null 
                       : new CommandLineArgs { Path = pathOpt.Value() };
        }

        private static void ConfigureLogging(IConfiguration config)
        {
            var logConfiguration = new LogConfiguration();

            config.GetSection("common:logging").Bind(logConfiguration);

            LogManager.Configure(logConfiguration);
        }

        private class CommandLineArgs
        {
            public string Path { get; set; }
        }
    }
}
