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
            Logger.Info(m => m("Starting application..."));
            Logger.Debug(m => m($"Specified directories for loadings components: {string.Join(";", this.componentPaths)}"));

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
                        Logger.Fatal(m => m("Type loading error"), currentException);
                    }
                }

                Logger.Fatal(m => m("Error during starting the application"), exception);
                throw;
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("Error during starting the application"), exception);
                throw;
            }

            return true;
        }

        public bool Stop()
        {
            Logger.Info(m => m("Stopping application..."));

            try
            {
                this.configuration.ComponentManager.UnloadComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("Error during stopping the application"), exception);
                throw;
            }

            return true;
        }

        public bool Resume()
        {
            Logger.Info(m => m("Resuming application..."));

            try
            {
                this.configuration.ComponentManager.ResumeComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("Error during resuming the application"), exception);
                throw;
            }

            return true;
        }

        public bool Pause()
        {
            Logger.Info(m => m("Pausing application..."));

            try
            {
                this.configuration.ComponentManager.PauseComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("Error during pausing the application"), exception);
                throw;
            }

            return true;
        }

        public bool Shutdown()
        {
            Logger.Info(m => m("Shutting down application..."));

            try
            {
                this.configuration.ComponentManager.UnloadComponents(this.configuration);
            }
            catch (Exception exception)
            {
                Logger.Fatal(m => m("Error during shutting down the application"), exception);
                throw;
            }

            return true;
        }
    }
}