using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HostBox.Configuration;
using HostBox.Loading;

using Microsoft.Extensions.Hosting;

namespace HostBox
{
    public class HostableComponentsFinalizer : IHostedService
    {
        private readonly ComponentsRunner componentsRunner;

        private readonly HostComponentsConfiguration configuration;

#if NETCOREAPP3_1_OR_GREATER
        private readonly IHostApplicationLifetime lifetime;
#else
        private readonly IApplicationLifetime lifetime;

#endif

#if NETCOREAPP3_1_OR_GREATER
        public HostableComponentsFinalizer(IHostApplicationLifetime lifetime, ComponentsRunner componentsRunner, HostComponentsConfiguration configuration)
#else
        public HostableComponentsFinalizer(IApplicationLifetime lifetime, HostedComponentsManager hostedComponentsManager, HostComponentsConfiguration configuration)
#endif
        {
            this.lifetime = lifetime;
            this.componentsRunner = componentsRunner;
            this.configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.lifetime.ApplicationStopping.Register(
                () =>
                    {
                        var cts = new CancellationTokenSource(this.configuration.StoppingTimeout);
                        var stopTasks = this.componentsRunner.GetComponents()
                            .Select(x => Task.Run(() => x.Stop(), cts.Token));
                        Task.WaitAll(stopTasks.ToArray());
                    });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
