using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace HostBox.Configuration
{
    public class SharedLibraryConfigurationSource : JsonConfigurationSource
    {
        public string LibraryName { get; set; }

        /// <inheritdoc />
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            this.EnsureDefaults(builder);

            return new SharedLibraryConfigurationProvider(this, this.LibraryName);
        }
    }
}