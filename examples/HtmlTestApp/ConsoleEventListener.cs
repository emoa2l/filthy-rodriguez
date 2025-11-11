using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Models;

namespace HtmlTestApp;

public class ConsoleEventListener : IPaymentEventListener
{
    private readonly ILogger<ConsoleEventListener> _logger;

    public ConsoleEventListener(ILogger<ConsoleEventListener> logger)
    {
        _logger = logger;
    }

    public Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🆕 PAYMENT CREATED: PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}",
            eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status);
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ PAYMENT CONFIRMED: PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}",
            eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status);
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnPaymentFailedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("❌ PAYMENT FAILED: PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}",
            eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status);
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnPaymentCanceledAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚫 PAYMENT CANCELED: PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}",
            eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status);
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnRefundInitiatedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💰 REFUND INITIATED: RefundId={RefundId}, PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}, Reason={Reason}",
            eventData.RefundId, eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status, eventData.Reason ?? "N/A");
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ REFUND SUCCEEDED: RefundId={RefundId}, PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}",
            eventData.RefundId, eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status);
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnRefundFailedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("❌ REFUND FAILED: RefundId={RefundId}, PaymentIntentId={PaymentIntentId}, Amount={Amount} {Currency}, Status={Status}, Reason={Reason}",
            eventData.RefundId, eventData.PaymentIntentId, eventData.Amount, eventData.Currency.ToUpper(), eventData.Status, eventData.Reason ?? "N/A");
        LogMetadata(eventData.Metadata);
        return Task.CompletedTask;
    }

    public Task OnDatabaseRecordCreatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💾 DATABASE RECORD CREATED: PaymentIntentId={PaymentIntentId}, OperationType={OperationType}, Id={Id}",
            eventData.PaymentIntentId, eventData.OperationType, eventData.Record.Id);
        if (!string.IsNullOrEmpty(eventData.SqlStatement))
        {
            _logger.LogDebug("SQL: {SqlStatement}", eventData.SqlStatement);
        }
        return Task.CompletedTask;
    }

    public Task OnDatabaseRecordUpdatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💾 DATABASE RECORD UPDATED: PaymentIntentId={PaymentIntentId}, OperationType={OperationType}, Id={Id}, Status={Status}",
            eventData.PaymentIntentId, eventData.OperationType, eventData.Record.Id, eventData.Record.Status);
        if (!string.IsNullOrEmpty(eventData.SqlStatement))
        {
            _logger.LogDebug("SQL: {SqlStatement}", eventData.SqlStatement);
        }
        return Task.CompletedTask;
    }

    private void LogMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata != null && metadata.Count > 0)
        {
            _logger.LogDebug("   Metadata: {Metadata}", string.Join(", ", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
    }
}
