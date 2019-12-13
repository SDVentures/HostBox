using System.Collections.Generic;

using Microsoft.Extensions.Configuration.Json;

namespace HostBox.Configuration
{
    public class SharedLibraryConfigurationProvider : JsonConfigurationProvider
    {
        private readonly string libraryName;

        public SharedLibraryConfigurationProvider(SharedLibraryConfigurationSource source, string libraryName)
            : base(source)
        {
            this.libraryName = libraryName.ToLower();
        }

        public override void Load()
        {
            base.Load();

            var result = new Dictionary<string, string>();

            foreach (var dataKey in this.Data.Keys)
            {
                result[$"shared-libraries:{this.libraryName}:{dataKey}"] = this.Data[dataKey];
            }

            this.Data = result;
        }
    }
}