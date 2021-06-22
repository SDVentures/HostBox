using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace HostBox.Configuration
{
    public static class JsonTemplateConfigurationExtensions
    {
        /// <summary>
        /// Adds a JSON configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <param name="valuesProviders"></param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddJsonTemplateFile(this IConfigurationBuilder builder,
            string path, bool optional, bool reloadOnChange,
            IEnumerable<IConfigurationProvider> valuesProviders,
            string placeholderPattern = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Invalid file path.", nameof(path));
            }

            return builder.AddJsonTemplateFile(s =>
                {
                    s.FileProvider = null;
                    s.Path = path;
                    s.Optional = optional;
                    s.ReloadOnChange = reloadOnChange;
                    s.ValuesProviders = valuesProviders;
                    s.PlaceholderPattern = placeholderPattern ?? "!{*}";

                    s.ResolveFileProvider();
                });
        }

        /// <summary>
        /// Adds a JSON configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddJsonTemplateFile(this IConfigurationBuilder builder, Action<JsonTemplateConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}