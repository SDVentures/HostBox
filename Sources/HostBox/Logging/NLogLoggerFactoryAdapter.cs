using System;
using System.IO;
using System.Xml;
using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Factory;
using NLog.Config;

namespace HostBox.Logging
{
    public class NLogLoggerFactoryAdapter : AbstractCachingLoggerFactoryAdapter
    {
        public NLogLoggerFactoryAdapter(NameValueCollection properties)
            : base(true)
        {
            string configType = string.Empty;
            string configFile = string.Empty;

            if (properties != null)
            {
                if (properties["configType"] != null)
                {
                    configType = properties["configType"].ToUpper();
                }

                if (properties["configFile"] != null)
                {
                    configFile = properties["configFile"];
                    if (configFile.StartsWith("~/") || configFile.StartsWith("~\\"))
                    {
                        configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/', '\\') + "/", configFile.Substring(2));
                    }
                }

                if (configType == "FILE")
                {
                    if (configFile == string.Empty)
                    {
                        throw new ConfigurationException("Configuration property 'configFile' must be set for NLog configuration of type 'FILE'.");
                    }

                    if (!File.Exists(configFile))
                    {
                        throw new ConfigurationException("NLog configuration file '" + configFile + "' does not exists");
                    }
                }
            }
            switch (configType)
            {
                case "INLINE":
                    break;
                case "FILE":
                    global::NLog.LogManager.Configuration = GetConfiguration(configFile);
                    break;
                default:
                    break;
            }
        }

        private static XmlLoggingConfiguration GetConfiguration(string configFile)
        {
            var fileStream = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1);
            using (var reader = XmlReader.Create(fileStream))
            {
                return new global::NLog.Config.XmlLoggingConfiguration(reader);
            }
        }

        /// <inheritdoc />
        protected override ILog CreateLogger(string name)
        {
            return new NLogLogger(global::NLog.LogManager.GetLogger(name));
        }
    }
}
