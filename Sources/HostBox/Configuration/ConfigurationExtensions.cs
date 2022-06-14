using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;

namespace HostBox.Configuration
{
    internal static class ConfigurationExtensions
    {
        public static void OnChange(
            this IConfiguration config,
            Borderline.IConfiguration borderlineConfig,
            Action<Borderline.IConfiguration> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            ChangeToken.OnChange(
                changeTokenProducer: () => config.GetReloadToken(),
                changeTokenConsumer: () => callback.Invoke(borderlineConfig));
        }
    }
}
