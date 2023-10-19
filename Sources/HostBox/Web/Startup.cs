using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HostBox.Web
{
    /// <summary>
    /// A wrapper around IStartup, that calls IStartup from entry assembly 
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            var startup = sp.GetService<IStartup>();

            startup?.ConfigureServices(services);
            HostboxWebExtensions.AddHealthChecks(services);
        }

        public void Configure(IApplicationBuilder app, IStartup startup)
        {
            HostboxWebExtensions.UseHealthChecks(app);
            startup.Configure(app);
        }
    }
}
