using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using HostBox.Configuration;

using Microsoft.Extensions.Configuration;

using Xunit;

using HostBox.Extensions;

namespace HostBox.Tests;

public class ConfigurationExtensionsTests
{
    private const string CompiledRegexConfigFilterJsonTwoGroups = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegex"": ""^\\[(?<prefix>\\S*)\\]\\.[a-zA-Z\\d\\.]+.\\[(?<suffix>\\S*)\\].settings\\.json$"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""],
    ""suffix"": [""VERSION""]
  },
  ""PRODUCT_KEY"": ""one"",
  ""VERSION"": ""1.0""
}";

    private const string CompiledRegexConfigFilterJson = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegex"": ""^\\[(?<prefix>\\S*)\\]\\.[a-zA-Z\\d\\.]+.settings\\.json$"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""]
  },
  ""PRODUCT_KEY"": ""one""
}";

    private const string CompiledDummyConfigFilterJsonNoRegex = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""]
  },
  ""PRODUCT_KEY"": ""one""
}";

    private const string CompiledDummyConfigFilterJsonNoRegexKey = @"{
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegex"": ""^\\[(?<prefix>\\S*)\\]\\.[a-zA-Z\\d\\.]+.settings\\.json$"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""]
  },
  ""PRODUCT_KEY"": ""one""
}";

    private const string CompiledDummyConfigFilterJsonNoGroupsKey = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegex"": ""^\\[(?<prefix>\\S*)\\]\\.[a-zA-Z\\d\\.]+.settings\\.json$"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""]
  },
  ""PRODUCT_KEY"": ""one""
}";

    private const string CompiledDummyConfigFilterJsonNoRegexGroupDef = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegex"": ""^\\[a-zA-Z\\d\\.]+.settings\\.json$"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""]
  },
  ""PRODUCT_KEY"": ""one""
}";

    private const string CompiledDummyConfigFilterJsonNoRegexGroupKey = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegex"": ""^\\[(?<prefix>\\S*)\\]\\.[a-zA-Z\\d\\.]+.settings\\.json$"",
  ""fileRegexGroups"": {
    ""group1"": [""PRODUCT_KEY""]
  },
  ""PRODUCT_KEY"": ""one""
}";

    private const string CompiledDummyConfigFilterJsonNoGroupValue = @"{
  ""fileRegexConfigKey"": ""fileRegex"",
  ""fileRegexGroupsConfigKey"": ""fileRegexGroups"",
  ""fileRegex"": ""^\\[(?<prefix>\\S*)\\]\\.[a-zA-Z\\d\\.]+.settings\\.json$"",
  ""fileRegexGroups"": {
    ""prefix"": [""PRODUCT_KEY""]
  }
}";

    [Theory]
    [InlineData(CompiledRegexConfigFilterJson)]
    [InlineData(CompiledRegexConfigFilterJsonTwoGroups)]
    public void GetConfigFileFilter_RegexFileFilter(string json)
    {
        var configuration = GetJsonConfiguration(json);

        var configFilter = configuration.GetConfigFileFilter("fileRegexConfigKey", "fileRegexGroupsConfigKey");

        Assert.IsType<RegexFileFilter>(configFilter);
    }

    [Theory]
    [InlineData(CompiledDummyConfigFilterJsonNoRegexKey)]
    [InlineData(CompiledDummyConfigFilterJsonNoGroupsKey)]
    [InlineData(CompiledDummyConfigFilterJsonNoRegex)]
    [InlineData(CompiledDummyConfigFilterJsonNoRegexGroupDef)]
    [InlineData(CompiledDummyConfigFilterJsonNoRegexGroupKey)]
    [InlineData(CompiledDummyConfigFilterJsonNoGroupValue)]
    public void GetConfigFileFilter_DummyConfigFileFilter(string json)
    {
        var configuration = GetJsonConfiguration(json);

        var configFilter = configuration.GetConfigFileFilter("fileRegexConfigKey", "fileRegexGroupsConfigKey");

        Assert.IsType<DummyConfigFileFilter>(configFilter);
    }

    private static IConfiguration GetJsonConfiguration(string json)
    {
        var builder = new ConfigurationBuilder();
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        builder.AddJsonStream(jsonStream);
        return builder.Build();
    }
}