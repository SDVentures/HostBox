using System;
using System.Reflection;

using HostShim;

namespace HostBox
{
    internal class DomainComponentFactory : IComponentFactory
    {
        private readonly bool asyncStart;

        public DomainComponentFactory(bool asyncStart)
        {
            this.asyncStart = asyncStart;
        }

        public IComponent CreateComponent(string componentPath, AppDomain targetDomain, AppDomain hostDomain)
        {
            return this.CreateMefDomainComponent(componentPath, targetDomain);
        }

        private IComponent CreateMefDomainComponent(string componentPath, AppDomain targetDomain)
        {
            return (IComponent)targetDomain.CreateInstanceFromAndUnwrap(
                typeof(MefDomainComponent).Assembly.Location,
                typeof(MefDomainComponent).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                new object[] { componentPath, this.asyncStart },
                null,
                null);
        }
    }
}