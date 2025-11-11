using Stripe;

namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Event arguments for general Stripe webhook events
/// </summary>
public class StripeWebhookEventArgs : EventArgs
{
    /// <summary>
    /// The Stripe webhook event
    /// </summary>
    public required Event Event { get; init; }
    
    /// <summary>
    /// Timestamp when the webhook was received
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
