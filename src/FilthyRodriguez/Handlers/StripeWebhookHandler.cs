using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Services;
using FilthyRodriguez.Models;
using System.Collections.Concurrent;

namespace FilthyRodriguez.Handlers;

public class StripeWebhookHandler
{
    private readonly StripePaymentOptions _options;
    private readonly IStripeWebhookNotifier _notifier;
    private readonly IEnumerable<IStripeWebhookHandler> _handlers;
    private readonly Func<PaymentIntent, Event, Task>? _callback;
    private readonly ITransactionRepository _transactionRepository;
    private readonly PaymentEventPublisher _eventPublisher;
    private readonly ILogger<StripeWebhookHandler> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private static readonly ConcurrentDictionary<string, Event> _webhookEvents = new();

    public StripeWebhookHandler(
        IOptions<StripePaymentOptions> options,
        IStripeWebhookNotifier notifier,
        IEnumerable<IStripeWebhookHandler> handlers,
        ITransactionRepository transactionRepository,
        ILogger<StripeWebhookHandler> logger,
        ILoggerFactory loggerFactory,
        PaymentEventPublisher? eventPublisher = null,
        Func<PaymentIntent, Event, Task>? callback = null)
    {
        _options = options.Value;
        _notifier = notifier;
        _handlers = handlers;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _eventPublisher = eventPublisher ?? new PaymentEventPublisher(
            Array.Empty<IPaymentEventListener>(), 
            loggerFactory.CreateLogger<PaymentEventPublisher>());
        _callback = callback;
    }

    public async Task<Event?> HandleWebhookAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        // Sanitize Content-Type to prevent log forging (remove newlines and control characters)
        var contentType = request.ContentType?.Replace("\n", "").Replace("\r", "") ?? "unknown";
        _logger.LogInformation("Processing webhook request. Content-Type: {ContentType}, Content-Length: {ContentLength}", 
            contentType, request.ContentLength);
            
        var json = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                request.Headers["Stripe-Signature"],
                _options.WebhookSecret
            );

            _logger.LogInformation("Webhook event received. EventId: {EventId}, EventType: {EventType}", 
                stripeEvent.Id, stripeEvent.Type);

            // Extract payment intent ID if available
            string? paymentIntentId = null;
            if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
            {
                paymentIntentId = paymentIntent.Id;
                _logger.LogInformation("Webhook event contains PaymentIntent. PaymentIntentId: {PaymentIntentId}, Status: {Status}", 
                    paymentIntent.Id, paymentIntent.Status);
            }

            // Store the event for retrieval by WebSocket clients
            _webhookEvents.TryAdd(stripeEvent.Id, stripeEvent);

            // Optionally clean up old events (keep last 100)
            if (_webhookEvents.Count > 100)
            {
                var oldestKey = _webhookEvents.Keys.First();
                _webhookEvents.TryRemove(oldestKey, out _);
                _logger.LogDebug("Cleaned up old webhook event. EventId: {EventId}", oldestKey);
            }

            // Update transaction repository if it's a payment intent event
            if (stripeEvent.Type.StartsWith("payment_intent."))
            {
                var pi = stripeEvent.Data.Object as PaymentIntent;
                if (pi != null)
                {
                    await UpdateTransactionFromWebhookAsync(pi, cancellationToken);
                }
            }

            // Notify via the notifier and handlers if enabled
            if (_options.WebhookNotifications.Enabled)
            {
                await NotifyWebhookAsync(stripeEvent);
            }

            return stripeEvent;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook signature received. This may indicate a security issue.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing webhook event");
            return null;
        }
    }

    private async Task UpdateTransactionFromWebhookAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByStripePaymentIntentIdAsync(paymentIntent.Id, cancellationToken);
        
        if (transaction != null)
        {
            // Update existing transaction
            transaction.Status = paymentIntent.Status;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            // Publish database updated event
            await _eventPublisher.PublishDatabaseRecordUpdatedAsync(new DatabaseEventData
            {
                PaymentIntentId = paymentIntent.Id,
                OperationType = "Update",
                Record = transaction
            }, cancellationToken);

            // Publish payment status events
            var eventData = new PaymentEventData
            {
                PaymentIntentId = paymentIntent.Id,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                Status = paymentIntent.Status,
                Metadata = paymentIntent.Metadata,
                DatabaseRecord = transaction
            };

            switch (paymentIntent.Status)
            {
                case "succeeded":
                    await _eventPublisher.PublishPaymentConfirmedAsync(eventData, cancellationToken);
                    break;
                case "canceled":
                    await _eventPublisher.PublishPaymentCanceledAsync(eventData, cancellationToken);
                    break;
                case "payment_failed":
                case "requires_payment_method":
                    await _eventPublisher.PublishPaymentFailedAsync(eventData, cancellationToken);
                    break;
            }
        }
    }

    private async Task NotifyWebhookAsync(Event stripeEvent)
    {
        try
        {
            // Notify via EventHandler pattern
            if (_notifier is StripeWebhookNotifier notifier)
            {
                await notifier.NotifyAsync(stripeEvent);
            }

            // Extract payment intent if this is a payment_intent event
            PaymentIntent? paymentIntent = null;
            if (stripeEvent.Type.StartsWith("payment_intent.") && stripeEvent.Data.Object is PaymentIntent pi)
            {
                paymentIntent = pi;
            }

            // Invoke callback if registered
            if (_callback != null && paymentIntent != null)
            {
                try
                {
                    await _callback(paymentIntent, stripeEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking webhook callback for payment intent {PaymentIntentId}", 
                        paymentIntent.Id);
                    
                    if (!_options.WebhookNotifications.ContinueOnError)
                    {
                        throw;
                    }
                }
            }

            // Call all registered handlers
            foreach (var handler in _handlers)
            {
                try
                {
                    // Call the general webhook handler
                    await handler.HandleWebhookAsync(stripeEvent);

                    // Call specific handlers based on event type
                    if (paymentIntent != null)
                    {
                        switch (stripeEvent.Type)
                        {
                            case "payment_intent.created":
                                await handler.HandlePaymentIntentCreatedAsync(paymentIntent, stripeEvent);
                                break;
                            case "payment_intent.succeeded":
                                await handler.HandlePaymentIntentSucceededAsync(paymentIntent, stripeEvent);
                                break;
                            case "payment_intent.payment_failed":
                                await handler.HandlePaymentIntentFailedAsync(paymentIntent, stripeEvent);
                                break;
                            case "payment_intent.canceled":
                                await handler.HandlePaymentIntentCanceledAsync(paymentIntent, stripeEvent);
                                break;
                            case "payment_intent.processing":
                                await handler.HandlePaymentIntentProcessingAsync(paymentIntent, stripeEvent);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking webhook handler {HandlerType} for event {EventType}", 
                        handler.GetType().Name, stripeEvent.Type);
                    
                    if (!_options.WebhookNotifications.ContinueOnError)
                    {
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook notifications for event {EventId}", stripeEvent.Id);
            
            if (!_options.WebhookNotifications.ContinueOnError)
            {
                throw;
            }
        }
    }

    public static Event? GetEvent(string eventId)
    {
        _webhookEvents.TryGetValue(eventId, out var stripeEvent);
        return stripeEvent;
    }

    public static IEnumerable<Event> GetRecentEvents(int count = 10)
    {
        return _webhookEvents.Values.TakeLast(count);
    }
}
