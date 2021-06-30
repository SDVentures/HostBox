#if !NETCOREAPP2_1

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HostBox
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
        }

        public void Configure(IApplicationBuilder app, IStartup startup)
        {
            startup.Configure(app);
        }
    }
}
#endif