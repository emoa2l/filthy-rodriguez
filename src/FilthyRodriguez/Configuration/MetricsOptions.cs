namespace FilthyRodriguez.Configuration;

/// <summary>
/// Configuration options for metrics emission
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Enable or disable metrics emission
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Prefix to add to all metric names
    /// </summary>
    public string Prefix { get; set; } = "stripe.payment";
    
    /// <summary>
    /// Include detailed tags (customer email, payment method type, etc.)
    /// </summary>
    public bool IncludeDetailedTags { get; set; } = false;
    
    /// <summary>
    /// Log metrics to console/logger
    /// </summary>
    public bool LogMetrics { get; set; } = false;
}
