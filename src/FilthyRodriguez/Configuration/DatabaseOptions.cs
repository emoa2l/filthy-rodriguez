namespace FilthyRodriguez.Configuration;

/// <summary>
/// Database configuration options for Entity Framework integration
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Whether database persistence is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Database connection string
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Database provider (SqlServer, PostgreSQL, MySQL, SQLite)
    /// </summary>
    public string Provider { get; set; } = "SqlServer";
    
    /// <summary>
    /// Table name for transaction storage
    /// </summary>
    public string TableName { get; set; } = "stripe_transactions";
    
    /// <summary>
    /// Field mapping configuration
    /// </summary>
    public FieldMappingOptions FieldMapping { get; set; } = new();
    
    /// <summary>
    /// Enable extended data capture for more detailed transaction information
    /// </summary>
    public bool CaptureExtendedData { get; set; } = false;
}

/// <summary>
/// Configuration for database field name mapping
/// </summary>
public class FieldMappingOptions
{
    /// <summary>
    /// Field name for transaction ID
    /// </summary>
    public string Id { get; set; } = "transaction_id";
    
    /// <summary>
    /// Field name for Stripe Payment Intent ID
    /// </summary>
    public string StripePaymentIntentId { get; set; } = "stripe_pi_id";
    
    /// <summary>
    /// Field name for payment status
    /// </summary>
    public string Status { get; set; } = "payment_status";
    
    /// <summary>
    /// Field name for amount
    /// </summary>
    public string Amount { get; set; } = "amount_cents";
    
    /// <summary>
    /// Field name for currency
    /// </summary>
    public string Currency { get; set; } = "currency_code";
    
    /// <summary>
    /// Field name for client secret
    /// </summary>
    public string ClientSecret { get; set; } = "client_secret";
    
    /// <summary>
    /// Field name for created timestamp
    /// </summary>
    public string CreatedAt { get; set; } = "created_timestamp";
    
    /// <summary>
    /// Field name for updated timestamp
    /// </summary>
    public string UpdatedAt { get; set; } = "updated_timestamp";
    
    /// <summary>
    /// Field name for metadata JSON
    /// </summary>
    public string Metadata { get; set; } = "metadata_json";
    
    // Extended fields (only used when DatabaseOptions.CaptureExtendedData = true)
    
    /// <summary>
    /// Field name for customer ID
    /// </summary>
    public string CustomerId { get; set; } = "customer_id";
    
    /// <summary>
    /// Field name for customer email
    /// </summary>
    public string CustomerEmail { get; set; } = "customer_email";
    
    /// <summary>
    /// Field name for payment method ID
    /// </summary>
    public string PaymentMethodId { get; set; } = "payment_method_id";
    
    /// <summary>
    /// Field name for payment method type
    /// </summary>
    public string PaymentMethodType { get; set; } = "payment_method_type";
    
    /// <summary>
    /// Field name for card last 4 digits
    /// </summary>
    public string CardLast4 { get; set; } = "card_last4";
    
    /// <summary>
    /// Field name for card brand
    /// </summary>
    public string CardBrand { get; set; } = "card_brand";
    
    /// <summary>
    /// Field name for description
    /// </summary>
    public string Description { get; set; } = "description";
    
    /// <summary>
    /// Field name for receipt email
    /// </summary>
    public string ReceiptEmail { get; set; } = "receipt_email";
    
    /// <summary>
    /// Field name for captured amount
    /// </summary>
    public string CapturedAmount { get; set; } = "captured_amount";
    
    /// <summary>
    /// Field name for refunded amount
    /// </summary>
    public string RefundedAmount { get; set; } = "refunded_amount";
    
    /// <summary>
    /// Field name for application fee amount
    /// </summary>
    public string ApplicationFeeAmount { get; set; } = "application_fee_amount";
}
