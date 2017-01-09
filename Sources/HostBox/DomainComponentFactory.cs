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
            return CreateMefDomainComponent(componentPath, this.asyncStart, targetDomain);
        }

        private static IComponent CreateMefDomainComponent(string componentPath, bool asyncStart, AppDomain targetDomain)
        {
            return (IComponent)targetDomain.CreateInstanceFromAndUnwrap(
                typeof(MefDomainComponent).Assembly.Location,
                typeof(MefDomainComponent).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                new object[] { componentPath, asyncStart },
                null,
                null);
        }
    }
}