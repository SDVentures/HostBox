using System.Collections.Generic;

namespace HostBox
{
    /// <summary>
    /// Application configuration factory
    /// </summary>
    internal interface IApplicationConfigurationFactory
    {
        /// <summary>
        /// Create initial configuration of the application
        /// </summary>
        /// <param name="componentPaths"> Components paths to executable code of the components </param>
        /// <returns> Initial configuration </returns>
        IApplicationConfiguration CreateApplicationConfiguration(IEnumerable<string> componentPaths);
    }
}