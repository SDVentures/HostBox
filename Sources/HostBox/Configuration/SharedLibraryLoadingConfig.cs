using System.Collections.Generic;

namespace HostBox.Configuration
{
    public class SharedLibraryLoadingConfig
    {
        public string DefaultBehavior { get; set; } = "prefer_shared";

        public Dictionary<string, string> Overrides { get; set; }
    }
}
