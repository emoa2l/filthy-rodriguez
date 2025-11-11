namespace FilthyRodriguez.Abstractions;

/// <summary>
/// Interface for listening to payment events raised by FilthyRodriguez
/// </summary>
public interface IPaymentEventListener
{
    /// <summary>
    /// Called when a payment intent is created
    /// </summary>
    Task OnPaymentCreatedAsync(Models.PaymentEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a payment is confirmed (succeeded)
    /// </summary>
    Task OnPaymentConfirmedAsync(Models.PaymentEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a payment fails
    /// </summary>
    Task OnPaymentFailedAsync(Models.PaymentEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a payment is canceled
    /// </summary>
    Task OnPaymentCanceledAsync(Models.PaymentEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a refund is initiated
    /// </summary>
    Task OnRefundInitiatedAsync(Models.RefundEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a refund succeeds
    /// </summary>
    Task OnRefundSucceededAsync(Models.RefundEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a refund fails
    /// </summary>
    Task OnRefundFailedAsync(Models.RefundEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a transaction is saved to the database
    /// </summary>
    Task OnDatabaseRecordCreatedAsync(Models.DatabaseEventData eventData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a database record is updated
    /// </summary>
    Task OnDatabaseRecordUpdatedAsync(Models.DatabaseEventData eventData, CancellationToken cancellationToken = default);
}
