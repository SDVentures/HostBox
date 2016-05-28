using System;

namespace HostBox
{
    /// <summary>
    /// Фабрика конфигурации домена приложения компонента.
    /// </summary>
    public interface IAppDomainSetupFactory
    {
        /// <summary>
        /// Создает конфигурацию домена приложения компонента.
        /// </summary>
        /// <param name="componentPath">Путь, по которому находится исполняемый код загружаемого компонента приложения.</param>
        /// <param name="hostDomain">Основной домен приложения.</param>
        /// <returns>Конфигурация домена приложения компонента.</returns>
        AppDomainSetup CreateAppDomainSetup(string componentPath, AppDomain hostDomain);
    }
}