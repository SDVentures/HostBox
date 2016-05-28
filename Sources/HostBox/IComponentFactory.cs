using System;

using HostShim;

namespace HostBox
{
    /// <summary>
    /// Фабрика загружаемых компонентов.
    /// </summary>
    public interface IComponentFactory
    {
        /// <summary>
        /// Создает загружаемый компонент.
        /// </summary>
        /// <param name="componentPath">Путь с исполняемым кодом загружаемого компонента.</param>
        /// <param name="targetDomain">Целевой домен приложения, в который загружается компонент.</param>
        /// <param name="hostDomain">Основной домен приложения.</param>
        /// <returns>Загружаемый компонент.</returns>
        IComponent CreateComponent(string componentPath, AppDomain targetDomain, AppDomain hostDomain);
    }
}