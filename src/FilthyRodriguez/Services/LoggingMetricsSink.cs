using Microsoft.Extensions.Logging;
using FilthyRodriguez.Abstractions;

namespace FilthyRodriguez.Services;

/// <summary>
/// Console/logging sink for metrics (default implementation)
/// </summary>
public class LoggingMetricsSink : IMetricsSink
{
    private readonly ILogger<LoggingMetricsSink> _logger;

    public LoggingMetricsSink(ILogger<LoggingMetricsSink> logger)
    {
        _logger = logger;
    }

    public Task EmitCounterAsync(string metricName, long value, IDictionary<string, string>? tags, CancellationToken cancellationToken = default)
    {
        var tagsStr = FormatTags(tags);
        _logger.LogInformation("📊 METRIC [Counter] {MetricName}={Value}{Tags}", metricName, value, tagsStr);
        return Task.CompletedTask;
    }

    public Task EmitGaugeAsync(string metricName, double value, IDictionary<string, string>? tags, CancellationToken cancellationToken = default)
    {
        var tagsStr = FormatTags(tags);
        _logger.LogInformation("📊 METRIC [Gauge] {MetricName}={Value}{Tags}", metricName, value, tagsStr);
        return Task.CompletedTask;
    }

    public Task EmitHistogramAsync(string metricName, double value, IDictionary<string, string>? tags, CancellationToken cancellationToken = default)
    {
        var tagsStr = FormatTags(tags);
        _logger.LogInformation("📊 METRIC [Histogram] {MetricName}={Value}{Tags}", metricName, value, tagsStr);
        return Task.CompletedTask;
    }

    private string FormatTags(IDictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return string.Empty;
            
        var tagsList = string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $" [{tagsList}]";
    }
}
