using System.Collections.Generic;

namespace HostBox
{
    /// <summary>
    /// Фабрика конфигураций приложения.
    /// </summary>
    internal interface IApplicationConfigurationFactory
    {
        /// <summary>
        /// Создает первоначальную конфигурацию приложения.
        /// </summary>
        /// <param name="componentPaths">Список путей с исполняемым кодом загружаемых компонентов.</param>
        /// <returns>Первоначальная конфигурация приложения.</returns>
        IApplicationConfiguration CreateApplicationConfiguration(IEnumerable<string> componentPaths);
    }
}