using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace HostBox.Configuration
{
    public class ComponentConfiguration : Borderline.IConfiguration
    {
        private readonly IConfiguration inner;

        public ComponentConfiguration(IConfiguration inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc />
        public string Key => string.Empty;

        /// <inheritdoc />
        public string Path => string.Empty;

        /// <inheritdoc />
        public string Value => string.Empty;

        /// <inheritdoc />
        public string this[string key] => this.inner[key];

        /// <inheritdoc />
        public T BindSection<T>(string path) where T : new()
        {
            var section = this.inner.GetSection(path);

            return section.Exists()
                       ? section.Get<T>()
                       : default;
        }

        /// <inheritdoc />
        public IEnumerable<Borderline.IConfiguration> GetChildren()
        {
            return this.inner
                .GetChildren()
                .Select(s => new ComponentConfigurationSection(s));
        }

        /// <inheritdoc />
        public void OnReload(Action<Borderline.IConfiguration> callback)
        {
            this.inner.GetReloadToken().RegisterChangeCallback(o => callback.Invoke(this), null);
        }

        public static Borderline.IConfiguration Create(IConfiguration source)
        {
            return new ComponentConfiguration(source);
        }
    }
}