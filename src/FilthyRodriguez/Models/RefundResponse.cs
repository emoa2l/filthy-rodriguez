namespace FilthyRodriguez.Models;

/// <summary>
/// Response model for a refund operation
/// </summary>
public class RefundResponse
{
    /// <summary>
    /// Refund ID from Stripe
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Payment intent ID that was refunded
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Refund status (succeeded, pending, failed, canceled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Amount refunded in cents
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency of the refund
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Timestamp when the refund was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
