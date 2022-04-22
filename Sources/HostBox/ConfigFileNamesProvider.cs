using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HostBox
{
    public class ConfigFileNamesProvider
    {
        private const string SettingsPath = "settings";

        private const string SettingsOverridingPath = "settingsOverriding";

        private const string ValuesPath = "values";

        private const string ConfigFilePattern = "*.settings.json";

        private readonly Regex prioritySettingRegex = new Regex(@"^(\[p(?<priority>\d+)\]\.)?[\w\.]+\.settings\.json$");

        private readonly string configName;

        private readonly string basePath;

        private readonly string settingOverridingFullPath;

        public ConfigFileNamesProvider(string configName, string basePath, string sharedLibraryPath)
        {
            this.configName = configName;
            this.basePath = basePath;
            this.settingOverridingFullPath = Path.Combine(sharedLibraryPath, SettingsOverridingPath);
        }

        public IEnumerable<string> GetTemplateValuesFiles()
        {
            var path = Path.Combine(this.basePath, SettingsPath, ValuesPath);

            if (!Directory.Exists(path))
            {
                yield break;
            }
            
            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }

        public IEnumerable<string> EnumerateConfigFiles()
        {
            var result = new List<(string filePath, int priority)>();

            var filesPaths = new[] { (SettingsPath, 0) }
                             .Concat(this.EnumerateEnvSubfolders().Select(x => (x, 50)))
                             .Concat(new[] { (this.settingOverridingFullPath, 100) });

            foreach (var (settingsPath, defaultPriority) in filesPaths)
            {
                foreach (var settingFilePath in this.EnumerateFiles(settingsPath))
                {
                    var settingFileName = Path.GetFileName(settingFilePath);

                    var match = this.prioritySettingRegex.Match(settingFileName);

                    if (!match.Success)
                    {
                        throw new Exception($"Config file must match regex [{this.prioritySettingRegex}]");
                    }

                    var matchPriority = match.Groups["priority"];
                    var settingFilePriority = matchPriority.Success ? int.Parse(matchPriority.Value) : defaultPriority;

                    result.Add((settingFilePath, settingFilePriority));
                }
            }

            return result.OrderBy(x => x.priority)
                         .Select(x => x.filePath);
        }

        private IEnumerable<string> EnumerateEnvSubfolders()
        {
            if (string.IsNullOrEmpty(this.configName))
            {
                yield break;
            }

            yield return Path.Combine(SettingsPath, this.configName.ToLower());
        }

        private IEnumerable<string> EnumerateFiles(string path)
        {
            var fullPath = Path.Combine(this.basePath, path);

            if (!Directory.Exists(fullPath))
            {
                yield break;
            }

            foreach (var file in Directory.GetFiles(fullPath, ConfigFilePattern, SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }
    }
}