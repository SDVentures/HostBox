using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace HostBox.Configuration
{
    public class ComponentConfiguration : Borderline.IConfiguration
    {
        private readonly IConfiguration inner;

        private ComponentConfiguration(IConfiguration inner)
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
            return this.inner.GetSection(path).Get<T>();
        }

        /// <inheritdoc />
        public IEnumerable<Borderline.IConfiguration> GetChildren()
        {
            return this.inner
                .GetChildren()
                .Select(s => new ComponentConfigurationSection(s));
        }

        /// <inheritdoc />
        public Borderline.IConfiguration GetSection(string path)
        {
            return new ComponentConfigurationSection(this.inner.GetSection(path));
        }

        /// <inheritdoc />
        public void OnReload(Action<Borderline.IConfiguration> callback)
            => inner.OnChange(this, callback);

        public static Borderline.IConfiguration Create(IConfiguration source)
        {
            return new ComponentConfiguration(source);
        }
    }
}