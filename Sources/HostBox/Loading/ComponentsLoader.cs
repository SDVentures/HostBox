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
    public class ComponentsLoader
    {
        private static readonly ILog Logger = LogManager.GetLogger<ComponentsLoader>();

        private readonly PluginLoader loader;

        public ComponentsLoader(ComponentConfig config, IConfiguration configuration)
        {
            var behaviorConfig = configuration
                            .GetSection("shared-libraries:gems.app:shared-libraries-loading")
                            .Get<SharedLibraryLoadingConfig>() ?? new SharedLibraryLoadingConfig();

            var pluginLoadConfig = new PluginLoadConfig
            {
                Assembly = config.Path,
                SharedTypes = new[]
                {
                    typeof(Borderline.IConfiguration),
                    typeof(DependencyContext)
                },
                SharedPath = config.SharedLibraryPath,
                DefaultSharedLibBehavior = ConvertBehavior(behaviorConfig.DefaultBehavior),
                SharedLibBehavior = behaviorConfig.Overrides == null
                        ? new Dictionary<string, SharedLibLoadBehavior>()
                        : behaviorConfig.Overrides.ToDictionary(kvp => kvp.Key, kvp => ConvertBehavior(kvp.Value))
            };

            this.loader = PluginLoader.CreateFromConfig(pluginLoadConfig);
        }

        public LoadComponentsResult LoadComponents(IConfiguration configuration)
        {
            var entryAssembly = this.loader.LoadDefaultAssembly();
            var entryAssemblyName = entryAssembly.GetName(false);

            var dc = DependencyContext.Load(this.loader.LoadDefaultAssembly());

            var componentsAssemblies = dc.GetRuntimeAssemblyNames(RuntimeEnvironment.GetRuntimeIdentifier())
                .Where(n => n != entryAssemblyName)
                .Select(this.loader.LoadAssembly)
                .ToArray();

            this.SetSharedLibrariesConfiguration(configuration, componentsAssemblies);
            var cfg = ComponentConfiguration.Create(configuration);
            var componentLoader = new ComponentAssemblyLoader(this.loader);

            var components = new List<IHostableComponent>();

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

                foreach (var factory in componentFactoryTypes
                    .Select(Activator.CreateInstance)
                    .Cast<IHostableComponentFactory>()
                    .ToArray())
                {
                    components.Add(factory.CreateComponent(componentLoader, cfg));
                }
            }

            return new LoadComponentsResult
            {
                EntryAssembly = entryAssembly,
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
                                new[]
                                    {
                                        sharedLibConfiguration
                                    });

                            Logger.Info(m => m($"Set configuration for shared library {libraryName}"));
                        }
                    }
                }
            }
        }

        private static SharedLibLoadBehavior ConvertBehavior(string behavior)
        {
            switch (behavior)
            {
                case "prefer_shared":
                    return SharedLibLoadBehavior.PreferShared;
                case "prefer_local":
                    return SharedLibLoadBehavior.PreferLocal;
                case "highest_version":
                    return SharedLibLoadBehavior.HighestVersion;
                default:
                    Logger.Warn(m => m($"Unknown shared library loading behavior: {behavior}. Using 'prefer_shared'"));
                    return SharedLibLoadBehavior.PreferShared;
            }
        }

        public class LoadComponentsResult
        {
            public Assembly EntryAssembly { get; set; }

            public IEnumerable<IHostableComponent> Components { get; set; }
        }
    }
}
