using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace HostBox.Configuration
{
    public class ComponentConfigurationSection : Borderline.IConfiguration
    {
        private readonly IConfigurationSection innerSection;

        public ComponentConfigurationSection(IConfigurationSection innerSection)
        {
            this.innerSection = innerSection;
        }

        /// <inheritdoc />
        public string Key => this.innerSection.Key;

        /// <inheritdoc />
        public string Path => this.innerSection.Path;

        /// <inheritdoc />
        public string Value => this.innerSection.Value;

        /// <inheritdoc />
        public string this[string key] => this.innerSection[key];

        /// <inheritdoc />
        public T BindSection<T>(string path) where T : new()
        {
            return this.innerSection.GetSection(path).Get<T>();
        }

        /// <inheritdoc />
        public IEnumerable<Borderline.IConfiguration> GetChildren()
        {
            return this.innerSection
                .GetChildren()
                .Select(s => new ComponentConfigurationSection(s));
        }

        /// <inheritdoc />
        public void OnReload(Action<Borderline.IConfiguration> callback)
        {
            var token = this.innerSection.GetReloadToken();

            token.RegisterChangeCallback(o => callback.Invoke(this), null);
        }
    }
}