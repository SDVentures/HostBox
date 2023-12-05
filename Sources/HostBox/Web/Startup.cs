using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HostBox.Web
{
    /// <summary>
    /// A wrapper around IStartup, that calls IStartup from entry assembly 
    /// </summary>
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Calling 'BuildServiceProvider' from application code results in more than one copy of
            // singleton services being created which might result in incorrect application behavior.
            // Consider alternatives such as dependency injecting services as parameters to 'Configure'.
            var sp = services.BuildServiceProvider();

            var startup = sp.GetService<IStartup>();

            startup.ConfigureServices(services);
            
            HostboxWebExtensions.AddHealthChecks(services);
        }

        public void Configure(IApplicationBuilder app, IStartup startup)
        {
            HostboxWebExtensions.UseHealthChecks(app);
            startup.Configure(app);
        }
    }
}
