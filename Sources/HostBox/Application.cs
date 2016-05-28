using System;
using System.Collections.Generic;
using System.Reflection;

using Common.Logging;

namespace HostBox
{
    internal class Application
    {
        private static readonly ILog Logger = LogManager.GetLogger<Application>();

        private readonly IEnumerable<string> componentPaths;

        private readonly IApplicationConfigurationFactory applicationConfigurationFactory;

        private IApplicationConfiguration configuration;

        public Application(IEnumerable<string> componentPaths, IApplicationConfigurationFactory applicationConfigurationFactory)
        {
            this.componentPaths = componentPaths;
            this.applicationConfigurationFactory = applicationConfigurationFactory;
        }

        public bool Start()
        {
            Logger.Info(m => m("Запуск приложения."));
            Logger.Debug(m => m("Для загрузки указаны компоненты из директорий: {0}", string.Join(";", this.componentPaths)));

            try
            {
                this.configuration = this.applicationConfigurationFactory.CreateApplicationConfiguration(this.componentPaths);

                this.configuration.ComponentManager.LoadComponents(this.configuration);
            }
            catch (ReflectionTypeLoadException exception)
            {
                if (exception.LoaderExceptions != null)
                {
                    foreach (var loaderException in exception.LoaderExceptions)
                    {
                        var currentException = loaderException;
                        Logger.Fatal(m => m("Ошибка загрузки типа"), currentException);
                    }
                }

                Logger.Fatal(m => m("При запуске приложения произошла ошибка."), exception);
                throw;
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("При запуске приложения произошла ошибка."), exception);
                throw;
            }

            return true;
        }

        public bool Stop()
        {
            Logger.Info(m => m("Остановка приложения."));

            try
            {
                this.configuration.ComponentManager.UnloadComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("При остановке приложения произошла ошибка."), exception);
                throw;
            }

            return true;
        }

        public bool Resume()
        {
            Logger.Info(m => m("Возобновление работы приложения."));

            try
            {
                this.configuration.ComponentManager.ResumeComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("При возобнолении работы приложения произошла ошибка."), exception);
                throw;
            }

            return true;
        }

        public bool Pause()
        {
            Logger.Info(m => m("Приостановка работы приложения."));

            try
            {
                this.configuration.ComponentManager.PauseComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("При попытке приостановить приложение произошла ошибка."), exception);
                throw;
            }

            return true;
        }

        public bool Shutdown()
        {
            Logger.Info(m => m("Завершение приложения."));

            try
            {
                this.configuration.ComponentManager.UnloadComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("При завершении приложение произошла ошибка."), exception);
                throw;
            }

            return true;
        }
    }
}