using System;
using System.Reflection;

using HostShim;

namespace HostBox
{
    internal class DomainComponentFactory : IComponentFactory
    {
        public IComponent CreateComponent(string componentPath, AppDomain targetDomain, AppDomain hostDomain)
        {
            return CreateMefDomainComponent(componentPath, targetDomain);
        }

        private static IComponent CreateMefDomainComponent(string componentPath, AppDomain targetDomain)
        {
            return (IComponent)targetDomain.CreateInstanceFromAndUnwrap(
                typeof(MefDomainComponent).Assembly.Location,
                typeof(MefDomainComponent).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                new object[] { componentPath },
                null,
                null);
        }
    }
}