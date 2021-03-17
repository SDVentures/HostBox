using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Common.Logging;

using HostBox.Borderline;
using HostBox.Configuration;

using McMaster.NETCore.Plugins;

using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace HostBox.Loading
{
    internal class ComponentsLoader
    {
        private static readonly ILog Logger = LogManager.GetLogger<ComponentsLoader>();

        private readonly PluginLoader loader;

        public ComponentsLoader(ComponentConfig config)
        {
            this.loader = PluginLoader.CreateFromAssemblyFile(
                config.Path,
                new[]
                    {
                        typeof(Borderline.IConfiguration),
                        typeof(DependencyContext)
                    },
                config.SharedLibraryPath);
        }

        public LoadAndRunComponentsResult LoadAndRunComponents(IConfiguration configuration, CancellationToken cancellationToken)
        {
            var loadComponentsResult = this.LoadComponents();
            this.RunComponents(loadComponentsResult.ComponentsFactories, loadComponentsResult.ComponentAssemblies, configuration, cancellationToken);
            
            return new LoadAndRunComponentsResult
            {
                EntryAssembly = loadComponentsResult.EntryAssembly
            };
        }

        private LoadComponentsResult LoadComponents()
        {
            var entryAssembly = this.loader.LoadDefaultAssembly();
            var entryAssemblyName = entryAssembly.GetName(false);

            var dc = DependencyContext.Load(this.loader.LoadDefaultAssembly());

            var componentsAssemblies = dc.GetRuntimeAssemblyNames(RuntimeEnvironment.GetRuntimeIdentifier())
                .Where(n => n != entryAssemblyName)
                .Select(this.loader.LoadAssembly)
                .ToArray();
            
            var factories = new List<IHostableComponentFactory>();

            foreach (var assembly in componentsAssemblies)
            {
                var componentFactoryTypes = assembly
                    .GetExportedTypes()
                    .Where(t => t.GetInterfaces().Any(i => typeof(IHostableComponentFactory).IsAssignableFrom(i)))
                    .ToArray();

                if (!componentFactoryTypes.Any())
                {
                    continue;
                }

                var instances = componentFactoryTypes
                    .Select(Activator.CreateInstance)
                    .Cast<IHostableComponentFactory>()
                    .ToArray();

                factories.AddRange(instances);
            }

            return new LoadComponentsResult
            {
                EntryAssembly = entryAssembly,
                ComponentAssemblies = componentsAssemblies,
                ComponentsFactories = factories
            };
        }

        private RunComponentsResult RunComponents(IEnumerable<IHostableComponentFactory> factories, IEnumerable<Assembly> componentsAssemblies, IConfiguration configuration, CancellationToken cancellationToken)
        {
            var cfg = ComponentConfiguration.Create(configuration);

            this.SetSharedLibrariesConfiguration(configuration, componentsAssemblies);

            var componentLoader = new ComponentAssemblyLoader(this.loader);

            var components = factories
                .Select(f => f.CreateComponent(componentLoader, cfg))
                .ToArray();

            var task = Task.Factory.StartNew(
                () =>
                    {
                        foreach (var component in components)
                        {
                            component.Start();
                        }
                    },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            return new RunComponentsResult
                {
                    StartTask = task,
                    Components = components
                };
        }

        private void SetSharedLibrariesConfiguration(IConfiguration configuration, IEnumerable<Assembly> componentsAssemblies)
        {
            foreach (var componentAssembly in componentsAssemblies)
            {
                var configurationFactory = componentAssembly.GetExportedTypes()
                    .FirstOrDefault(x => x.Name == "ConfigurationProvider");

                if (configurationFactory != null)
                {
                    var method = configurationFactory.GetMethod("Set");

                    if (method != null && method.IsStatic)
                    {
                        var parameters = method.GetParameters();

                        if (parameters.Length == 1)
                        {
                            var libraryName = componentAssembly.GetName().Name.ToLower();
                            var configType = parameters[0].ParameterType;

                            var sharedLibConfiguration =
                                configuration
                                    .GetSection($"shared-libraries:{libraryName}")?
                                    .Get(configType);

                            method.Invoke(
                                null,
                                new []
                                    {
                                        sharedLibConfiguration
                                    });

                            Logger.Info(m => m($"Set configuration for shared library {libraryName}"));
                        }
                    }
                }
            }
        }

        private class LoadComponentsResult
        {
            public Assembly EntryAssembly { get; set; }
            
            public IEnumerable<Assembly> ComponentAssemblies { get; set; }
            
            public IEnumerable<IHostableComponentFactory> ComponentsFactories { get; set; }
        }

        private class RunComponentsResult
        {
            public IHostableComponent[] Components { get; set; }

            public Task StartTask { get; set; }
        }
        
        internal class LoadAndRunComponentsResult
        {
            public Assembly EntryAssembly { get; set; }
        }
    }
}