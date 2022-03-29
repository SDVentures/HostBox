using System.Collections.Generic;

namespace HostBox.Configuration
{
    public interface IConfigFileFilter
    {
        IEnumerable<string> Filter(IEnumerable<string> files);
    }
}