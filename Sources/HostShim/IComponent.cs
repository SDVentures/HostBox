namespace HostShim
{
    /// <summary>
    /// Загружаемый компонент приложения.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Путь к исполняемому коду компонента.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Запускает компонент.
        /// </summary>
        void Start();

        /// <summary>
        /// Возобновляет работу загружаемого компонента.
        /// </summary>
        void Resume();

        /// <summary>
        /// Приостанавливает работу загружаемого компонента.
        /// </summary>
        void Pause();

        /// <summary>
        /// Останавливает работу загружаемого компонента.
        /// </summary>
        void Stop();
    }
}