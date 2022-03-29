using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HostBox.Configuration
{
    public class RegexFileFilter : IConfigFileFilter
    {
        private readonly IConfigFileFilter nextFilter;

        private readonly Regex regex;

        private readonly IDictionary<string, List<string>> groupsValues;

        public RegexFileFilter(Regex regex, IDictionary<string, string[]> groupsValues, IConfigFileFilter nextFilter)
        {
            this.regex = regex;
            this.groupsValues = groupsValues.ToDictionary(x => x.Key, x => x.Value.Select(v => v.ToLower()).ToList());
            this.nextFilter = nextFilter;
        }

        public IEnumerable<string> Filter(IEnumerable<string> files)
        {
            return this.nextFilter.Filter(
                this.FilterByGroups(files)
                    .OrderBy(x => x.hasGroups)
                    .Select(x => x.file));
        }

        private IEnumerable<(string file, bool hasGroups)> FilterByGroups(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var match = this.regex.Match(Path.GetFileName(file));

                if (match.Success)
                {
                    if (this.IsFilterPassed(match, out var hasGroups))
                    {
                        yield return (file, hasGroups);
                    }
                }
                else
                {
                    yield return (file, hasGroups: false);
                }
            }
        }

        private bool IsFilterPassed(Match fileNameMatch, out bool hasGroups)
        {
            hasGroups = false;

            // check only groups which has specified filter values, because if the name doesn't contain the group it should pass the filter
            var matchGroupKeys = GetKeys(fileNameMatch.Groups)
                .Where(this.groupsValues.ContainsKey)
                .Where(group => fileNameMatch.Groups[group].Success);

            foreach (var matchGroup in matchGroupKeys)
            {
                hasGroups = true;
                var groupValue = fileNameMatch.Groups[matchGroup].Value;

                if (string.IsNullOrEmpty(groupValue) || !groupValue.All(ch => char.IsLetterOrDigit(ch) || ch == '.'))
                {
                    return false;
                }

                if (!this.groupsValues[matchGroup].Contains(groupValue.ToLower()))
                {
                    // if at least one group doesn't pass by value the file doesn't pass the filter
                    return false;
                }
            }

            return true;

            IEnumerable<string> GetKeys(GroupCollection groups)
            {
#if NETCOREAPP2_1
                for (var i = 0; i < groups.Count; ++i)
                {
                    yield return groups[i].Name;
                }
#else
                return groups.Keys;
#endif
            }
        }
    }
}