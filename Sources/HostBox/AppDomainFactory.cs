using System;

namespace HostBox
{
    internal class AppDomainFactory : IAppDomainFactory
    {
        public AppDomain CreateAppDomain(string componentPath, AppDomain hostDomain, AppDomainSetup appDomainSetup)
        {
            return AppDomain.CreateDomain(componentPath, hostDomain.Evidence, appDomainSetup);
        }
    }
}