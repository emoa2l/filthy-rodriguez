using Stripe;

namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Notifier for Stripe webhook events using the .NET EventHandler pattern
/// </summary>
public interface IStripeWebhookNotifier
{
    /// <summary>
    /// Raised when a payment_intent.created webhook is received
    /// </summary>
    event EventHandler<PaymentIntentEventArgs>? PaymentIntentCreated;
    
    /// <summary>
    /// Raised when a payment_intent.succeeded webhook is received
    /// </summary>
    event EventHandler<PaymentIntentEventArgs>? PaymentIntentSucceeded;
    
    /// <summary>
    /// Raised when a payment_intent.payment_failed webhook is received
    /// </summary>
    event EventHandler<PaymentIntentEventArgs>? PaymentIntentFailed;
    
    /// <summary>
    /// Raised when a payment_intent.canceled webhook is received
    /// </summary>
    event EventHandler<PaymentIntentEventArgs>? PaymentIntentCanceled;
    
    /// <summary>
    /// Raised when a payment_intent.processing webhook is received
    /// </summary>
    event EventHandler<PaymentIntentEventArgs>? PaymentIntentProcessing;
    
    /// <summary>
    /// Raised when any webhook event is received
    /// </summary>
    event EventHandler<StripeWebhookEventArgs>? WebhookReceived;
}
