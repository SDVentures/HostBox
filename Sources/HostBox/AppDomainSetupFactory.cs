using System;
using System.IO;

namespace HostBox
{
    internal class AppDomainSetupFactory : IAppDomainSetupFactory
    {
        private const string ConfigFileName = "app.config";

        public AppDomainSetup CreateAppDomainSetup(string componentPath, AppDomain hostDomain)
        {
            var configurationFile = Path.Combine(componentPath, ConfigFileName);
            var appDomainSetup = new AppDomainSetup
                                     {
                                         ApplicationName = componentPath,
                                         ApplicationBase = componentPath,
                                         ConfigurationFile = configurationFile
                                     };

            return appDomainSetup;
        }
    }
}