using System;

using HostShim;

namespace HostBox
{
    /// <summary>
    /// Component factory
    /// </summary>
    public interface IComponentFactory
    {
        /// <summary>
        /// Create component
        /// </summary>
        /// <param name="componentPath"> Path to executable code of the component </param>
        /// <param name="targetDomain"> Target domain in which component will be loaded </param>
        /// <param name="hostDomain"> Main application domain </param>
        /// <returns> Loaded component </returns>
        IComponent CreateComponent(string componentPath, AppDomain targetDomain, AppDomain hostDomain);
    }
}

