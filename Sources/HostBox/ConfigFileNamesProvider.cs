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

        private readonly Regex prioritySettingRegex = new Regex(@"^(?<settingName>\w+)\.(?<priority>\d+)\.settings\.json$");

        private readonly Regex settingNameRegex = new Regex(@"^(?<settingName>\w+)(\.(?<priority>\d+))?\.settings\.json$");

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
            IEnumerable<string> result = null;

            var configFiles = this.GetFilesWithPath(SettingsPath);
            var overridingConfigFiles = this.GetFilesWithPath(this.settingOverridingFullPath);

            var configFilesMatches = configFiles.Select(x => x.Key)
                                                .Select(x => this.prioritySettingRegex.Match(x))
                                                .Where(m => m.Success)
                                                .ToList();

            if (configFilesMatches.Any())
            {
                var overridingResult = new List<string>();

                foreach (var configFileMatch in configFilesMatches)
                {
                    var settingName = configFileMatch.Groups["settingName"].Value;
                    var configPriority = int.Parse(configFileMatch.Groups["priority"].Value);
                    var configFilePath = configFiles[configFileMatch.Value];

                    foreach (var (overridingFileName, overridingFilePath) in overridingConfigFiles)
                    {
                        var match = this.settingNameRegex.Match(overridingFileName);

                        if (!match.Success || settingName != match.Groups["settingName"].Value)
                        {
                            continue;
                        }

                        var matchPriority = match.Groups["priority"];
                        var overridingConfigPriority = matchPriority.Success ? int.Parse(matchPriority.Value) : 1;

                        if (configPriority > overridingConfigPriority)
                        {
                            overridingResult.Add(overridingFilePath);
                            overridingResult.Add(configFilePath);
                        }
                        else
                        {
                            overridingResult.Add(configFilePath);
                            overridingResult.Add(overridingFilePath);
                        }

                        break;
                    }
                }

                result = overridingResult.Union(configFiles.Select(x => x.Value))
                                         .Union(overridingConfigFiles.Select(x => x.Value))
                                         .Distinct();
            }
            else
            {
                result = configFiles.Select(x => x.Value).Union(overridingConfigFiles.Select(x => x.Value));
            }

            return result.Union(this.EnumerateEnvSubfolders().SelectMany(this.EnumerateFiles));
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

        private Dictionary<string, string> GetFilesWithPath(string path)
        {
            return this.EnumerateFiles(path).ToDictionary(Path.GetFileName, p => p);
        }
    }
}