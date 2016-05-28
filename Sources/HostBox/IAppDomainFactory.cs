using System;

namespace HostBox
{
    /// <summary>
    /// Фабрика домена приложения загружаемого компонента.
    /// </summary>
    public interface IAppDomainFactory
    {
        /// <summary>
        /// Создает домен приложения загружаемого компонента.
        /// </summary>
        /// <param name="componentPath">Путь к исполняемому коду загружаемого компонента.</param>
        /// <param name="hostDomain">Основной домен приложения.</param>
        /// <param name="appDomainSetup">Конфигурация создаваемого домена приложения для загружаемого компонента.</param>
        /// <returns>Домен приложения для загружаемого компонента.</returns>
        AppDomain CreateAppDomain(string componentPath, AppDomain hostDomain, AppDomainSetup appDomainSetup);
    }
}