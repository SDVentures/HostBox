using System;

namespace HostBox.Configuration
{
    public class HostComponentsConfiguration
    {
        public TimeSpan StoppingTimeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}