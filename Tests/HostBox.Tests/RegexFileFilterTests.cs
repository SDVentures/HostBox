using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using HostBox.Configuration;

using Xunit;

namespace HostBox.Tests;

public class RegexFileFilterTests
{
    private readonly Regex prefixRegex = new(@"^(?:\[(?<prefix>\S*)\]\.)*[a-zA-Z\d\.]+.settings\.json$");

    private readonly Regex prefixSuffixRegex = new(@"^(?:\[(?<prefix>\S*)\]\.)*[a-zA-Z\d\.]+\.(?:\[(?<suffix>\S*)\]\.)*settings\.json$");

    private readonly IConfigFileFilter dummyConfigFileFilter = new DummyConfigFileFilter();

    [Theory]
    [InlineData(@"\tmp\[key].app.settings.json", true)]
    [InlineData(@"\tmp\[fake].app.settings.json", false)]
    [InlineData(@"\tmp\app.settings.json", true)]
    public void Filter_OneGroup_Accepted(string filePath, bool accepted)
    {
        var filter = new RegexFileFilter(
            this.prefixRegex,
            new Dictionary<string, string[]>
                {
                    {"prefix", new[] {"key"}}
                },
            this.dummyConfigFileFilter);

        var result = filter.Filter(new[] {filePath});

        Assert.Equal(accepted, result.Any());
    }

    [Theory]
    [InlineData(@"\tmp\[key].app.settings.json", true)]
    [InlineData(@"\tmp\[fake].app.settings.json", false)]
    [InlineData(@"\tmp\app.settings.json", true)]
    [InlineData(@"\tmp\[key].app.[1.0].settings.json", true)]
    [InlineData(@"\tmp\[key].app.[2.0].settings.json", false)]
    [InlineData(@"\tmp\app.[1.0].settings.json", true)]
    public void Filter_TwoGroups_Accepted(string filePath, bool accepted)
    {
        var filter = new RegexFileFilter(
            this.prefixSuffixRegex,
            new Dictionary<string, string[]>
                {
                    {"prefix", new[] {"key"}},
                    {"suffix", new[] {"1.0"}}
                },
            this.dummyConfigFileFilter);

        var result = filter.Filter(new[] {filePath});

        Assert.Equal(accepted, result.Any());
    }

    [Theory]
    [InlineData(@"\tmp\[key].app.settings.json", true)]
    [InlineData(@"\tmp\[wow].app.settings.json", true)]
    [InlineData(@"\tmp\[fake].app.settings.json", false)]
    [InlineData(@"\tmp\app.settings.json", true)]
    public void Filter_TwoGroupValues_Accepted(string filePath, bool accepted)
    {
        var filter = new RegexFileFilter(
            this.prefixRegex,
            new Dictionary<string, string[]>
                {
                    {"prefix", new[] {"key", "wow"}}
                },
            this.dummyConfigFileFilter);

        var result = filter.Filter(new[] {filePath});

        Assert.Equal(accepted, result.Any());
    }

    [Theory]
    [InlineData(@"\tmp\[key1].app.settings.json", "key1", true)]
    [InlineData(@"\tmp\[1app].app.settings.json", "1app", true)]
    [InlineData(@"\tmp\[001].app.settings.json", "001", true)]
    [InlineData(@"\tmp\[x++].app.settings.json", "x++", false)]
    [InlineData(@"\tmp\[].app.settings.json", "", false)]
    public void Filter_PrefixFormat(string filePath, string prefix, bool accepted)
    {
        var filter = new RegexFileFilter(
            this.prefixRegex,
            new Dictionary<string, string[]>
                {
                    {"prefix", new[] {prefix}}
                },
            this.dummyConfigFileFilter);

        var result = filter.Filter(new[] {filePath});

        Assert.Equal(accepted, result.Any());
    }

    [Fact]
    public void Filter_OutputOrder()
    {
        var filter = new RegexFileFilter(
            this.prefixRegex,
            new Dictionary<string, string[]>
                {
                    {"prefix", new[] {"key"}}
                },
            this.dummyConfigFileFilter);
        var files = new[]
                        {
                            (file: @"\tmp\[key].app.settings.json", order: 2),
                            (file: @"\tmp\app.settings.json", order: 0),
                            (file: @"\tmp\[key].bus.settings.json", order: 3),
                            (file: @"\tmp\bus.settings.json", order: 1)
                        };

        var result = filter.Filter(files.Select(x => x.file));

        var correctOrderedFiles = result.Where((file, index) => files.Any(source => source.file == file && source.order == index));
        Assert.Equal(files.Length, correctOrderedFiles.Count());
    }
}