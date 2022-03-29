using System.Collections.Generic;

namespace HostBox.Configuration
{
    public class DummyConfigFileFilter : IConfigFileFilter
    {
        public IEnumerable<string> Filter(IEnumerable<string> files) => files;
    }
}