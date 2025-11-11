using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Models;

namespace FilthyRodriguez.Services;

/// <summary>
/// Automatic metrics collection from payment events
/// </summary>
public class MetricsEventListener : IPaymentEventListener
{
    private readonly IPaymentMetrics _metrics;

    public MetricsEventListener(IPaymentMetrics metrics)
    {
        _metrics = metrics;
    }

    public Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("created.count");
        _metrics.RecordAmount("created.amount", eventData.Amount, eventData.Currency);
        
        var tags = new Dictionary<string, string>
        {
            ["status"] = eventData.Status,
            ["currency"] = eventData.Currency
        };
        _metrics.IncrementCounter("created.by_currency", 1, tags);
        
        return Task.CompletedTask;
    }

    public Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("confirmed.count");
        _metrics.RecordAmount("confirmed.amount", eventData.Amount, eventData.Currency);
        
        var tags = new Dictionary<string, string>
        {
            ["currency"] = eventData.Currency
        };
        _metrics.IncrementCounter("confirmed.by_currency", 1, tags);
        
        return Task.CompletedTask;
    }

    public Task OnPaymentFailedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("failed.count");
        _metrics.RecordAmount("failed.amount", eventData.Amount, eventData.Currency);
        
        var tags = new Dictionary<string, string>
        {
            ["status"] = eventData.Status,
            ["currency"] = eventData.Currency
        };
        _metrics.IncrementCounter("failed.by_status", 1, tags);
        
        return Task.CompletedTask;
    }

    public Task OnPaymentCanceledAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("canceled.count");
        _metrics.RecordAmount("canceled.amount", eventData.Amount, eventData.Currency);
        
        return Task.CompletedTask;
    }

    public Task OnRefundInitiatedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("refund.initiated.count");
        _metrics.RecordAmount("refund.initiated.amount", eventData.Amount, eventData.Currency);
        
        if (!string.IsNullOrEmpty(eventData.Reason))
        {
            var tags = new Dictionary<string, string>
            {
                ["reason"] = eventData.Reason,
                ["currency"] = eventData.Currency
            };
            _metrics.IncrementCounter("refund.initiated.by_reason", 1, tags);
        }
        
        return Task.CompletedTask;
    }

    public Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("refund.succeeded.count");
        _metrics.RecordAmount("refund.succeeded.amount", eventData.Amount, eventData.Currency);
        
        return Task.CompletedTask;
    }

    public Task OnRefundFailedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("refund.failed.count");
        _metrics.RecordAmount("refund.failed.amount", eventData.Amount, eventData.Currency);
        
        return Task.CompletedTask;
    }

    public Task OnDatabaseRecordCreatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("database.record_created.count");
        
        var tags = new Dictionary<string, string>
        {
            ["operation"] = eventData.OperationType
        };
        _metrics.IncrementCounter("database.operations", 1, tags);
        
        return Task.CompletedTask;
    }

    public Task OnDatabaseRecordUpdatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementCounter("database.record_updated.count");
        
        var tags = new Dictionary<string, string>
        {
            ["operation"] = eventData.OperationType,
            ["status"] = eventData.Record.Status
        };
        _metrics.IncrementCounter("database.operations", 1, tags);
        
        return Task.CompletedTask;
    }
}
