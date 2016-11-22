using System;

using HostShim;

namespace HostBox
{
    /// <summary>
    /// Configuration of loading component
    /// </summary>
    public class ComponentConfiguration : IComponentConfiguration
    {
        /// <summary>
        /// Domain of the loading component
        /// Uses when component loading in separate domain
        /// In cases when component loaded in the application domain may be <c>null</c> or equals to the main application domain
        /// </summary>
        public virtual AppDomain ComponentDomain { get; set; }

        /// <summary>
        /// Loading component
        /// Uses for component managing
        /// Responsible for the loading of the executable code into the application domain
        /// </summary>
        public virtual IComponent Component { get; set; }
    }
}