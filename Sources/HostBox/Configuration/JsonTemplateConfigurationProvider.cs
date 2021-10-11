using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using NLog.LayoutRenderers;

namespace HostBox.Configuration
{
    public class JsonTemplateConfigurationProvider : JsonConfigurationProvider
    {
        private readonly List<IConfigurationProvider> valuesProviders;

        private readonly string placeholderStart;
        private readonly string placeholderEnd;
        private static readonly ILog Logger = LogManager.GetLogger<JsonTemplateConfigurationProvider>();

        /// <inheritdoc />
        public JsonTemplateConfigurationProvider(JsonTemplateConfigurationSource source, IEnumerable<IConfigurationProvider> valuesProviders, string placeholderPattern)
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
        }

        public override void Load(Stream stream)
        {
            base.Load(stream);

            var changes = new Dictionary<string, string>();
            var defaultValues = new Dictionary<string, string>();

            foreach (var pair in this.Data)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                var placeholders = this.FindPlaceholders(pair.Value)
                    .GroupBy(p => p.Name)
                    .Select(g => g.First())
                    .ToArray();

                if (placeholders.Length == 0)
                {
                    continue;
                }

                var result = pair.Value;

                foreach (var placeholder in placeholders)
                {
                    if (placeholder.DefaultValue != null)
                    {
                        if (defaultValues.TryGetValue(placeholder.Name, out var existing))
                        {
                            if (existing != placeholder.DefaultValue)
                            {
                                throw new Exception($"For placeholder [{placeholder.Name}] different default values were specified. Use same default for one placeholder");
                            }
                        }
                        else
                        {
                            defaultValues[placeholder.Name] = placeholder.DefaultValue;
                        }
                    }

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
                            throw new KeyNotFoundException($"Configuration path [{pair.Key}] contains placeholder [{placeholder}] that not present in values provider.");
                        }

                        value = placeholder.DefaultValue;
                        Logger.Info(m => m($"Placeholder [{placeholder.Name}] default value [{placeholder.DefaultValue}] used"));
                    }

                    result = result.Replace(placeholder.ToString(), value);
                }

                changes[pair.Key] = result;
            }

            foreach (var key in changes.Keys)
            {
                this.Data[key] = changes[key];
            }
        }

        private IEnumerable<Placeholder> FindPlaceholders(string source)
        {
            var startIndex = source.IndexOf(this.placeholderStart, StringComparison.InvariantCulture);

            while (startIndex >= 0)
            {
                var nameStartIndex = startIndex + this.placeholderStart.Length;

                var endIndex = source.IndexOf(this.placeholderEnd, nameStartIndex, StringComparison.InvariantCulture);

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

                yield return new Placeholder(this.placeholderStart, this.placeholderEnd, name, defaultValue);

                startIndex = source.IndexOf(
                    this.placeholderStart,
                    endIndex + this.placeholderEnd.Length,
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
    }
}