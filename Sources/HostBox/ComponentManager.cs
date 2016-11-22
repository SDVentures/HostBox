using System;
using System.Linq;

using Common.Logging;

namespace HostBox
{
    /// <summary>
    /// Managing components
    /// Performs loading, unloading, stopping and starting components
    /// </summary>
    public class ComponentManager : IComponentManager
    {
        private static readonly ILog Logger = LogManager.GetLogger<ComponentManager>();

        private readonly IComponentFactory componentFactory;

        private readonly IAppDomainSetupFactory appDomainSetupFactory;

        private readonly IAppDomainFactory appDomainFactory;

        /// <summary>
        /// Initializes a new instance of the class <see cref="ComponentManager"/>.
        /// </summary>
        /// <param name="componentFactory"> Component factory. </param>
        /// <param name="appDomainSetupFactory"> Application domain configuration factory, in which component loads. </param>
        /// <param name="appDomainFactory"> Component application domain factory. </param>
        public ComponentManager(IComponentFactory componentFactory, IAppDomainSetupFactory appDomainSetupFactory, IAppDomainFactory appDomainFactory)
        {
            this.componentFactory = componentFactory;
            this.appDomainSetupFactory = appDomainSetupFactory;
            this.appDomainFactory = appDomainFactory;
        }

        /// <summary>
        /// Load components into application, based on the received configuration.
        /// </summary>
        /// <param name="configuration"> Application configuration. </param>
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
        /// Unload component from the application, which are mentioned in the application configuration
        /// Components unloading in the reverse order of the loading
        /// </summary>
        /// <param name="configuration"> Application configuration. </param>
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
        /// Pause work of the component from the list of components based on configuration
        /// Components pausing in the reverse order of the loading
        /// </summary>
        /// <param name="configuration"> Application configuration. </param>
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
        /// Resume work of the components
        /// </summary>
        /// <param name="configuration"> Application configuration. </param>
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
        /// Load component based on given configuration
        /// </summary>
        /// <param name="configuration"> Application configuration. </param>
        /// <param name="componentPath"> Path to executable code of the component. </param>
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
        /// Unload component
        /// </summary>
        /// <param name="componentConfiguration"> Configuration of the component to be unloaded. </param>
        protected virtual void UnloadComponent(IComponentConfiguration componentConfiguration)
        {
            Logger.Info(m => m("Выгружается компонент из {0}", componentConfiguration.ComponentDomain.FriendlyName));
            componentConfiguration.Component.Stop();
            AppDomain.Unload(componentConfiguration.ComponentDomain);
        }
    }
}