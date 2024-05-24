// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace McMaster.NETCore.Plugins
{
    internal class PluginLoadConfig
    {
        public string Assembly { get; set; }

        public Type[] SharedTypes { get; set; }

        public string SharedPath { get; set; }

        public SharedLibLoadBehavior DefaultSharedLibBehavior { get; set; }

        public Dictionary<string, SharedLibLoadBehavior> SharedLibBehavior { get; set; }
    }
}
