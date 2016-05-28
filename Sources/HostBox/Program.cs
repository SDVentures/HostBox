using System;
using System.Collections.Generic;
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

        private static readonly ILog Logger = LogManager.GetLogger<Program>();

        private static void Main()
        {
            var appDomainSetupFactory = new AppDomainSetupFactory();
            var appDomainFactory = new AppDomainFactory();
            var componentFactory = new DomainComponentFactory();
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
                        configuration.SetServiceName("HostBox");
                        configuration.SetDisplayName("HostBox");
                        configuration.SetDescription("HostBox");
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
            Logger.Warn("Перехват необработанного исключения. Возможно, состояние системы повреждено.", e.Exception);
        }

        private static void AddServiceStartupOption(InstallHostSettings settings, IEnumerable<string> paths)
        {
            using (RegistryKey system = Registry.LocalMachine.OpenSubKey("System"))
            {
                if (system == null)
                {
                    Logger.Error(@"Не удалось найти в системном реестре ключ HKEY_LOCAL_MACHINE\SYSTEM");
                    return;
                }

                using (RegistryKey currentControlSet = system.OpenSubKey("CurrentControlSet"))
                {
                    if (currentControlSet == null)
                    {
                        Logger.Error(@"Не удалось найти в системно реестре ключ HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet");
                        return;
                    }

                    using (RegistryKey services = currentControlSet.OpenSubKey("Services"))
                    {
                        if (services == null)
                        {
                            Logger.Error(@"Не удалось найти в системно реестре ключ HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services");
                            return;
                        }

                        using (RegistryKey service = services.OpenSubKey(settings.ServiceName, true))
                        {
                            if (service == null)
                            {
                                Logger.Error(@"Не удалось найти в системно реестре ключ HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\" + settings.ServiceName);
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