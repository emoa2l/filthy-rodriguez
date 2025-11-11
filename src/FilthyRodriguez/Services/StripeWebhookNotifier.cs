using Microsoft.Extensions.Logging;
using Stripe;
using FilthyRodriguez.Abstractions;

namespace FilthyRodriguez.Services;

/// <summary>
/// Implementation of IStripeWebhookNotifier that raises events for webhook notifications
/// </summary>
public class StripeWebhookNotifier : IStripeWebhookNotifier
{
    private readonly ILogger<StripeWebhookNotifier> _logger;

    public StripeWebhookNotifier(ILogger<StripeWebhookNotifier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<PaymentIntentEventArgs>? PaymentIntentCreated;
    
    /// <inheritdoc />
    public event EventHandler<PaymentIntentEventArgs>? PaymentIntentSucceeded;
    
    /// <inheritdoc />
    public event EventHandler<PaymentIntentEventArgs>? PaymentIntentFailed;
    
    /// <inheritdoc />
    public event EventHandler<PaymentIntentEventArgs>? PaymentIntentCanceled;
    
    /// <inheritdoc />
    public event EventHandler<PaymentIntentEventArgs>? PaymentIntentProcessing;
    
    /// <inheritdoc />
    public event EventHandler<StripeWebhookEventArgs>? WebhookReceived;

    /// <summary>
    /// Raises the appropriate events based on the webhook event type
    /// </summary>
    /// <param name="stripeEvent">The Stripe webhook event</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task NotifyAsync(Event stripeEvent)
    {
        var receivedAt = DateTime.UtcNow;
        
        // Always raise WebhookReceived for any event
        await RaiseEventAsync(WebhookReceived, new StripeWebhookEventArgs
        {
            Event = stripeEvent,
            ReceivedAt = receivedAt
        });

        // Raise specific payment intent events
        if (stripeEvent.Type.StartsWith("payment_intent.") && stripeEvent.Data.Object is PaymentIntent paymentIntent)
        {
            var args = new PaymentIntentEventArgs
            {
                PaymentIntent = paymentIntent,
                WebhookEvent = stripeEvent,
                ReceivedAt = receivedAt
            };

            switch (stripeEvent.Type)
            {
                case "payment_intent.created":
                    await RaiseEventAsync(PaymentIntentCreated, args);
                    break;
                case "payment_intent.succeeded":
                    await RaiseEventAsync(PaymentIntentSucceeded, args);
                    break;
                case "payment_intent.payment_failed":
                    await RaiseEventAsync(PaymentIntentFailed, args);
                    break;
                case "payment_intent.canceled":
                    await RaiseEventAsync(PaymentIntentCanceled, args);
                    break;
                case "payment_intent.processing":
                    await RaiseEventAsync(PaymentIntentProcessing, args);
                    break;
            }
        }
    }

    private async Task RaiseEventAsync<TEventArgs>(EventHandler<TEventArgs>? eventHandler, TEventArgs args) 
        where TEventArgs : EventArgs
    {
        if (eventHandler == null)
        {
            return;
        }

        var invocationList = eventHandler.GetInvocationList();
        
        foreach (var handler in invocationList)
        {
            try
            {
                var eventHandlerDelegate = (EventHandler<TEventArgs>)handler;
                eventHandlerDelegate.Invoke(this, args);
                
                // Give async void handlers a chance to start
                // Note: async void handlers are fire-and-forget by design in .NET
                // This is a limitation of the EventHandler<T> pattern
                await Task.Yield();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking webhook event handler for event type {EventType}", 
                    typeof(TEventArgs).Name);
                // Continue processing other handlers even if one fails
            }
        }
    }
}
