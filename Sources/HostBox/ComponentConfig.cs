namespace HostBox
{
    /// <summary>
    /// Component loading configuration.
    /// </summary>
    public class ComponentConfig
    {
        /// <summary>
        /// Gets or sets path to domain binaries.
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Gets or sets path to shared libraries binaries.
        /// </summary>
        public string SharedLibraryPath { get; set; }
    }
}