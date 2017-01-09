using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

using Common.Logging;

using Microsoft.Win32;

using Topshelf;
using Topshelf.Common.Logging;
using Topshelf.Runtime;

namespace HostBox
{
    internal class Program
    {
        private const string PathSeparator = ";";

        private const string ServiceName = "HostBox";

        private static readonly ILog Logger = LogManager.GetLogger<Program>();

        private static void Main()
        {
            var appDomainSetupFactory = new AppDomainSetupFactory();
            var appDomainFactory = new AppDomainFactory();

            var asyncStart = false;
            if (ConfigurationManager.AppSettings[nameof(asyncStart)] != null)
            {
                bool.TryParse(ConfigurationManager.AppSettings[nameof(asyncStart)], out asyncStart);
            }

            var componentFactory = new DomainComponentFactory(asyncStart);
            var componentManager = new ComponentManager(componentFactory, appDomainSetupFactory, appDomainFactory);
            var applicationConfigurationFactory = new ApplicationConfigurationFactory(componentManager);

            TaskScheduler.UnobservedTaskException += UnobservedTaskException;

            HostFactory.Run(
                configuration =>
                    {
                        IEnumerable<string> paths = new List<string>();
                        configuration.AddCommandLineDefinition(
                            "path",
                            value =>
                                {
                                    paths = value.Split(
                                        new[] { PathSeparator },
                                        StringSplitOptions.RemoveEmptyEntries).ToList();
                                });
                        configuration.UseCommonLogging();
                        configuration.AfterInstall(settings => AddServiceStartupOption(settings, paths));
                        configuration.SetServiceName(ServiceName);
                        configuration.SetDisplayName(ServiceName);
                        configuration.SetDescription(ServiceName);
                        configuration.Service<Application>(
                            service =>
                                {
                                    service.ConstructUsing(settings => new Application(paths, applicationConfigurationFactory));
                                    service.WhenStarted((application, control) => application.Start());
                                    service.WhenStopped((application, control) => application.Stop());
                                    service.WhenPaused((application, control) => application.Pause());
                                    service.WhenContinued((application, control) => application.Resume());
                                    service.WhenShutdown((application, control) => application.Shutdown());
                                });
                        configuration.ApplyCommandLine();
                    });
        }

        private static void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Logger.Warn(m => m("Unhandled exception was intercepted. The state of the system may be corrupted"), e.Exception);
        }

        private static void AddServiceStartupOption(InstallHostSettings settings, IEnumerable<string> paths)
        {
            using (RegistryKey system = Registry.LocalMachine.OpenSubKey("System"))
            {
                if (system == null)
                {
                    Logger.Error(@"Could not find the registry key HKEY_LOCAL_MACHINE\SYSTEM");
                    return;
                }

                using (RegistryKey currentControlSet = system.OpenSubKey("CurrentControlSet"))
                {
                    if (currentControlSet == null)
                    {
                        Logger.Error(@"Could not find the registry key HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet");
                        return;
                    }

                    using (RegistryKey services = currentControlSet.OpenSubKey("Services"))
                    {
                        if (services == null)
                        {
                            Logger.Error(@"Could not find the registry key HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services");
                            return;
                        }

                        using (RegistryKey service = services.OpenSubKey(settings.ServiceName, true))
                        {
                            if (service == null)
                            {
                                Logger.Error($@"Could not find the registry key HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\{settings.ServiceName}");
                                return;
                            }

                            var imagePath = (string)service.GetValue("ImagePath");
                            imagePath += " -path:" + string.Join(PathSeparator, paths);
                            service.SetValue("ImagePath", imagePath);
                        }
                    }
                }
            }
        }
    }
}