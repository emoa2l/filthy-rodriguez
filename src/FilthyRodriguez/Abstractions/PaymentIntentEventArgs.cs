using Stripe;

namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Event arguments for payment intent webhook events
/// </summary>
public class PaymentIntentEventArgs : EventArgs
{
    /// <summary>
    /// The payment intent from the webhook
    /// </summary>
    public required PaymentIntent PaymentIntent { get; init; }
    
    /// <summary>
    /// The full Stripe webhook event
    /// </summary>
    public required Event WebhookEvent { get; init; }
    
    /// <summary>
    /// Timestamp when the webhook was received
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
