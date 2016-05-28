using System.Collections.Generic;

namespace HostBox
{
    /// <summary>
    /// Конфигурация приложения. 
    /// Используется для контроля за ходом выполнения приложения и обеспечивает 
    /// возможность менять поведение приложения во время выполнения.
    /// </summary>
    public class ApplicationConfiguration : IApplicationConfiguration
    {
        /// <summary>
        /// Пути к загружаемым компонентам приложения.
        /// </summary>
        public virtual ICollection<string> ComponentPaths { get; set; }

        /// <summary>
        /// Ответственный за управление загружаемыми компонентами.
        /// Отвечает за загрузку, выгрузку, запуск и остановку компонентов.
        /// </summary>
        public virtual IComponentManager ComponentManager { get; set; }

        /// <summary>
        /// Коллекция конфигураций загруженных компонентов.
        /// Которые используются для контроля за кодом выполнения загружаемого компонента.
        /// Обеспечивают возможность менять поведение загружаемого компонента во время выполнения.
        /// </summary>
        public virtual ICollection<IComponentConfiguration> ComponentConfigurations { get; set; }
    }
}