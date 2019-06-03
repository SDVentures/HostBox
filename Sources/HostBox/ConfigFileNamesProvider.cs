using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HostBox
{
    public class ConfigFileNamesProvider
    {
        private const string SettingsPath = "settings";

        private const string ValuesPath = "values";

        private const string ConfigFilePattern = "*.settings.json";

        private const string TemplateValuesFile = "values.json";

        private readonly string configName;

        private readonly string basePath;

        public ConfigFileNamesProvider(string configName, string basePath)
        {
            this.configName = configName;
            this.basePath = basePath;
        }

        public string GetTemplateValuesFile()
        {
            return Path.Combine(this.basePath, SettingsPath, ValuesPath, TemplateValuesFile);
        }

        public IEnumerable<string> EnumerateConfigFiles()
        {
            var pathsToSearchIn = new[] { SettingsPath }.Union(this.EnumerateEnvSubfolders());

            foreach (var path in pathsToSearchIn)
            {
                var fullPath = Path.Combine(this.basePath, path);

                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(fullPath, ConfigFilePattern, SearchOption.TopDirectoryOnly))
                {
                    yield return file;
                }
            }
        }

        private IEnumerable<string> EnumerateEnvSubfolders()
        {
            if (string.IsNullOrEmpty(this.configName))
            {
                yield break;
            }

            yield return Path.Combine(SettingsPath, this.configName.ToLower());
        }
    }
}