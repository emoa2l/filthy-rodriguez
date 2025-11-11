namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Interface for emitting payment metrics
/// </summary>
public interface IPaymentMetrics
{
    /// <summary>
    /// Increment a counter metric
    /// </summary>
    void IncrementCounter(string metricName, long value = 1, IDictionary<string, string>? tags = null);
    
    /// <summary>
    /// Record a gauge value (point-in-time measurement)
    /// </summary>
    void RecordGauge(string metricName, double value, IDictionary<string, string>? tags = null);
    
    /// <summary>
    /// Record a histogram/timing value
    /// </summary>
    void RecordHistogram(string metricName, double value, IDictionary<string, string>? tags = null);
    
    /// <summary>
    /// Record an amount in cents (for payment/refund amounts)
    /// </summary>
    void RecordAmount(string metricName, long amountCents, string currency, IDictionary<string, string>? tags = null);
}
