using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Models;
using Microsoft.Extensions.Logging;

namespace FilthyRodriguez.Services;

/// <summary>
/// Service for raising payment events to registered listeners
/// </summary>
public class PaymentEventPublisher
{
    private readonly IEnumerable<IPaymentEventListener> _listeners;
    private readonly ILogger<PaymentEventPublisher> _logger;

    public PaymentEventPublisher(
        IEnumerable<IPaymentEventListener> listeners,
        ILogger<PaymentEventPublisher> logger)
    {
        _listeners = listeners;
        _logger = logger;
    }

    public async Task PublishPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing PaymentCreated event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnPaymentCreatedAsync(eventData, cancellationToken),
            "PaymentCreated",
            eventData.PaymentIntentId);
    }

    public async Task PublishPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing PaymentConfirmed event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnPaymentConfirmedAsync(eventData, cancellationToken),
            "PaymentConfirmed",
            eventData.PaymentIntentId);
    }

    public async Task PublishPaymentFailedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Publishing PaymentFailed event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnPaymentFailedAsync(eventData, cancellationToken),
            "PaymentFailed",
            eventData.PaymentIntentId);
    }

    public async Task PublishPaymentCanceledAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing PaymentCanceled event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnPaymentCanceledAsync(eventData, cancellationToken),
            "PaymentCanceled",
            eventData.PaymentIntentId);
    }

    public async Task PublishRefundInitiatedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing RefundInitiated event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnRefundInitiatedAsync(eventData, cancellationToken),
            "RefundInitiated",
            eventData.PaymentIntentId);
    }

    public async Task PublishRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing RefundSucceeded event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnRefundSucceededAsync(eventData, cancellationToken),
            "RefundSucceeded",
            eventData.PaymentIntentId);
    }

    public async Task PublishRefundFailedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Publishing RefundFailed event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnRefundFailedAsync(eventData, cancellationToken),
            "RefundFailed",
            eventData.PaymentIntentId);
    }

    public async Task PublishDatabaseRecordCreatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing DatabaseRecordCreated event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnDatabaseRecordCreatedAsync(eventData, cancellationToken),
            "DatabaseRecordCreated",
            eventData.PaymentIntentId);
    }

    public async Task PublishDatabaseRecordUpdatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing DatabaseRecordUpdated event for {PaymentIntentId}", eventData.PaymentIntentId);
        await PublishToListenersAsync(
            listener => listener.OnDatabaseRecordUpdatedAsync(eventData, cancellationToken),
            "DatabaseRecordUpdated",
            eventData.PaymentIntentId);
    }

    private async Task PublishToListenersAsync(
        Func<IPaymentEventListener, Task> publishAction,
        string eventName,
        string paymentIntentId)
    {
        var listeners = _listeners.ToList();
        
        if (!listeners.Any())
        {
            _logger.LogTrace("No event listeners registered for {EventName}", eventName);
            return;
        }

        _logger.LogDebug("Publishing {EventName} to {ListenerCount} listener(s)", eventName, listeners.Count);

        var tasks = listeners.Select(async listener =>
        {
            try
            {
                await publishAction(listener);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in event listener {ListenerType} handling {EventName} for {PaymentIntentId}",
                    listener.GetType().Name, eventName, paymentIntentId);
            }
        });

        await Task.WhenAll(tasks);
    }
}
