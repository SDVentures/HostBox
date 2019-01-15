using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common.Logging;

using HostBox.Borderline;
using HostBox.Configuration;
using HostBox.Loading;

using McMaster.NETCore.Plugins;

using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace HostBox
{
    public class Application : IHostedService
    {
        private static readonly TaskCompletionSource<object> DelayStart = new TaskCompletionSource<object>();

        private static readonly TaskCompletionSource<object> DelayStop = new TaskCompletionSource<object>();

        private readonly ILog logger;

        private readonly IConfiguration appConfiguration;

        private StartResult description;

        public Application(IConfiguration appConfiguration, ComponentConfig config, IApplicationLifetime lifetime)
        {
            if (lifetime == null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }

            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            this.ComponentConfig = config;

            this.logger = config.LoggerFactory.Invoke(this.GetType());

            lifetime.ApplicationStarted.Register(this.OnStarted);
            lifetime.ApplicationStopping.Register(this.OnStopping);
            lifetime.ApplicationStopped.Register(this.OnStopped);
        }

        public ComponentConfig ComponentConfig { get; }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(this.ComponentConfig.Path));

            cancellationToken.Register(() => DelayStart.TrySetCanceled());

            this.description = this.LoadAndRunComponents(cancellationToken);

            this.description.StartTask
                .ContinueWith(
                    t => DelayStart.TrySetResult(null),
                    cancellationToken,
                    TaskContinuationOptions.NotOnFaulted,
                    TaskScheduler.Current)
                .ContinueWith(
                    t => DelayStart.TrySetException(t.Exception),
                    cancellationToken,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Current);

            return DelayStart.Task;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => DelayStop.TrySetCanceled());

            var componentStopTask = Task.Factory
                .StartNew(
                    () =>
                        {
                            foreach (var component in this.description.Components)
                            {
                                component.Stop();
                            }
                        },
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

            componentStopTask.ContinueWith(t => DelayStop.TrySetResult(null), cancellationToken);

            return DelayStop.Task;
        }

        private void OnStarted()
        {
            this.logger.Trace("Application started.");
        }

        private void OnStopping()
        {
            this.logger.Trace("Application stopping.");
        }

        private void OnStopped()
        {
            this.logger.Trace("Application stopped.");
        }

        private StartResult LoadAndRunComponents(CancellationToken cancellationToken)
        {
            var loader = PluginLoader.CreateFromAssemblyFile(
                this.ComponentConfig.Path,
                new[]
                    {
                        typeof(Borderline.IConfiguration),
                        typeof(DependencyContext)
                    });

            var entryAssemblyName = loader.LoadDefaultAssembly().GetName(false);

            var dc = DependencyContext.Load(loader.LoadDefaultAssembly());

            var factories = new List<IHostableComponentFactory>();

            var componentAssemblies = dc.GetRuntimeAssemblyNames(RuntimeEnvironment.GetRuntimeIdentifier())
                .Where(n => n != entryAssemblyName)
                .Select(loader.LoadAssembly)
                .ToArray();

            foreach (var assembly in componentAssemblies)
            {
                var componentFactoryTypes = assembly
                    .GetExportedTypes()
                    .Where(t => t.GetInterfaces().Any(i => i == typeof(IHostableComponentFactory)))
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

            var cfg = ComponentConfiguration.Create(this.appConfiguration);

            var componentLoader = new ComponentAssemblyLoader(loader);

            var components = factories
                .Select(f => f.CreateComponent(componentLoader, cfg))
                .ToArray();

            var startTask = Task.Factory.StartNew(
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

            return new StartResult
            {
                Components = components,
                StartTask = startTask
            };
        }

        private class StartResult
        {
            public IHostableComponent[] Components { get; set; }

            public Task StartTask { get; set; }
        }
    }
}