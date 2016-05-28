namespace HostBox
{
    /// <summary>
    /// Управляющий компонентами.
    /// Выполняет работы по загрузке компонентов, выгрузке компонентов, остановке и запуску.
    /// </summary>
    public interface IComponentManager
    {
        /// <summary>
        /// Загружает компоненты в приложение, на основе полученной конфигурации.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        void LoadComponents(IApplicationConfiguration configuration);

        /// <summary>
        /// Выгружает компоненты из приложения, которые указаны в конфигурации приложения. 
        /// Компоненты выгружаются в порядке обратном порядку загрузки приложения.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        void UnloadComponents(IApplicationConfiguration configuration);

        /// <summary>
        /// Приостанавливает работу компонентов приложения из списка компонентов в конфигурации приложения.
        /// Компоненты приостанавливают работу в порядке обратном порядку загрузки компонентов.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        void PauseComponents(IApplicationConfiguration configuration);

        /// <summary>
        /// Возобновляет работу компонентов приложения.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        void ResumeComponents(IApplicationConfiguration configuration);
    }
}