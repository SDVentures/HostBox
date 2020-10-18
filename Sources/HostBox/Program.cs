using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Common.Logging;
using Common.Logging.Configuration;

using HostBox.Configuration;

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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
            CommandLineArgs commandLineArgs = null;
            try
            {
                commandLineArgs = GetCommandLineArgs(args);
                
                if (commandLineArgs.StartConfirmationRequired)
                {
                    Console.WriteLine("Press enter to start");
                    Console.ReadLine();
                }

                if (commandLineArgs.CommandLineArgsValid)
                {
                    IHostBuilder builder = PrepareHostBuilder(commandLineArgs);
                    await RunHost(builder);
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

        private static async Task RunHost(IHostBuilder builder)
        {
            using (IHost host = builder.Build())
            {
                try
                {
                    await host.StartAsync();

                    try
                    {
                        await host.WaitForShutdownAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.Fatal("Unable to stop host graceful due exception.", e);
                    }
                }
                catch (Exception e)
                {
                    Logger.Fatal("Unable to start host due exception.", e);
                }
            }
        }

        private static IHostBuilder PrepareHostBuilder(CommandLineArgs args)
        {
            var componentPath = Path.GetFullPath(args.Path, Directory.GetCurrentDirectory());

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
                        Logger.Trace(m => m("Loading hostable component using path [{0}].", componentPath));

                        var componentBasePath = Path.GetDirectoryName(componentPath);

                        config.SetBasePath(componentBasePath);

                        var configName = ctx.Configuration[ConfigurationNameEnvVariable];

                        Logger.Info(m => m("Application was launched with configuration '{0}'.", configName));

                        config.LoadSharedLibraryConfigurationFiles(Logger, componentBasePath,
                            args.SharedLibrariesPath);

                        var configProvider = new ConfigFileNamesProvider(configName, componentBasePath);

                        var templateValuesSource =
                            new JsonConfigurationSource
                            {
                                Path = configProvider.GetTemplateValuesFile(),
                                FileProvider = null,
                                ReloadOnChange = false,
                                Optional = true
                            };

                        templateValuesSource.ResolveFileProvider();

                        var templateValuesProvider = templateValuesSource.Build(config);

                        templateValuesProvider.Load();

                        foreach (var configFile in configProvider.EnumerateConfigFiles())
                        {
                            config.AddJsonTemplateFile(configFile, false, false, templateValuesProvider,
                                args.PlaceholderPattern);

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
                                SharedLibraryPath = args.SharedLibrariesPath,
                                LoggerFactory = LogManager.GetLogger
                            });

                        services.AddSingleton<IHostedService, Application>();
                    });

            return builder;
        }

        private static CommandLineArgs GetCommandLineArgs(string[] source)
        {
            var cmdLnApp = new CommandLineApplication (false) { Name = "host", FullName = "HostBox" };

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

            var patternOpt = cmdLnApp.Option(
                "--placeholder-pattern",
                "Pattern of placeholders to find and replace into the component configuration (default is '!{*}')",
                CommandOptionType.SingleValue);
            
            var confirmStartOpt = cmdLnApp.Option(
                "-cs|--confirm-start",
                "Requirement to ask for confirmation before starting the application",
                CommandOptionType.NoValue);
            
            var confirmFinishOpt = cmdLnApp.Option(
                "-cf|--confirm-finish",
                "Requirement to ask for confirmation before terminating the application",
                CommandOptionType.NoValue);

            var defaultSharedPath =  Environment.GetEnvironmentVariable("SHARED_LIBRARIES_PATH") ?? Path.Combine("..", "shared", "libraries");
            var sharedOpt = cmdLnApp.Option(
                "-slp|--shared-libraries-path",
                $"Directory path where additional dll dependencies located (resolved under component directory, default is '{defaultSharedPath}'. Use the environment variable 'SHARED_LIBRARIES_PATH' to override the default..)",
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

                        if (!patternOpt.HasValue())
                        {
                            patternOpt.Values.Add("!{*}");
                        }

                        if (!sharedOpt.HasValue())
                        {
                            sharedOpt.Values.Add(defaultSharedPath);
                        }

                        return 0;
                    });

            CommandLineArgs cmdLnArgs= new CommandLineArgs();
                
            try
            {
                cmdLnApp.Execute(source);

                cmdLnArgs.Path = pathOpt.Value();
                cmdLnArgs.PlaceholderPattern = patternOpt.Value();
                cmdLnArgs.SharedLibrariesPath = sharedOpt.Value();
                cmdLnArgs.StartConfirmationRequired = confirmStartOpt.HasValue();
                cmdLnArgs.FinishConfirmationRequired = confirmFinishOpt.HasValue();
                
                if (cmdLnApp.RemainingArguments?.Count > 0)
                {
                    Console.WriteLine($"Unparsed args: {string.Join(",", cmdLnApp.RemainingArguments)}");
                }
                else
                {
                    cmdLnArgs.CommandLineArgsValid = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error:{e}");
            }

            if (!cmdLnArgs.CommandLineArgsValid)
            {
                cmdLnApp.ShowHelp();
            }

            return cmdLnArgs;
        }

        private static void ConfigureLogging(IConfiguration config)
        {
            var logConfiguration = new LogConfiguration();

            config.GetSection("common:logging").Bind(logConfiguration);

            LogManager.Configure(logConfiguration);
        }

        private class CommandLineArgs
        {
            public bool CommandLineArgsValid;
            
            public string Path { get; set; }

            public string PlaceholderPattern { get; set; }

            public string SharedLibrariesPath { get; set; }
            
            public bool StartConfirmationRequired { get; set; }
            
            public bool FinishConfirmationRequired { get; set; }
        }
    }
}
