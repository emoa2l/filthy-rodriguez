using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Configuration;

namespace FilthyRodriguez.Services;

/// <summary>
/// Default implementation of payment metrics emission
/// </summary>
public class PaymentMetricsService : IPaymentMetrics
{
    private readonly ILogger<PaymentMetricsService> _logger;
    private readonly MetricsOptions _options;
    private readonly IEnumerable<IMetricsSink> _sinks;

    public PaymentMetricsService(
        ILogger<PaymentMetricsService> logger,
        IOptions<MetricsOptions> options,
        IEnumerable<IMetricsSink> sinks)
    {
        _logger = logger;
        _options = options.Value;
        _sinks = sinks;
    }

    public void IncrementCounter(string metricName, long value = 1, IDictionary<string, string>? tags = null)
    {
        if (!_options.Enabled) return;

        var fullMetricName = $"{_options.Prefix}.{metricName}";
        
        if (_options.LogMetrics)
        {
            _logger.LogDebug("Counter: {MetricName} += {Value}", fullMetricName, value);
        }

        foreach (var sink in _sinks)
        {
            _ = sink.EmitCounterAsync(fullMetricName, value, tags);
        }
    }

    public void RecordGauge(string metricName, double value, IDictionary<string, string>? tags = null)
    {
        if (!_options.Enabled) return;

        var fullMetricName = $"{_options.Prefix}.{metricName}";
        
        if (_options.LogMetrics)
        {
            _logger.LogDebug("Gauge: {MetricName} = {Value}", fullMetricName, value);
        }

        foreach (var sink in _sinks)
        {
            _ = sink.EmitGaugeAsync(fullMetricName, value, tags);
        }
    }

    public void RecordHistogram(string metricName, double value, IDictionary<string, string>? tags = null)
    {
        if (!_options.Enabled) return;

        var fullMetricName = $"{_options.Prefix}.{metricName}";
        
        if (_options.LogMetrics)
        {
            _logger.LogDebug("Histogram: {MetricName} = {Value}", fullMetricName, value);
        }

        foreach (var sink in _sinks)
        {
            _ = sink.EmitHistogramAsync(fullMetricName, value, tags);
        }
    }

    public void RecordAmount(string metricName, long amountCents, string currency, IDictionary<string, string>? tags = null)
    {
        if (!_options.Enabled) return;

        var enhancedTags = tags != null ? new Dictionary<string, string>(tags) : new Dictionary<string, string>();
        enhancedTags["currency"] = currency.ToLower();

        var fullMetricName = $"{_options.Prefix}.{metricName}";
        
        if (_options.LogMetrics)
        {
            _logger.LogDebug("Amount: {MetricName} = {Amount} {Currency}", fullMetricName, amountCents, currency);
        }

        foreach (var sink in _sinks)
        {
            _ = sink.EmitHistogramAsync(fullMetricName, amountCents, enhancedTags);
        }
    }
}
