namespace FilthyRodriguez.Models;

/// <summary>
/// Represents a payment transaction entity for persistence
/// </summary>
public class TransactionEntity
{
    /// <summary>
    /// Unique transaction identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Stripe Payment Intent ID
    /// </summary>
    public string StripePaymentIntentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current payment status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount in smallest currency unit (cents)
    /// </summary>
    public long Amount { get; set; }
    
    /// <summary>
    /// Currency code (e.g., "usd", "eur")
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// Client secret for payment confirmation
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// JSON-serialized metadata
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Transaction creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Transaction last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    // Extended fields (optional - captured when DatabaseOptions.CaptureExtendedData = true)
    
    /// <summary>
    /// Stripe Customer ID
    /// </summary>
    public string? CustomerId { get; set; }
    
    /// <summary>
    /// Customer email address
    /// </summary>
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Payment method ID used for this transaction
    /// </summary>
    public string? PaymentMethodId { get; set; }
    
    /// <summary>
    /// Payment method type (card, bank_account, etc.)
    /// </summary>
    public string? PaymentMethodType { get; set; }
    
    /// <summary>
    /// Last 4 digits of card (if payment method is card)
    /// </summary>
    public string? CardLast4 { get; set; }
    
    /// <summary>
    /// Card brand (visa, mastercard, amex, etc.)
    /// </summary>
    public string? CardBrand { get; set; }
    
    /// <summary>
    /// Transaction description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Email address for receipt
    /// </summary>
    public string? ReceiptEmail { get; set; }
    
    /// <summary>
    /// Amount actually captured (may differ from authorized amount)
    /// </summary>
    public long? CapturedAmount { get; set; }
    
    /// <summary>
    /// Total amount refunded
    /// </summary>
    public long? RefundedAmount { get; set; }
    
    /// <summary>
    /// Application fee amount (for platform/marketplace scenarios)
    /// </summary>
    public long? ApplicationFeeAmount { get; set; }
}
