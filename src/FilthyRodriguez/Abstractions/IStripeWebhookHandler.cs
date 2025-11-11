using Stripe;

namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Interface for handling Stripe webhook events. Implement this interface to create custom webhook handlers.
/// All methods have default implementations that do nothing, so you only need to implement the ones you care about.
/// </summary>
public interface IStripeWebhookHandler
{
    /// <summary>
    /// Handle a payment_intent.succeeded webhook event
    /// </summary>
    /// <param name="paymentIntent">The payment intent that succeeded</param>
    /// <param name="webhookEvent">The full Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent, Event webhookEvent) => Task.CompletedTask;
    
    /// <summary>
    /// Handle a payment_intent.payment_failed webhook event
    /// </summary>
    /// <param name="paymentIntent">The payment intent that failed</param>
    /// <param name="webhookEvent">The full Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent, Event webhookEvent) => Task.CompletedTask;
    
    /// <summary>
    /// Handle a payment_intent.canceled webhook event
    /// </summary>
    /// <param name="paymentIntent">The payment intent that was canceled</param>
    /// <param name="webhookEvent">The full Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandlePaymentIntentCanceledAsync(PaymentIntent paymentIntent, Event webhookEvent) => Task.CompletedTask;
    
    /// <summary>
    /// Handle a payment_intent.processing webhook event
    /// </summary>
    /// <param name="paymentIntent">The payment intent that is processing</param>
    /// <param name="webhookEvent">The full Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandlePaymentIntentProcessingAsync(PaymentIntent paymentIntent, Event webhookEvent) => Task.CompletedTask;
    
    /// <summary>
    /// Handle a payment_intent.created webhook event
    /// </summary>
    /// <param name="paymentIntent">The payment intent that was created</param>
    /// <param name="webhookEvent">The full Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandlePaymentIntentCreatedAsync(PaymentIntent paymentIntent, Event webhookEvent) => Task.CompletedTask;
    
    /// <summary>
    /// Handle any webhook event (called for all events)
    /// </summary>
    /// <param name="webhookEvent">The Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandleWebhookAsync(Event webhookEvent) => Task.CompletedTask;
}
