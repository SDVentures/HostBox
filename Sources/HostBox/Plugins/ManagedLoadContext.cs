// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Common.Logging;

using McMaster.NETCore.Plugins.LibraryModel;

using Microsoft.Extensions.DependencyModel;

namespace McMaster.NETCore.Plugins.Loader
{
    /// <summary>
    /// An implementation of <see cref="AssemblyLoadContext" /> which attempts to load managed and native
    /// binaries at runtime immitating some of the behaviors of corehost.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original style")]
    internal class ManagedLoadContext : AssemblyLoadContext
    {
        private static readonly ILog Logger = LogManager.GetLogger<ManagedLoadContext>();

        private readonly string _basePath;
        private readonly string _sharedBasePath;
        private readonly SharedLibLoadBehavior _defaultLibBehavior;
        private readonly Dictionary<string, SharedLibLoadBehavior> _sharedLibBehavior;

        private readonly IReadOnlyDictionary<string, ManagedLibrary> _managedAssemblies;
        private readonly IReadOnlyDictionary<string, NativeLibrary> _nativeLibraries;
        private readonly IReadOnlyCollection<string> _privateAssemblies;
        private readonly IReadOnlyCollection<string> _defaultAssemblies;
        private readonly IReadOnlyCollection<string> _additionalProbingPaths;
        private readonly bool _preferDefaultLoadContext;
        private readonly string[] _resourceRoots;

        public ManagedLoadContext(string baseDirectory,
                                  string sharedPath,
            IReadOnlyDictionary<string, ManagedLibrary> managedAssemblies,
            IReadOnlyDictionary<string, NativeLibrary> nativeLibraries,
            IReadOnlyCollection<string> privateAssemblies,
            IReadOnlyCollection<string> defaultAssemblies,
            IReadOnlyCollection<string> additionalProbingPaths,
            IReadOnlyCollection<string> resourceProbingPaths,
            bool preferDefaultLoadContext,
            SharedLibLoadBehavior defaultLibBehavior,
            Dictionary<string, SharedLibLoadBehavior> sharedLibBehavior)
        {
            if (resourceProbingPaths == null)
            {
                throw new ArgumentNullException(nameof(resourceProbingPaths));
            }

            _basePath = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            _sharedBasePath = Path.Combine(baseDirectory, sharedPath);
            _managedAssemblies = managedAssemblies ?? throw new ArgumentNullException(nameof(managedAssemblies));
            _privateAssemblies = privateAssemblies ?? throw new ArgumentNullException(nameof(privateAssemblies));
            _defaultAssemblies = defaultAssemblies ?? throw new ArgumentNullException(nameof(defaultAssemblies));
            _nativeLibraries = nativeLibraries ?? throw new ArgumentNullException(nameof(nativeLibraries));
            _additionalProbingPaths = additionalProbingPaths ?? throw new ArgumentNullException(nameof(additionalProbingPaths));
            _preferDefaultLoadContext = preferDefaultLoadContext;
            _defaultLibBehavior = defaultLibBehavior;
            _sharedLibBehavior = sharedLibBehavior;

            _resourceRoots = new[] { _basePath }
                .Concat(resourceProbingPaths)
                .ToArray();

            Default.Resolving += ManagedLoadContext_Resolving;
        }

        private Assembly ManagedLoadContext_Resolving(AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            this.SearchForLibrary(ManagedLibrary.CreateFromPackage(assemblyName.Name, assemblyName.Version?.ToString(), assemblyName.Name + ".dll"), out var path);

            return this.LoadFromAssemblyPath(path);
        }

        /// <summary>
        /// Load an assembly.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        protected override Assembly Load(AssemblyName assemblyName)
        {
            if ((_preferDefaultLoadContext || _defaultAssemblies.Contains(assemblyName.Name)) && !_privateAssemblies.Contains(assemblyName.Name))
            {
                // If default context is preferred, check first for types in the default context unless the dependency has been declared as private
                try
                {
                    var defaultAssembly = Default.LoadFromAssemblyName(assemblyName);
                    if (defaultAssembly != null)
                    {
                        // return null so ALC will fallback to loading from Default ALC directly
                        return null;
                    }
                }
                catch
                {
                    // Swallow errors in loading from the default context
                }
            }

            // Resource assembly binding does not use the TPA. Instead, it probes PLATFORM_RESOURCE_ROOTS (a list of folders)
            // for $folder/$culture/$assemblyName.dll
            // See https://github.com/dotnet/coreclr/blob/3fca50a36e62a7433d7601d805d38de6baee7951/src/binder/assemblybinder.cpp#L1232-L1290

            if (!string.IsNullOrEmpty(assemblyName.CultureName) && !string.Equals("neutral", assemblyName.CultureName))
            {
                foreach (var resourceRoot in _resourceRoots)
                {
                    var resourcePath = Path.Combine(resourceRoot, assemblyName.CultureName, assemblyName.Name + ".dll");
                    if (File.Exists(resourcePath))
                    {
                        return LoadFromAssemblyPath(resourcePath);
                    }
                }

                return null;
            }

            if (_managedAssemblies.TryGetValue(assemblyName.Name, out var library))
            {
                if (SearchForLibrary(library, out var path))
                {
                    return LoadFromAssemblyPath(path);
                }
            }
            else
            {
                // if an assembly was not listed in the list of known assemblies,
                // fallback to the load context base directory
                var localFile = Path.Combine(_basePath, assemblyName.Name + ".dll");
                if (File.Exists(localFile))
                {
                    return LoadFromAssemblyPath(localFile);
                }
            }

            return null;
        }

        /// <summary>
        /// Loads the unmanaged binary using configured list of native libraries.
        /// </summary>
        /// <param name="unmanagedDllName"></param>
        /// <returns></returns>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            foreach (var prefix in PlatformInformation.NativeLibraryPrefixes)
            {
                if (_nativeLibraries.TryGetValue(prefix + unmanagedDllName, out var library))
                {
                    if (SearchForLibrary(library, prefix, out var path))
                    {
                        return LoadUnmanagedDllFromResolvedPath(path);
                    }
                }
                else
                {
                    // coreclr allows code to use [DllImport("sni")] or [DllImport("sni.dll")]
                    // This library treats the file name without the extension as the lookup name,
                    // so this loop is necessary to check if the unmanaged name matches a library
                    // when the file extension has been trimmed.
                    foreach (var suffix in PlatformInformation.NativeLibraryExtensions)
                    {
                        if (!unmanagedDllName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // check to see if there is a library entry for the library without the file extension
                        var trimmedName = unmanagedDllName.Substring(0, unmanagedDllName.Length - suffix.Length);

                        if (_nativeLibraries.TryGetValue(prefix + trimmedName, out library))
                        {
                            if (SearchForLibrary(library, prefix, out var path))
                            {
                                return LoadUnmanagedDllFromResolvedPath(path);
                            }
                        }
                        else
                        {
                            // fallback to native assets which match the file name in the plugin base directory
                            var localFile = Path.Combine(_basePath, prefix + unmanagedDllName + suffix);
                            if (File.Exists(localFile))
                            {
                                return LoadUnmanagedDllFromResolvedPath(localFile);
                            }

                            var localFileWithoutSuffix = Path.Combine(_basePath, prefix + unmanagedDllName);
                            if (File.Exists(localFileWithoutSuffix))
                            {
                                return LoadUnmanagedDllFromResolvedPath(localFileWithoutSuffix);
                            }
                        }
                    }

                }
            }

            return base.LoadUnmanagedDll(unmanagedDllName);
        }

        private bool SearchForLibrary(ManagedLibrary library, out string path)
        {
            if (!_sharedLibBehavior.TryGetValue(library.Name.Name, out var behavior))
            {
                behavior = _defaultLibBehavior;
            }

            string sharedLib = null;
            if (!string.IsNullOrEmpty(this._sharedBasePath))
            {
                sharedLib = Path.Combine(this._sharedBasePath, library.AppLocalPath);
                if (!File.Exists(sharedLib))
                {
                    sharedLib = null;
                }
            }

            var localFile = Path.Combine(this._basePath, library.AppLocalPath);
            var localFileExists = File.Exists(localFile);
            if (sharedLib != null)
            {
                if (behavior == SharedLibLoadBehavior.PreferLocal && localFileExists)
                {
                    path = localFile;
                    return true;
                }

                if (behavior == SharedLibLoadBehavior.HighestVersion && localFileExists)
                {
                    if (CompareLibraries(localFile, sharedLib) >= 0)
                    {
                        path = localFile;
                        return true;
                    }
                }

                // prefer shared behavior
                Logger.Trace(m => m("Loading shared library from [{0}]. Version {1}", 
                                        sharedLib, 
                                        FileVersionInfo.GetVersionInfo(sharedLib).FileVersion));
                path = sharedLib;
                return true;
            }
            else
            {
                // no shared lib - always check local
                if (localFileExists)
                {
                    path = localFile;
                    return true;
                }
            }

            // 2. Search additional probing paths
            foreach (var searchPath in _additionalProbingPaths)
            {
                var candidate = Path.Combine(searchPath, library.AdditionalProbingPath);
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            // 3. Search in base path
            foreach (var ext in PlatformInformation.ManagedAssemblyExtensions)
            {
                var local = Path.Combine(_basePath, library.Name.Name + ext);
                if (File.Exists(local))
                {
                    path = local;
                    return true;
                }
            }

            path = null;
            return false;
        }

        private bool SearchForLibrary(NativeLibrary library, string prefix, out string path)
        {
            // 1. Search in base path
            foreach (var ext in PlatformInformation.NativeLibraryExtensions)
            {
                var candidate = Path.Combine(_basePath, $"{prefix}{library.Name}{ext}");
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            // 2. Search in base path + app local (for portable deployments of netcoreapp)
            var local = Path.Combine(_basePath, library.AppLocalPath);
            if (File.Exists(local))
            {
                path = local;
                return true;
            }

            // 3. Search additional probing paths
            foreach (var searchPath in _additionalProbingPaths)
            {
                var candidate = Path.Combine(searchPath, library.AdditionalProbingPath);
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            path = null;
            return false;
        }

        private IntPtr LoadUnmanagedDllFromResolvedPath(string unmanagedDllPath)
        {
            var normalized = Path.GetFullPath(unmanagedDllPath);
            return LoadUnmanagedDllFromPath(normalized);
        }

        /// <summary>
        /// 0 - version match
        /// > 0 - first is higher,
        /// < 0 - second is higher
        /// </summary>
        /// <returns></returns>
        private int CompareLibraries(FileVersionInfo first, FileVersionInfo second)
        {
            if (!Version.TryParse(first.FileVersion, out var firstVersion))
            {
                firstVersion = new Version(0, 0, 0, 0);
            }
            if (!Version.TryParse(second.FileVersion, out var secondVersion))
            {
                secondVersion = new Version(0, 0, 0, 0);
            }

            return firstVersion.CompareTo(secondVersion);
        }

        /// <summary>
        /// 0 - version match
        /// > 0 - first is higher,
        /// < 0 - second is higher
        /// </summary>
        /// <returns></returns>
        private int CompareLibraries(string firstPath, string secondPath)
        {
            var firstVersionInfo = FileVersionInfo.GetVersionInfo(firstPath);
            var secondVersionInfo = FileVersionInfo.GetVersionInfo(secondPath);

            return CompareLibraries(firstVersionInfo, secondVersionInfo);
        }
    }
}
