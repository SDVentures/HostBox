using System;

using Common.Logging;

namespace HostBox
{
    /// <summary>
    /// Component loading configuration.
    /// </summary>
    public class ComponentConfig
    {
        /// <summary>
        /// Gets or sets path to component entry assembly.
        /// </summary>
        public string EntryAssembly { get; set; }

        /// <summary>
        /// Gets or sets path to domain binaries.
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Gets or sets path to shared libraries binaries.
        /// </summary>
        public string SharedLibraryPath { get; set; }

        /// <summary>
        /// Gets or sets logger factory function.
        /// </summary>
        public Func<Type, ILog> LoggerFactory { get; set; }
    }
}