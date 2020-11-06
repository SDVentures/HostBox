using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace HostBox.Configuration
{
    public class JsonTemplateConfigurationProvider : JsonConfigurationProvider
    {
        private readonly IConfigurationProvider valuesProvider;

        private readonly string placeholderStart;
        private readonly string placeholderEnd;

        /// <inheritdoc />
        public JsonTemplateConfigurationProvider(JsonTemplateConfigurationSource source, IConfigurationProvider valuesProvider, string placeholderPattern)
            : base(source)
        {
            this.valuesProvider = valuesProvider;

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
                    if (!this.valuesProvider.TryGet(placeholder.Name, out var value))
                    {
                        throw new KeyNotFoundException($"Configuration path [{pair.Key}] contains placeholder [{placeholder}] that not present in values provider.");
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

                yield return new Placeholder(this.placeholderStart, this.placeholderEnd, name);

                startIndex = source.IndexOf(
                    this.placeholderStart,
                    endIndex + this.placeholderEnd.Length,
                    StringComparison.InvariantCulture);
            }
        }

        private class Placeholder
        {
            public Placeholder(string start, string end, string name)
            {
                this.Start = start;
                this.End = end;
                this.Name = name;
            }

            public string Start { get; }

            public string End { get; }

            public string Name { get; }

            public override string ToString()
            {
                return $"{this.Start}{this.Name}{this.End}";
            }
        }
    }
}