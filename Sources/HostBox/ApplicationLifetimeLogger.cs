using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Extensions.Hosting;

namespace HostBox
{
    public class ApplicationLifetimeLogger : IHostedService
    {
        private static readonly ILog Logger = LogManager.GetLogger<ApplicationLifetimeLogger>();
       
#if !NETCOREAPP2_1
        public ApplicationLifetimeLogger(IHostApplicationLifetime lifetime)
        {
            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopping.Register(OnStopping);
            lifetime.ApplicationStopped.Register(OnStopped);
        }
#endif
#if NETCOREAPP2_1
        public ApplicationLifetimeLogger(IApplicationLifetime lifetime)
        {
            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopping.Register(OnStopping);
            lifetime.ApplicationStopped.Register(OnStopped);
        }
#endif

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static void OnStarted()
        {
            Logger.Trace("Application started.");
        }

        private static void OnStopping()
        {
            Logger.Trace("Application stopping.");
        }

        private static void OnStopped()
        {
            Logger.Trace("Application stopped.");
        }
    }
}