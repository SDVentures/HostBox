using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using HostBox.Configuration;

using Microsoft.Extensions.Configuration;

namespace HostBox.Extensions
{
    public static class ConfigurationExtensions
    {
        public static bool TryGetValue(this IConfiguration configuration, string configKey, out string value)
        {
            value = configuration[configKey];
            return !string.IsNullOrEmpty(value);
        }

        public static IConfiguration CopyCombined(this IConfiguration configuration, IConfigurationBuilder additionalConfig)
        {
            var newBuilder = new ConfigurationBuilder();
            newBuilder.AddConfiguration(configuration);

            foreach (var source in additionalConfig.Sources)
            {
                newBuilder.Add(source);
            }

            return newBuilder.Build();
        }

        public static IConfigFileFilter GetConfigFileFilter(this IConfiguration configuration, string regexSourceKey, string regexGroupsSourceKey)
        {
            var dummyConfigFileFilter = new DummyConfigFileFilter();

            if (configuration.TryGetValue(regexGroupsSourceKey, out var regexGroupsConfigKey) &&
                configuration.TryGetValue(regexSourceKey, out var regexConfigKey) &&
                configuration.TryGetValue(regexConfigKey, out var regexConfigValue))
            {
                var regex = new Regex(regexConfigValue);
                var regexGroupNames = regex.GetGroupNames().Where(x => x != "0").ToList();

                var groupsValues = configuration.GetSection(regexGroupsConfigKey)
                    .Get<Dictionary<string, string[]>>()
                    .Where(x => regexGroupNames.Contains(x.Key)) // get only groups which specified in the regex
                    .Where(x => x.Value != null && x.Value.Any()) // exclude groups with undefined array of config keys
                    .Select(GetGroupValues)
                    .Where(x => x.values.Any())
                    .ToDictionary(x => x.group, x => x.values);

                if (groupsValues.Any())
                {
                    return new RegexFileFilter(regex, groupsValues, nextFilter: dummyConfigFileFilter);
                }
            }

            return dummyConfigFileFilter;

            (string group, string[] values) GetGroupValues(KeyValuePair<string, string[]> groupValuesKeys) =>
                (
                    group: groupValuesKeys.Key,
                    values: groupValuesKeys.Value
                        .Select(valueConfig => configuration[valueConfig]) // obtain the groups' values
                        .Where(groupValue => !string.IsNullOrEmpty(groupValue)) // exclude undefined values
                        .ToArray()
                );
        }
    }
}