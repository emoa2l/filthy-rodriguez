namespace FilthyRodriguez.Models;

/// <summary>
/// Base event data for all payment events
/// </summary>
public class PaymentEventData
{
    /// <summary>
    /// Stripe Payment Intent ID
    /// </summary>
    public required string PaymentIntentId { get; init; }
    
    /// <summary>
    /// Amount in cents
    /// </summary>
    public required long Amount { get; init; }
    
    /// <summary>
    /// Currency code (e.g., "usd")
    /// </summary>
    public required string Currency { get; init; }
    
    /// <summary>
    /// Current payment status
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Custom metadata attached to the payment
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
    
    /// <summary>
    /// Database transaction entity (if database is enabled)
    /// </summary>
    public TransactionEntity? DatabaseRecord { get; init; }
}

/// <summary>
/// Event data for refund operations
/// </summary>
public class RefundEventData
{
    /// <summary>
    /// Stripe Payment Intent ID being refunded
    /// </summary>
    public required string PaymentIntentId { get; init; }
    
    /// <summary>
    /// Stripe Refund ID
    /// </summary>
    public required string RefundId { get; init; }
    
    /// <summary>
    /// Refund amount in cents
    /// </summary>
    public required long Amount { get; init; }
    
    /// <summary>
    /// Currency code (e.g., "usd")
    /// </summary>
    public required string Currency { get; init; }
    
    /// <summary>
    /// Refund status
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; init; }
    
    /// <summary>
    /// Timestamp when the refund was initiated
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Custom metadata attached to the refund
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
    
    /// <summary>
    /// Database transaction entity (if database is enabled)
    /// </summary>
    public TransactionEntity? DatabaseRecord { get; init; }
}

/// <summary>
/// Event data for database operations
/// </summary>
public class DatabaseEventData
{
    /// <summary>
    /// Stripe Payment Intent ID
    /// </summary>
    public required string PaymentIntentId { get; init; }
    
    /// <summary>
    /// Type of database operation (Insert, Update, Delete)
    /// </summary>
    public required string OperationType { get; init; }
    
    /// <summary>
    /// Database transaction entity
    /// </summary>
    public required TransactionEntity Record { get; init; }
    
    /// <summary>
    /// Timestamp when the database operation occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// SQL statement executed (if available)
    /// </summary>
    public string? SqlStatement { get; init; }
}
