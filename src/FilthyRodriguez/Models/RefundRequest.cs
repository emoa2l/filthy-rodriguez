namespace FilthyRodriguez.Models;

/// <summary>
/// Request model for processing a refund
/// </summary>
public class RefundRequest
{
    /// <summary>
    /// Payment intent ID to refund
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount to refund in cents (optional, defaults to full refund)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional metadata for the refund
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
