using System.Collections.Generic;

using HostBox.Borderline;

namespace HostBox.Loading
{
    public class HostedComponentsManager
    {
        private List<IHostableComponent> components = new List<IHostableComponent>();

        public void AddComponents(IEnumerable<IHostableComponent> components)
        {
            this.components.AddRange(components);
        }

        public IEnumerable<IHostableComponent> GetComponents() =>
            this.components.ToArray();
    }
}