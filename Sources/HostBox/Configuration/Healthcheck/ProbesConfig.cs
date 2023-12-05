using System.Collections.Generic;

namespace HostBox.Configuration.Healthcheck
{
    internal class ProbesConfig
    {
        public string HealthRoute { get; set; }

        public int HealthPort { get; set; }

        public Dictionary<string, KnownHealthCheckConfig> KnownChecks { get; set; }
    }
}
