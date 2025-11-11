namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Interface for custom metrics sink implementations
/// </summary>
public interface IMetricsSink
{
    /// <summary>
    /// Emit a counter metric
    /// </summary>
    Task EmitCounterAsync(string metricName, long value, IDictionary<string, string>? tags, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Emit a gauge metric
    /// </summary>
    Task EmitGaugeAsync(string metricName, double value, IDictionary<string, string>? tags, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Emit a histogram metric
    /// </summary>
    Task EmitHistogramAsync(string metricName, double value, IDictionary<string, string>? tags, CancellationToken cancellationToken = default);
}
