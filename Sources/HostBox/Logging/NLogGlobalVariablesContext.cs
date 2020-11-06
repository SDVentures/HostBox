using ContextNLog = NLog.MappedDiagnosticsContext;
using Common.Logging;

namespace HostBox.Logging
{
    public class NLogGlobalVariablesContext : IVariablesContext
    {
        /// <inheritdoc />
        public void Set(string key, object value)
        {
            ContextNLog.Set(key, value != null ? value.ToString() : null);
        }

        /// <inheritdoc />
        public object Get(string key)
        {
            return ContextNLog.Get(key);
        }

        /// <inheritdoc />
        public bool Contains(string key)
        {
            return ContextNLog.Contains(key);
        }

        /// <inheritdoc />
        public void Remove(string key)
        {
            ContextNLog.Remove(key);
        }

        /// <inheritdoc />
        public void Clear()
        {
            ContextNLog.Clear();
        }
    }

}