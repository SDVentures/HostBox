using System.Collections.Generic;

using HostBox.Borderline;

namespace HostBox.Loading
{
    public class HostedComponentsManager
    {
        private readonly List<IHostableComponent> components = new List<IHostableComponent>();

        public HostedComponentsManager(IEnumerable<IHostableComponent> components)
        {
            this.AddComponents(components);
        }

        public void AddComponents(IEnumerable<IHostableComponent> components)
        {
            if (components != null)
            {
                this.components.AddRange(components);
            }
        }

        public IEnumerable<IHostableComponent> GetComponents() =>
            this.components.ToArray();
    }
}