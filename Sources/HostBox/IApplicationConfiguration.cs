using System.Collections.Generic;

namespace HostBox
{
    /// <summary>
    /// Application configuration.
    /// It is used for application execution monitoring 
    /// and provides the ability to change the behavior of the application at runtime
    /// </summary>
    public interface IApplicationConfiguration
    {
        /// <summary>
        /// Application components paths
        /// </summary>
        ICollection<string> ComponentPaths { get; set; }

        /// <summary>
        /// Responsible for managing loaded components:
        /// load, unload, start and stop components
        /// </summary>
        IComponentManager ComponentManager { get; set; }

        /// <summary>
        /// Collection of the configuration loaded components
        /// Provides the ability to change the behavior of the application at runtime
        /// </summary>
        ICollection<IComponentConfiguration> ComponentConfigurations { get; set; }
    }
}