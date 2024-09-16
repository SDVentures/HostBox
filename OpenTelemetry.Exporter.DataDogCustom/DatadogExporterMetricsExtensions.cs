using OpenTelemetry.Metrics;

namespace OpenTelemetry.Exporter.DataDogCustom;

public static class DatadogExporterMetricsExtensions
{
    private const int DefaultExportIntervalMilliseconds = 10000;
    private const int DefaultExportTimeoutMilliseconds = 60000;// Timeout.Infinite;

    // todo: options to override interval & timeout
    public static MeterProviderBuilder AddDatadogExporter(
        this MeterProviderBuilder builder)
    {
        return builder.AddReader(sp =>
        {
            var metricReaderOptions = new MetricReaderOptions();

            return BuildDatadogExporterMetricReader(metricReaderOptions);
        });
    }

    private static MetricReader BuildDatadogExporterMetricReader(
        //??? exporterOptions,
        MetricReaderOptions metricReaderOptions)
    {
        var metricExporter = new DatadogMetricExporter();

        var metricReader = new PeriodicExportingMetricReader(
            metricExporter, 
            DefaultExportIntervalMilliseconds, 
            DefaultExportTimeoutMilliseconds)
        {
            TemporalityPreference = metricReaderOptions.TemporalityPreference,
        };

        return metricReader;
    }
}
