using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace HostBox.Configuration
{
    public class JsonTemplateConfigurationSource : JsonConfigurationSource
    {
        public IConfigurationProvider ValuesProvider { get; set; }

        public string PlaceholderPattern { get; set; }

        /// <inheritdoc />
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            this.EnsureDefaults(builder);

            return new JsonTemplateConfigurationProvider(
                this, 
                this.ValuesProvider,
                this.PlaceholderPattern);
        }
    }
}