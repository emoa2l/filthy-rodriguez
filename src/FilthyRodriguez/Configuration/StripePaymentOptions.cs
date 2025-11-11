namespace FilthyRodriguez.Configuration;

/// <summary>
/// Configuration options for the Stripe Payment Plugin
/// </summary>
public class StripePaymentOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "FilthyRodriguez";
    
    /// <summary>
    /// Stripe API key from your Stripe dashboard
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Stripe webhook signing secret from your Stripe dashboard
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional URL to redirect after successful payment
    /// </summary>
    public string? SuccessUrl { get; set; }
    
    /// <summary>
    /// Optional URL to redirect after cancelled payment
    /// </summary>
    public string? CancelUrl { get; set; }

    /// <summary>
    /// Webhook notification configuration
    /// </summary>
    public WebhookNotificationOptions WebhookNotifications { get; set; } = new();

    /// <summary>
    /// Database configuration for optional Entity Framework persistence
    /// </summary>
    public DatabaseOptions? Database { get; set; }
}

/// <summary>
/// Configuration options for webhook notifications
/// </summary>
public class WebhookNotificationOptions
{
    /// <summary>
    /// Whether webhook notifications are enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to continue processing if a callback fails (default: true)
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Maximum time in seconds for callback execution (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to retry failed callbacks (default: false)
    /// </summary>
    public bool RetryFailedCallbacks { get; set; } = false;

    /// <summary>
    /// Maximum number of retries for failed callbacks (default: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
