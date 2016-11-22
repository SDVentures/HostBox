using System;

namespace HostBox
{
    /// <summary>
    /// Component application domain factory
    /// </summary>
    public interface IAppDomainFactory
    {
        /// <summary>
        /// Create application domain of the loading component
        /// </summary>
        /// <param name="componentPath"> Path to executable code of the component. </param>
        /// <param name="hostDomain"> Main application domain. </param>
        /// <param name="appDomainSetup"> Application domain configuration for the loading component. </param>
        /// <returns> Application domain for the loading component. </returns>
        AppDomain CreateAppDomain(string componentPath, AppDomain hostDomain, AppDomainSetup appDomainSetup);
    }
}