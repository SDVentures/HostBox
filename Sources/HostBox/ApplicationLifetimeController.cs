using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HostBox.Loading;

using Microsoft.Extensions.Hosting;

namespace HostBox
{
    public class ApplicationLifetimeController : IHostedService
    {
#if NET5_0_OR_GREATER
        private readonly IHostApplicationLifetime lifetime;
#else
        private readonly IApplicationLifetime lifetime;

#endif
        private readonly HostedComponentsManager hostedComponentsManager;

#if NET5_0_OR_GREATER
        public ApplicationLifetimeController(IHostApplicationLifetime lifetime, HostedComponentsManager hostedComponentsManager)
#else
        public ApplicationLifetimeController(IApplicationLifetime lifetime, HostedComponentsManager hostedComponentsManager)
#endif
        {
            this.lifetime = lifetime;
            this.hostedComponentsManager = hostedComponentsManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.lifetime.ApplicationStopping.Register(
                () =>
                    {
                        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                        var stopTasks = this.hostedComponentsManager.GetComponents()
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