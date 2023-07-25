using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using HostBox.Borderline;

namespace HostBox.Loading
{
    public class ComponentsRunner
    {
        private readonly List<IHostableComponent> components = new List<IHostableComponent>();

        public ComponentsRunner(IEnumerable<IHostableComponent> components)
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

        public Task RunComponents(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        foreach (var component in components)
                        {
                            component.Start(); // TODO: should pass the cancellationToken
                        }
                    },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}