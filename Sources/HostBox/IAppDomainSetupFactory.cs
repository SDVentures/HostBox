using System;

namespace HostBox
{
    /// <summary>
    /// Application domain configuration factory
    /// </summary>
    public interface IAppDomainSetupFactory
    {
        /// <summary>
        /// Create configuraton of the component application domain 
        /// </summary>
        /// <param name="componentPath"> Path to executable code of the component. </param>
        /// <param name="hostDomain"> Main application domain. </param>
        /// <returns> Configuration of the component application domain. </returns>
        AppDomainSetup CreateAppDomainSetup(string componentPath, AppDomain hostDomain);
    }
}