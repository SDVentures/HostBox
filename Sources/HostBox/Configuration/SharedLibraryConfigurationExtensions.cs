using System;
using System.IO;

using Common.Logging;

using Microsoft.Extensions.Configuration;

namespace HostBox.Configuration
{
    public static class SharedLibraryConfigurationExtensions
    {
        public static void LoadSharedLibraryConfigurationFiles(this IConfigurationBuilder builder, ILog logger, string componentBasePath, string sharedLibraryPath)
        {
            var sharedLibrariesPath = Path.Combine(componentBasePath, sharedLibraryPath);
            
            bool directoryExists = Directory.Exists(sharedLibrariesPath); //var e = new DirectoryInfo(sharedLibrariesPath).Exists;
            logger.Info(m => m($"Is shared library directory exists? -- {directoryExists}"));

            if (directoryExists)
            {
                var files = Directory.GetFiles(sharedLibrariesPath, "*.settings.json");

                foreach (var file in files)
                {
                    builder.AddSharedLibraryFile(file, false, false, Path.GetFileName(file).Replace(".settings.json", string.Empty));

                    logger.Info(m => m("Shared library configuration file [{0}] is loaded.", file));
                }
            }
        }

        /// <summary>
        /// Adds a JSON configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <param name="libraryName"></param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddSharedLibraryFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, string libraryName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Invalid file path.", nameof(path));
            }

            return builder.AddSharedLibraryFile(s =>
                {
                    s.FileProvider = null;
                    s.Path = path;
                    s.Optional = optional;
                    s.ReloadOnChange = reloadOnChange;
                    s.LibraryName = libraryName;

                    s.ResolveFileProvider();
                });
        }

        /// <summary>
        /// Adds a JSON configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddSharedLibraryFile(this IConfigurationBuilder builder, Action<SharedLibraryConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}