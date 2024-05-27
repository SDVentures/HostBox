// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using McMaster.NETCore.Plugins.Loader;

namespace McMaster.NETCore.Plugins
{
    /// <summary>
    /// This loader attempts to load binaries for execution (both managed assemblies and native libraries)
    /// in the same way that .NET Core would if they were originally part of the .NET Core application.
    /// <para>
    /// This loader reads configuration files produced by .NET Core (.deps.json and runtimeconfig.json)
    /// as well as a custom file (*.config files). These files describe a list of .dlls and a set of dependencies.
    /// The loader searches the plugin path, as well as any additionally specified paths, for binaries
    /// which satisfy the plugin's requirements.
    /// </para>
    /// Original code: https://github.com/natemcmaster/DotNetCorePlugins
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original style")]
    internal class PluginLoader
    {
        public static PluginLoader CreateFromConfig(PluginLoadConfig loadConfig)
        {
            var config = new FileOnlyPluginConfig(loadConfig.Assembly);
            var baseDir = Path.GetDirectoryName(loadConfig.Assembly);
            return new PluginLoader(config, baseDir, PluginLoaderOptions.None, loadConfig);
        }

        private class FileOnlyPluginConfig : PluginConfig
        {
            public FileOnlyPluginConfig(string filePath)
                : base(new AssemblyName(Path.GetFileNameWithoutExtension(filePath)), Array.Empty<AssemblyName>())
            { }
        }

        private readonly string _mainAssembly;
        private readonly AssemblyLoadContext _context;

        /// <summary>
        /// Load the main assembly for the plugin.
        /// </summary>
        public Assembly LoadDefaultAssembly()
        => _context.LoadFromAssemblyPath(_mainAssembly);

        /// <summary>
        /// Load an assembly by name.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>The assembly.</returns>
        public Assembly LoadAssembly(AssemblyName assemblyName)
            => _context.LoadFromAssemblyName(assemblyName);

        /// <summary>
        /// Load an assembly by name.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>The assembly.</returns>
        public Assembly LoadAssembly(string assemblyName)
            => LoadAssembly(new AssemblyName(assemblyName));

        internal PluginLoader(PluginConfig config,
                              string baseDir,
                              PluginLoaderOptions loaderOptions,
                              PluginLoadConfig loadConfig)
        {
            _mainAssembly = Path.Combine(baseDir, config.MainAssembly.Name + ".dll");
            _context = CreateLoadContext(baseDir, config, loaderOptions, loadConfig);
        }

        private static AssemblyLoadContext CreateLoadContext(
            string baseDir,
            PluginConfig config,
            PluginLoaderOptions loaderOptions,
            PluginLoadConfig loadConfig)
        {
            var depsJsonFile = Path.Combine(baseDir, config.MainAssembly.Name + ".deps.json");

            var builder = new AssemblyLoadContextBuilder();

            if (loadConfig.SharedPath != null)
            {
                builder.AddSharedPath(loadConfig.SharedPath);
            }

            if (File.Exists(depsJsonFile))
            {
                builder.AddDependencyContext(depsJsonFile);
            }

            builder.SetBaseDirectory(baseDir);

            foreach (var ext in config.PrivateAssemblies)
            {
                builder.PreferLoadContextAssembly(ext);
            }

            if (loaderOptions.HasFlag(PluginLoaderOptions.PreferSharedTypes))
            {
                builder.PreferDefaultLoadContext(true);
            }

            if (loadConfig.SharedTypes != null)
            {
                foreach (var type in loadConfig.SharedTypes)
                {
                    builder.PreferDefaultLoadContextAssembly(type.Assembly.GetName());
                }
            }

            var pluginRuntimeConfigFile = Path.Combine(baseDir, config.MainAssembly.Name + ".runtimeconfig.json");

            builder.TryAddAdditionalProbingPathFromRuntimeConfig(pluginRuntimeConfigFile, includeDevConfig: true, out _);

            // Always include runtimeconfig.json from the host app.
            // in some cases, like `dotnet test`, the entry assembly does not actually match with the
            // runtime config file which is why we search for all files matching this extensions.
            foreach (var runtimeconfig in Directory.GetFiles(AppContext.BaseDirectory, "*.runtimeconfig.json"))
            {
                builder.TryAddAdditionalProbingPathFromRuntimeConfig(runtimeconfig, includeDevConfig: true, out _);
            }

            return builder.Build(
                loadConfig.DefaultSharedLibBehavior,
                loadConfig.SharedLibBehavior);
        }
    }
}
