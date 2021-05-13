using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace HostBox.Configuration
{
    public class JsonTemplateConfigurationSource : JsonConfigurationSource
    {
        public IEnumerable<IConfigurationProvider> ValuesProviders { get; set; }

        public string PlaceholderPattern { get; set; }

        /// <inheritdoc />
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            this.EnsureDefaults(builder);

            return new JsonTemplateConfigurationProvider(
                this, 
                this.ValuesProviders,
                this.PlaceholderPattern);
        }
    }
}