using System;

using HostShim;

namespace HostBox
{
    /// <summary>
    /// Конфигурация загружаемых компонентов приложения.
    /// </summary>
    public class ComponentConfiguration : IComponentConfiguration
    {
        /// <summary>
        /// Домен приложения загружаемого компонента.
        /// Используется в случае, когда компонент загружается в независимый от основного приложения домен приложения.
        /// В случае загрузки в основной домен приложения, может быть либо <c>null</c>, либо равен основному домену приложения.
        /// </summary>
        public virtual AppDomain ComponentDomain { get; set; }

        /// <summary>
        /// Загружаемый компонент приложения.
        /// Используется для управления компонентом.
        /// Отвечает за загрузку исполняемого кода компонента в домен приложений.
        /// </summary>
        public virtual IComponent Component { get; set; }
    }
}