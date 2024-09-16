using System.Text;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Exporter.DataDogCustom;

/// <summary>
/// temp hacky solution!
/// </summary>
public class DatadogMetricExporter : BaseExporter<Metric>
{
    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime
    private static readonly Dictionary<string, string> MetricNamesTransformation = new()
    {
        { "process.runtime.dotnet.gc.allocations.size", "gc.allocations.size.bytes" },
        { "process.runtime.dotnet.gc.duration", "gc.duration.ns" },
        { "process.runtime.dotnet.gc.heap.size", "gc.heap.size.bytes" },
        { "process.runtime.dotnet.monitor.lock_contention.count", "lock_contention" },
        { "process.runtime.dotnet.thread_pool.threads.count", "thread_pool.threads" },
        { "process.runtime.dotnet.thread_pool.completed_items.count", "thread_pool.completed_items" },
        { "process.runtime.dotnet.thread_pool.queue.length", "thread_pool.queue" },
        { "process.runtime.dotnet.exceptions.count", "exceptions" },
    };

    private static readonly HashSet<string> CummulativeMetrics = new()
    {
        "process.runtime.dotnet.gc.allocations.size",
        "process.runtime.dotnet.gc.duration",
        "process.runtime.dotnet.thread_pool.completed_items.count",
        "process.runtime.dotnet.monitor.lock_contention.count",
        "process.runtime.dotnet.exceptions.count",
    };

    private readonly Dictionary<string, long> lastValuesForCummulative = new();

    public override ExportResult Export(in Batch<Metric> batch)
    {
        Console.WriteLine("Exporting metrics test =================================");
        foreach (var metric in batch)
        {
            if (MetricNamesTransformation.TryGetValue(metric.Name, out var displayName))
            {
                WriteMetricValue(metric, displayName);
            }
        }

        return ExportResult.Success;
    }

    private void WriteMetricValue(Metric metric, string displayName)
    {
        var name = metric.Name;
        var isCummulative = CummulativeMetrics.Contains(name);

        foreach (ref readonly var metricPoint in metric.GetMetricPoints())
        {
            var metricType = metric.MetricType;
            long currentValue = 0L;
            if (metricType.IsLong())
            {
                if (metricType.IsSum())
                {
                    currentValue = metricPoint.GetSumLong();

                    if (isCummulative)
                    {
                        if (lastValuesForCummulative.TryGetValue(name, out var lastValue))
                        {
                            lastValuesForCummulative[name] = currentValue;
                            currentValue -= lastValue;
                        }
                        else
                        {
                            lastValuesForCummulative[name] = currentValue;
                        }
                    }
                }
                else
                {
                    currentValue = metricPoint.GetGaugeLastValueLong();
                }
            }

            StringBuilder tagsBuilder = new StringBuilder();
            foreach (var tag in metricPoint.Tags)
            {
                if (tag.Value == null)
                {
                    continue;
                }
                tagsBuilder.Append($"{tag.Key}: {tag.Value};");
                tagsBuilder.Append(' ');
            }

            Console.WriteLine($"todo: send to datadog [{displayName}]: {currentValue}. tags: [{tagsBuilder}]");
        }
    }
}
