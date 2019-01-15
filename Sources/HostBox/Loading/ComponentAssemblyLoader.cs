using System.Reflection;

using HostBox.Borderline;

using McMaster.NETCore.Plugins;

namespace HostBox.Loading
{
    public class ComponentAssemblyLoader : IAssemblyLoader
    {
        private readonly PluginLoader inner;

        /// <inheritdoc />
        public ComponentAssemblyLoader(PluginLoader inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc />
        public Assembly Load(AssemblyName name)
        {
            return this.inner.LoadAssembly(name);
        }
    }
}