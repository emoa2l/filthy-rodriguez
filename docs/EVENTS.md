# Payment Event System

## Overview

FilthyRodriguez includes a comprehensive event system that raises events for all payment operations, allowing parent applications to capture and respond to payment lifecycle events in real-time.

## Event Types

### Payment Events
- **PaymentCreated** - Raised when a payment intent is created
- **PaymentConfirmed** - Raised when a payment succeeds
- **PaymentFailed** - Raised when a payment fails
- **PaymentCanceled** - Raised when a payment is canceled

### Refund Events
- **RefundInitiated** - Raised when a refund request is processed
- **RefundSucceeded** - Raised when a refund completes successfully
- **RefundFailed** - Raised when a refund fails

### Database Events (when database is enabled)
- **DatabaseRecordCreated** - Raised when a transaction is saved to the database
- **DatabaseRecordUpdatedAsync** - Raised when a database record is updated

## Implementation

### 1. Create an Event Listener

Implement the `IPaymentEventListener` interface:

```csharp
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Models;

public class PaymentAnalyticsListener : IPaymentEventListener
{
    private readonly ILogger<PaymentAnalyticsListener> _logger;
    private readonly IAnalyticsService _analytics;

    public PaymentAnalyticsListener(
        ILogger<PaymentAnalyticsListener> logger,
        IAnalyticsService analytics)
    {
        _logger = logger;
        _analytics = analytics;
    }

    public async Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment created: {PaymentIntentId} for {Amount} {Currency}",
            eventData.PaymentIntentId, eventData.Amount, eventData.Currency);
        
        await _analytics.TrackEvent("payment_created", new {
            payment_id = eventData.PaymentIntentId,
            amount = eventData.Amount,
            currency = eventData.Currency
        });
    }

    public async Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment confirmed: {PaymentIntentId}", eventData.PaymentIntentId);
        
        // Update order status
        if (eventData.Metadata?.TryGetValue("order_id", out var orderId) == true)
        {
            await _analytics.TrackConversion(orderId, eventData.Amount);
        }
    }

    public async Task OnPaymentFailedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Payment failed: {PaymentIntentId}", eventData.PaymentIntentId);
        await _analytics.TrackEvent("payment_failed", eventData);
    }

    public async Task OnPaymentCanceledAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment canceled: {PaymentIntentId}", eventData.PaymentIntentId);
    }

    public async Task OnRefundInitiatedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refund initiated: {RefundId} for payment {PaymentIntentId}",
            eventData.RefundId, eventData.PaymentIntentId);
    }

    public async Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refund succeeded: {RefundId}", eventData.RefundId);
        await _analytics.TrackEvent("refund_completed", eventData);
    }

    public async Task OnRefundFailedAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Refund failed: {RefundId}", eventData.RefundId);
    }

    public async Task OnDatabaseRecordCreatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Database record created for {PaymentIntentId}", eventData.PaymentIntentId);
    }

    public async Task OnDatabaseRecordUpdatedAsync(DatabaseEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Database record updated for {PaymentIntentId}", eventData.PaymentIntentId);
    }
}
```

### 2. Register the Listener

In `Program.cs`:

```csharp
using FilthyRodriguez.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFilthyRodriguez(builder.Configuration)
    .AddPaymentEventListener<PaymentAnalyticsListener>();
    
// Register additional listeners as needed
builder.Services.AddPaymentEventListener<OrderManagementListener>();
builder.Services.AddPaymentEventListener<EmailNotificationListener>();

var app = builder.Build();
app.MapStripePaymentEndpoints("/api/stripe");
app.Run();
```

## Event Data Models

### PaymentEventData

```csharp
public class PaymentEventData
{
    public string PaymentIntentId { get; init; }      // Stripe Payment Intent ID
    public long Amount { get; init; }                  // Amount in cents
    public string Currency { get; init; }              // Currency code (e.g., "usd")
    public string Status { get; init; }                // Current payment status
    public DateTime Timestamp { get; init; }           // When the event occurred
    public Dictionary<string, string>? Metadata { get; init; }  // Custom metadata
    public TransactionEntity? DatabaseRecord { get; init; }      // Database record (if enabled)
}
```

### RefundEventData

```csharp
public class RefundEventData
{
    public string PaymentIntentId { get; init; }      // Original payment ID
    public string RefundId { get; init; }             // Stripe Refund ID
    public long Amount { get; init; }                  // Refund amount in cents
    public string Currency { get; init; }              // Currency code
    public string Status { get; init; }                // Refund status
    public string? Reason { get; init; }               // Reason for refund
    public DateTime Timestamp { get; init; }           // When refund was initiated
    public Dictionary<string, string>? Metadata { get; init; }
    public TransactionEntity? DatabaseRecord { get; init; }
}
```

### DatabaseEventData

```csharp
public class DatabaseEventData
{
    public string PaymentIntentId { get; init; }
    public string OperationType { get; init; }         // "Insert", "Update", "Delete"
    public TransactionEntity Record { get; init; }     // The database record
    public DateTime Timestamp { get; init; }
    public string? SqlStatement { get; init; }         // SQL statement (if available)
}
```

## Use Cases

### 1. Order Management

```csharp
public class OrderManagementListener : IPaymentEventListener
{
    private readonly IOrderService _orders;

    public async Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
    {
        if (eventData.Metadata?.TryGetValue("order_id", out var orderId) == true)
        {
            await _orders.MarkAsPaid(orderId);
            await _orders.StartFulfillment(orderId);
        }
    }

    public async Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken)
    {
        // Cancel order fulfillment
        var order = await _orders.FindByPaymentIntentId(eventData.PaymentIntentId);
        if (order != null)
        {
            await _orders.CancelOrder(order.Id, "Refunded");
        }
    }
    
    // ... implement other methods
}
```

### 2. Email Notifications

```csharp
public class EmailNotificationListener : IPaymentEventListener
{
    private readonly IEmailService _email;

    public async Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
    {
        if (eventData.DatabaseRecord?.CustomerEmail != null)
        {
            await _email.SendReceiptAsync(
                eventData.DatabaseRecord.CustomerEmail,
                eventData.PaymentIntentId,
                eventData.Amount,
                eventData.Currency);
        }
    }

    public async Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken)
    {
        if (eventData.DatabaseRecord?.CustomerEmail != null)
        {
            await _email.SendRefundConfirmationAsync(
                eventData.DatabaseRecord.CustomerEmail,
                eventData.RefundId,
                eventData.Amount);
        }
    }
    
    // ... implement other methods
}
```

### 3. Analytics and Metrics

```csharp
public class MetricsListener : IPaymentEventListener
{
    private readonly IMetricsCollector _metrics;

    public async Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
    {
        _metrics.Increment("payments.created");
        _metrics.Histogram("payment.amount", eventData.Amount);
    }

    public async Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
    {
        _metrics.Increment("payments.succeeded");
        _metrics.Histogram("payment.confirmed_amount", eventData.Amount);
    }

    public async Task OnPaymentFailedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
    {
        _metrics.Increment("payments.failed");
    }

    public async Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken)
    {
        _metrics.Increment("refunds.succeeded");
        _metrics.Histogram("refund.amount", eventData.Amount);
    }
    
    // ... implement other methods
}
```

### 4. Fraud Detection

```csharp
public class FraudDetectionListener : IPaymentEventListener
{
    private readonly IFraudService _fraud;

    public async Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
    {
        var riskScore = await _fraud.AnalyzePayment(eventData);
        if (riskScore > 0.8)
        {
            // Flag for manual review
            await _fraud.FlagForReview(eventData.PaymentIntentId, riskScore);
        }
    }

    public async Task OnRefundInitiatedAsync(RefundEventData eventData, CancellationToken cancellationToken)
    {
        // Track refund patterns
        await _fraud.RecordRefund(eventData.PaymentIntentId, eventData.Amount);
    }
    
    // ... implement other methods
}
```

## Multiple Listeners

You can register multiple listeners, and they will all be called in parallel:

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .AddPaymentEventListener<OrderManagementListener>()
    .AddPaymentEventListener<EmailNotificationListener>()
    .AddPaymentEventListener<MetricsListener>()
    .AddPaymentEventListener<FraudDetectionListener>();
```

## Error Handling

- Exceptions in listeners are logged but do not stop payment processing
- Each listener runs independently
- Failed listeners don't affect other listeners
- Payment operations complete even if all listeners fail

## Performance

- Events are published asynchronously
- Multiple listeners run in parallel using `Task.WhenAll`
- No impact on payment processing performance
- Listeners should be fast or use background processing for heavy operations

## Accessing Database Records

When database persistence is enabled and `CaptureExtendedData: true`:

```csharp
public async Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken)
{
    if (eventData.DatabaseRecord != null)
    {
        // Access extended data
        var customerEmail = eventData.DatabaseRecord.CustomerEmail;
        var cardBrand = eventData.DatabaseRecord.CardBrand;
        var cardLast4 = eventData.DatabaseRecord.CardLast4;
        
        // Use the data
        await SendCustomReceipt(customerEmail, cardBrand, cardLast4);
    }
}
```

## Testing

### Mock Listener for Testing

```csharp
public class TestPaymentEventListener : IPaymentEventListener
{
    public List<PaymentEventData> PaymentsCreated { get; } = new();
    public List<PaymentEventData> PaymentsConfirmed { get; } = new();
    public List<RefundEventData> RefundsSucceeded { get; } = new();

    public Task OnPaymentCreatedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        PaymentsCreated.Add(eventData);
        return Task.CompletedTask;
    }

    public Task OnPaymentConfirmedAsync(PaymentEventData eventData, CancellationToken cancellationToken = default)
    {
        PaymentsConfirmed.Add(eventData);
        return Task.CompletedTask;
    }

    public Task OnRefundSucceededAsync(RefundEventData eventData, CancellationToken cancellationToken = default)
    {
        RefundsSucceeded.Add(eventData);
        return Task.CompletedTask;
    }
    
    // ... implement other methods
}
```

### Unit Test Example

```csharp
[Fact]
public async Task Payment_Success_Should_Raise_Confirmed_Event()
{
    // Arrange
    var listener = new TestPaymentEventListener();
    services.AddPaymentEventListener<TestPaymentEventListener>();
    
    // Act
    var payment = await _paymentService.CreatePaymentAsync(new PaymentRequest {
        Amount = 1000,
        Currency = "usd"
    });
    
    // ... complete payment
    
    // Assert
    Assert.Single(listener.PaymentsConfirmed);
    Assert.Equal(payment.Id, listener.PaymentsConfirmed[0].PaymentIntentId);
}
```

## Comparison with Webhooks

| Feature | Payment Events | Stripe Webhooks |
|---------|---------------|-----------------|
| Latency | Immediate (in-process) | Network delay |
| Reliability | Synchronous | Async, retries |
| Scope | All operations | Stripe-initiated only |
| Database Access | Includes DB record | Requires lookup |
| Custom Logic | Any C# code | HTTP endpoint |
| Testing | Easy (in-memory) | Requires webhook endpoint |

## Best Practices

1. **Keep Listeners Fast** - Offload heavy work to background jobs
2. **Handle Errors Gracefully** - Don't throw exceptions for non-critical failures
3. **Use Structured Logging** - Log event data for debugging
4. **Separate Concerns** - One listener per responsibility (SRP)
5. **Test Independently** - Unit test each listener in isolation
6. **Monitor Performance** - Track listener execution time

## Related Documentation

- [Webhook Handlers](README.md#webhook-notification-patterns) - Stripe webhook integration
- [Database Integration](DATABASE_INTEGRATION.md) - Database persistence with events
- [Extended Data Capture](EXTENDED_DATA.md) - Additional transaction data in events
