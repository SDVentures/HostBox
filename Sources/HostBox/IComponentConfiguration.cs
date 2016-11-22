using System;

using HostShim;

namespace HostBox
{
    /// <summary>
    /// Component configuration
    /// It is used to manage component execution
    /// </summary>
    public interface IComponentConfiguration
    {
        /// <summary>
        /// Component application domaint
        /// It is used when component is loading into the separate application domain
        /// In case, when component was loaded into the main domain may be null or equals to the main application domain
        /// </summary>
        AppDomain ComponentDomain { get; set; }

        /// <summary>
        /// Application component
        /// It is used for component management
        /// Responsible for loading executable code into the application domain
        /// </summary>
        IComponent Component { get; set; }
    }
}