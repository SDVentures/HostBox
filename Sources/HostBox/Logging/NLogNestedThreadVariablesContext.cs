using System;

using ContextNLog = NLog.NestedDiagnosticsContext;
using Common.Logging;

namespace HostBox.Logging
{
    public class NLogNestedThreadVariablesContext : INestedVariablesContext
    {
        /// <inheritdoc />
        public IDisposable Push(string text)
        {
            return ContextNLog.Push(text);
        }

        /// <inheritdoc />
        public string Pop()
        {
            return ContextNLog.Pop();
        }

        /// <inheritdoc />
        public void Clear()
        {
            ContextNLog.Clear();
        }

        /// <inheritdoc />
        public bool HasItems => ContextNLog.GetAllMessages().Length > 0;
    }
}