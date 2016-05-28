using System;
using System.Linq;

using Common.Logging;

namespace HostBox
{
    /// <summary>
    /// Управляющий компонентами.
    /// Выполняет работы по загрузке компонентов, выгрузке компонентов, остановке и запуску.
    /// </summary>
    public class ComponentManager : IComponentManager
    {
        private static readonly ILog Logger = LogManager.GetLogger<ComponentManager>();

        private readonly IComponentFactory componentFactory;

        private readonly IAppDomainSetupFactory appDomainSetupFactory;

        private readonly IAppDomainFactory appDomainFactory;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ComponentManager"/>.
        /// </summary>
        /// <param name="componentFactory">Фабрика компонентов.</param>
        /// <param name="appDomainSetupFactory">Фабрика конфигурации домена приложений, в который загружается компонент.</param>
        /// <param name="appDomainFactory">Фабрика домена приложений загружаемых компонентов.</param>
        public ComponentManager(IComponentFactory componentFactory, IAppDomainSetupFactory appDomainSetupFactory, IAppDomainFactory appDomainFactory)
        {
            this.componentFactory = componentFactory;
            this.appDomainSetupFactory = appDomainSetupFactory;
            this.appDomainFactory = appDomainFactory;
        }

        /// <summary>
        /// Загружает компоненты в приложение, на основе полученной конфигурации.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        public virtual void LoadComponents(IApplicationConfiguration configuration)
        {
            if (configuration.ComponentPaths.Count == 0)
            {
                Logger.Warn(m => m("Нет компонентов для загрузки."));
            }

            foreach (var componentPath in configuration.ComponentPaths)
            {
                string path = componentPath;
                this.LoadComponent(configuration, path);
            }
        }

        /// <summary>
        /// Выгружает компоненты из приложения, которые указаны в конфигурации приложения. 
        /// Компоненты выгружаются в порядке обратном порядку загрузки приложения.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        public virtual void UnloadComponents(IApplicationConfiguration configuration)
        {
            foreach (var componentConfiguration in configuration.ComponentConfigurations.Reverse())
            {
                var cfg = componentConfiguration;
                try
                {
                    this.UnloadComponent(cfg);
                }
                catch (Exception exception)
                {
                    Logger.Error(m => m("Выгрузка компонента '{0}' завершилась провалом.", cfg.ComponentDomain.FriendlyName), exception);
                }
            }

            configuration.ComponentConfigurations.Clear();
        }

        /// <summary>
        /// Приостанавливает работу компонентов приложения из списка компонентов в конфигурации приложения.
        /// Компоненты приостанавливают работу в порядке обратном порядку загрузки компонентов.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        public virtual void PauseComponents(IApplicationConfiguration configuration)
        {
            foreach (var componentConfiguration in configuration.ComponentConfigurations.Reverse())
            {
                var cfg = componentConfiguration;
                Logger.Info(m => m("Приостанавливается работа компонента из {0}", cfg.ComponentDomain.FriendlyName));
                componentConfiguration.Component.Pause();
            }
        }

        /// <summary>
        /// Возобновляет работу компонентов приложения.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        public virtual void ResumeComponents(IApplicationConfiguration configuration)
        {
            foreach (var componentConfiguration in configuration.ComponentConfigurations)
            {
                var cfg = componentConfiguration;
                Logger.Info(m => m("Приостанавливается работа компонента из {0}", cfg.ComponentDomain.FriendlyName));
                componentConfiguration.Component.Resume();
            }
        }

        /// <summary>
        /// Загружает компонент на основе предоставленной конфигурации.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <param name="componentPath">Путь с исполняемым кодом компонента.</param>
        protected virtual void LoadComponent(IApplicationConfiguration configuration, string componentPath)
        {
            Logger.Info(m => m("Загружается компонент из {0}", componentPath));

            AppDomainSetup appDomainSetup = this.appDomainSetupFactory.CreateAppDomainSetup(componentPath, AppDomain.CurrentDomain);
            AppDomain appDomain = this.appDomainFactory.CreateAppDomain(componentPath, AppDomain.CurrentDomain, appDomainSetup);
            var component = this.componentFactory.CreateComponent(componentPath, appDomain, AppDomain.CurrentDomain);

            component.Start();

            var componentConfiguration = new ComponentConfiguration
                                             {
                                                 ComponentDomain = appDomain,
                                                 Component = component
                                             };
            configuration.ComponentConfigurations.Add(componentConfiguration);
        }

        /// <summary>
        /// Выгружает компонент.
        /// </summary>
        /// <param name="componentConfiguration">Конфигурация компонента подлежащего выгрузке.</param>
        protected virtual void UnloadComponent(IComponentConfiguration componentConfiguration)
        {
            Logger.Info(m => m("Выгружается компонент из {0}", componentConfiguration.ComponentDomain.FriendlyName));
            componentConfiguration.Component.Stop();
            AppDomain.Unload(componentConfiguration.ComponentDomain);
        }
    }
}