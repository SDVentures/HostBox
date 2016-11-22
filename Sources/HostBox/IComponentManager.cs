namespace HostBox
{
    /// <summary>
    /// Component manager
    /// Performs component loading and unloading, start and stop it 
    /// </summary>
    public interface IComponentManager
    {
        /// <summary>
        /// Load the component into the application based on configuration
        /// </summary>
        /// <param name="configuration"> Application configuration </param>
        void LoadComponents(IApplicationConfiguration configuration);

        /// <summary>
        /// Unload the component into the application based on configuration
        /// Components unloading in the reverse order of the loading
        /// </summary>
        /// <param name="configuration"> Application configuration </param>
        void UnloadComponents(IApplicationConfiguration configuration);

        /// <summary>
        /// Pause work of the component from the list of components based on configuration
        /// Components pausing in the reverse order of the loading
        /// </summary>
        /// <param name="configuration"> Application configuration </param>
        void PauseComponents(IApplicationConfiguration configuration);

        /// <summary>
        /// Resume work of the components
        /// </summary>
        /// <param name="configuration"> Application configuration </param>
        void ResumeComponents(IApplicationConfiguration configuration);
    }
}