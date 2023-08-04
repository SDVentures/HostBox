using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace HostBox.Configuration
{
    public class JsonTemplateConfigurationProvider : JsonConfigurationProvider
    {
        private readonly List<IConfigurationProvider> valuesProviders;

        private readonly string placeholderStart;
        private readonly string placeholderEnd;
        private readonly string envPlaceholderStart;
        private readonly string envPlaceholderEnd;
        private static readonly ILog Logger = LogManager.GetLogger<JsonTemplateConfigurationProvider>();

        /// <inheritdoc />
        public JsonTemplateConfigurationProvider(
            JsonTemplateConfigurationSource source,
            IEnumerable<IConfigurationProvider> valuesProviders,
            string placeholderPattern,
            string envPlaceholderPattern)
            : base(source)
        {
            this.valuesProviders = valuesProviders.ToList();

            var parts = placeholderPattern.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new ArgumentException("Placeholder should implement format '<START>*<END>'.");
            }

            this.placeholderStart = parts[0];
            this.placeholderEnd = parts[1];

            parts = envPlaceholderPattern.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new ArgumentException("Environment variables placeholder should implement format '<START>*<END>'.");
            }

            this.envPlaceholderStart = parts[0];
            this.envPlaceholderEnd = parts[1];
        }

        public override void Load(Stream stream)
        {
            base.Load(stream);

            ReplacePlaceholders();
        }

        private void ReplacePlaceholders()
        {
            var changes = new Dictionary<string, string>();
            var defaultValues = new Dictionary<string, string>();
            var envDefaultValues = new Dictionary<string, string>();

            foreach (var pair in this.Data)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                var newValue = pair.Value;
                var replaced = false;
                var replaceFromValuesFileResult = ReplaceValue(pair.Key, pair.Value, this.placeholderStart, this.placeholderEnd, ref defaultValues, ProvideValueFromValuesFile);
                if (replaceFromValuesFileResult.Success)
                {
                    newValue = replaceFromValuesFileResult.Value;
                    replaced = true;
                }

                var replaceFromEnvVariableResult = ReplaceValue(pair.Key, newValue, this.envPlaceholderStart, this.envPlaceholderEnd, ref envDefaultValues, ProvideValueFromEnvVariable);
                if (replaceFromEnvVariableResult.Success)
                {
                    newValue = replaceFromEnvVariableResult.Value;
                    replaced = true;
                }

                if (replaced)
                {
                    changes[pair.Key] = newValue;
                }
            }

            foreach (var key in changes.Keys)
            {
                this.Data[key] = changes[key];
            }
        }

        private ReplaceResult ReplaceValue(string confKey, string confValue, string placeholderStart, string placeholderEnd, ref Dictionary<string, string> defaultValues, Func<string, string, Placeholder, string> valueProvider)
        {
            var placeholders = this.FindPlaceholders(confValue, placeholderStart, placeholderEnd)
                .GroupBy(p => p.Name)
                .Select(g => g.First())
                .ToArray();

            if (placeholders.Length == 0)
            {
                return new ReplaceResult { Value = confValue };
            }

            var result = confValue;

            foreach (var placeholder in placeholders)
            {
                if (placeholder.DefaultValue != null)
                {
                    if (defaultValues.TryGetValue(placeholder.Name, out var existing))
                    {
                        if (existing != placeholder.DefaultValue)
                        {
                            throw new Exception($"For placeholder [{placeholder.Name}] with pattern {placeholderStart}{placeholderEnd} different default values were specified. Use same default for one placeholder");
                        }
                    }
                    else
                    {
                        defaultValues[placeholder.Name] = placeholder.DefaultValue;
                    }
                }

                var value = valueProvider(confKey, confValue, placeholder);
                result = result.Replace(placeholder.ToString(), value);

            }

            return new ReplaceResult { Success = true, Value = result };
        }

        private string ProvideValueFromValuesFile(string confKey, string confValue, Placeholder placeholder)
        {
            string value = null;
            var providersWithValueCount = 0;
            foreach (var valuesProvider in this.valuesProviders)
            {
                if (!valuesProvider.TryGet(placeholder.Name, out var currentVal))
                {
                    continue;
                }

                providersWithValueCount++;
                if (providersWithValueCount > 1)
                {
                    throw new Exception($"Placeholder [{placeholder.Name}] exists in multiple values file. Only one file should have the value");
                }

                value = currentVal;
            }

            if (providersWithValueCount == 0)
            {
                if (placeholder.DefaultValue == null)
                {
                    throw new KeyNotFoundException($"Configuration path [{confKey}] contains placeholder [{placeholder}] that not present in values provider.");
                }

                value = placeholder.DefaultValue;
                Logger.Info(m => m($"Placeholder [{placeholder.Name}] default value [{placeholder.DefaultValue}] used"));
            }

            return value;
        }

        private string ProvideValueFromEnvVariable(string confKey, string confValue, Placeholder placeholder)
        {
            var value = Environment.GetEnvironmentVariable(placeholder.Name, EnvironmentVariableTarget.Process);
            if (value == null && placeholder.DefaultValue == null)
            {
                throw new KeyNotFoundException($"Configuration path [{confKey}] contains placeholder [{placeholder}] that not present in environment variables.");
            }

            return value ?? placeholder.DefaultValue;
        }

        private IEnumerable<Placeholder> FindPlaceholders(string source, string placeholderStart, string placeholderEnd)
        {
            var startIndex = source.IndexOf(placeholderStart, StringComparison.InvariantCulture);

            while (startIndex >= 0)
            {
                var nameStartIndex = startIndex + placeholderStart.Length;

                var endIndex = source.IndexOf(placeholderEnd, nameStartIndex, StringComparison.InvariantCulture);

                if (endIndex < nameStartIndex)
                {
                    yield break;
                }

                var name = source.Substring(nameStartIndex, endIndex - nameStartIndex);

                var defaultValueIdx = name.IndexOf("=", StringComparison.Ordinal);

                string defaultValue = null;
                if (defaultValueIdx != -1)
                {
                    defaultValue = name.Substring(defaultValueIdx + 1);
                    name = name.Substring(0, defaultValueIdx);
                }

                yield return new Placeholder(placeholderStart, placeholderEnd, name, defaultValue);

                startIndex = source.IndexOf(
                    placeholderStart,
                    endIndex + placeholderEnd.Length,
                    StringComparison.InvariantCulture);
            }
        }

        private class Placeholder
        {
            public Placeholder(string start, string end, string name, string defaultValue)
            {
                this.Start = start;
                this.End = end;
                this.Name = name;
                this.DefaultValue = defaultValue;
            }

            public string Start { get; }

            public string End { get; }

            public string Name { get; }

            public string DefaultValue { get; }

            public override string ToString()
            {
                return $"{this.Start}{this.Name}{(DefaultValue == null ? null : "=" + DefaultValue)}{this.End}";
            }
        }

        private class ReplaceResult
        {
            public bool Success { get; set; }

            public string Value { get; set; }
        }
    }
}