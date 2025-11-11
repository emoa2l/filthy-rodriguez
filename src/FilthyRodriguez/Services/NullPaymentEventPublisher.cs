using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Models;

namespace FilthyRodriguez.Services;

/// <summary>
/// Null object pattern implementation for PaymentEventPublisher when no listeners are registered
/// </summary>
internal class NullPaymentEventPublisher : IPaymentEventListener
{
    public Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnPaymentFailedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnPaymentCanceledAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnRefundInitiatedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnRefundFailedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnDatabaseRecordCreatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnDatabaseRecordUpdatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
