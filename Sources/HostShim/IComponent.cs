namespace HostShim
{
    /// <summary>
    /// Application component
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Path to the executable code of the component 
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Starts the component
        /// </summary>
        void Start();

        /// <summary>
        /// Resumes the component
        /// </summary>
        void Resume();

        /// <summary>
        /// Pauses the component
        /// </summary>
        void Pause();

        /// <summary>
        /// Stops the component
        /// </summary>
        void Stop();
    }
}