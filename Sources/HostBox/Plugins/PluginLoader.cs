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
    /// </summary>
    internal class PluginLoader
    {
        /// <summary>
        /// Create a plugin loader using the settings from a plugin config file.
        /// <seealso cref="PluginConfig" /> for defaults on the plugin configuration.
        /// </summary>
        /// <param name="filePath">The file path to the plugin config.</param>
        /// <param name="sharedTypes">A list of types which should be shared between the host and the plugin.</param>
        /// <returns>A loader.</returns>
        public static PluginLoader CreateFromConfigFile(string filePath, Type[] sharedTypes = null)
        {
            var config = PluginConfig.CreateFromFile(filePath);
            var baseDir = Path.GetDirectoryName(filePath);
            return new PluginLoader(config, baseDir, sharedTypes, PluginLoaderOptions.None, null);
        }

        /// <summary>
        /// Create a plugin loader for an assembly file.
        /// </summary>
        /// <param name="assemblyFile">The file path to the plugin config.</param>
        /// <param name="sharedTypes">A list of types which should be shared between the host and the plugin.</param>
        /// <param name="probingPath">additional probing path</param>
        /// <returns>A loader.</returns>
        public static PluginLoader CreateFromAssemblyFile(string assemblyFile, Type[] sharedTypes = null, string sharedPath = null)
        {
            Console.WriteLine($"PluginLoader: CreateFromAssemblyFile {assemblyFile}. sharedPath: {sharedPath}. sharedTypes: {(sharedTypes == null ? -1 : sharedTypes.Length)}");
            var config = new FileOnlyPluginConfig(assemblyFile);
            var baseDir = Path.GetDirectoryName(assemblyFile);
            return new PluginLoader(config, baseDir, sharedTypes, PluginLoaderOptions.None, sharedPath);
        }

        /// <summary>
        /// Create a plugin loader for an assembly file.
        /// </summary>
        /// <param name="assemblyFile">The file path to the plugin config.</param>
        /// <param name="loaderOptions">Options for the loader</param>
        /// <returns>A loader.</returns>
        public static PluginLoader CreateFromAssemblyFile(string assemblyFile, PluginLoaderOptions loaderOptions)
        {
            var config = new FileOnlyPluginConfig(assemblyFile);
            var baseDir = Path.GetDirectoryName(assemblyFile);
            return new PluginLoader(config, baseDir, Array.Empty<Type>(), loaderOptions, null);
        }

        private class FileOnlyPluginConfig : PluginConfig
        {
            public FileOnlyPluginConfig(string filePath)
                : base(new AssemblyName(Path.GetFileNameWithoutExtension(filePath)), Array.Empty<AssemblyName>())
            { }
        }

        private readonly string _mainAssembly;
        private AssemblyLoadContext _context;

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
        {
            Console.WriteLine($"Load Assembly {assemblyName} {assemblyName.FullName}");
            return _context.LoadFromAssemblyName(assemblyName);
        }

        /// <summary>
        /// Load an assembly by name.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>The assembly.</returns>
        public Assembly LoadAssembly(string assemblyName)
            => LoadAssembly(new AssemblyName(assemblyName));

        internal PluginLoader(PluginConfig config,
                              string baseDir,
                              Type[] sharedTypes,
                              PluginLoaderOptions loaderOptions,
                              string sharedPath)
        {
            _mainAssembly = Path.Combine(baseDir, config.MainAssembly.Name + ".dll"); 
            Console.WriteLine($"PluginLoader: ctor _mainAssembly={_mainAssembly}");
            _context = CreateLoadContext(baseDir, config, sharedTypes, loaderOptions, sharedPath);
        }

        private static AssemblyLoadContext CreateLoadContext(
            string baseDir,
            PluginConfig config,
            Type[] sharedTypes,
            PluginLoaderOptions loaderOptions,
            string sharedPath)
        {
            var depsJsonFile = Path.Combine(baseDir, config.MainAssembly.Name + ".deps.json");

            var builder = new AssemblyLoadContextBuilder();

            if (sharedPath != null)
            {
                builder.AddSharedPath(sharedPath);
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

            if (sharedTypes != null)
            {
                foreach (var type in sharedTypes)
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

            return builder.Build();
        }
    }
}
