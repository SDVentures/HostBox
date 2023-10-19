using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HostBox.Web
{
    public class StartupHealthCheckOnly
    {
        public void ConfigureServices(IServiceCollection services)
        {
            HostboxWebExtensions.AddHealthChecks(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            HostboxWebExtensions.UseHealthChecks(app);
        }
    }
}
