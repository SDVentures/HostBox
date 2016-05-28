using System.Collections.Generic;
using System.Linq;

namespace HostBox
{
    internal class ApplicationConfigurationFactory : IApplicationConfigurationFactory
    {
        private readonly IComponentManager componentManager;

        public ApplicationConfigurationFactory(IComponentManager componentManager)
        {
            this.componentManager = componentManager;
        }

        public IApplicationConfiguration CreateApplicationConfiguration(IEnumerable<string> componentPaths)
        {
            return new ApplicationConfiguration
                        {
                            ComponentManager = this.componentManager,
                            ComponentPaths = componentPaths.ToList(),
                            ComponentConfigurations = new List<IComponentConfiguration>()
                        };
        }
    }
}