using System.Collections.Generic;

namespace HostBox
{
    /// <summary>
    /// Application Configuration
    /// Uses to control the progress of the application and provides 
    /// ability to change application behaviour during execution
    /// </summary>
    public class ApplicationConfiguration : IApplicationConfiguration
    {
        /// <summary>
        /// Paths to application loading components
        /// </summary>
        public virtual ICollection<string> ComponentPaths { get; set; }

        /// <summary>
        /// Responsible for managing loading components:
        /// loading, unloading, starting and stopping 
        /// </summary>
        public virtual IComponentManager ComponentManager { get; set; }

        /// <summary>
        /// Collection of the configuration of loaded components
        /// Uses to control the progress of the application and provides 
        /// ability to change application behaviour during execution
        /// </summary>
        public virtual ICollection<IComponentConfiguration> ComponentConfigurations { get; set; }
    }
}