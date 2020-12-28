using System;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.CommandLineUtils;

namespace HostBox
{
    internal class CommandLineArgsProvider
    {
        public static CommandLineArgs Get(string[] source)
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
#if NETCOREAPP3_1
            var webOpt = cmdLnApp.Option(
                "-w|--web",
                "Runs HostBox as a web application",
                CommandOptionType.NoValue);
#endif

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

            var cmdLnArgs= new CommandLineArgs();

            try
            {
                cmdLnApp.Execute(source);

                cmdLnArgs.Path = pathOpt.Value();
                cmdLnArgs.PlaceholderPattern = patternOpt.Value();
                cmdLnArgs.SharedLibrariesPath = sharedOpt.Value();
                cmdLnArgs.StartConfirmationRequired = confirmStartOpt.HasValue();
                cmdLnArgs.FinishConfirmationRequired = confirmFinishOpt.HasValue();

#if NETCOREAPP3_1
                cmdLnArgs.Web = webOpt.HasValue();
#endif

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
    }
}