using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Models;
using FilthyRodriguez.Abstractions;
using System.Text.Json;

namespace FilthyRodriguez.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly StripePaymentOptions _options;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly PaymentEventPublisher _eventPublisher;
    private readonly BalanceService _balanceService;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public StripePaymentService(
        IOptions<StripePaymentOptions> options,
        ITransactionRepository transactionRepository,
        ILogger<StripePaymentService> logger,
        ILoggerFactory loggerFactory,
        PaymentEventPublisher? eventPublisher = null)
    {
        _options = options.Value;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _eventPublisher = eventPublisher ?? new PaymentEventPublisher(
            Array.Empty<IPaymentEventListener>(), 
            loggerFactory.CreateLogger<PaymentEventPublisher>());
        StripeConfiguration.ApiKey = _options.ApiKey;
        _paymentIntentService = new PaymentIntentService();
        _balanceService = new BalanceService();
        
        _refundService = new RefundService();
        _balanceService = new BalanceService();

        _logger.LogInformation("StripePaymentService initialized");
    }

    public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating payment intent for amount {Amount} {Currency}", request.Amount, request.Currency);
        
        try
        {
            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                Metadata = request.Metadata,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    // Allow redirects if SuccessUrl is configured, otherwise disable them
                    AllowRedirects = !string.IsNullOrEmpty(_options.SuccessUrl) ? "always" : "never"
                }
            };

            // Note: return_url is set during confirmation, not creation
            var paymentIntent = await _paymentIntentService.CreateAsync(createOptions, cancellationToken: cancellationToken);

            // Persist transaction to repository
            var transaction = new TransactionEntity
            {
                Id = Guid.NewGuid().ToString(),
                StripePaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                ClientSecret = paymentIntent.ClientSecret,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                CreatedAt = paymentIntent.Created,
                UpdatedAt = DateTime.UtcNow
            };

            // Populate extended data if enabled
            if (_options.Database?.CaptureExtendedData == true)
            {
                PopulateExtendedData(transaction, paymentIntent);
            }

            await _transactionRepository.CreateAsync(transaction, cancellationToken);

            // Publish event
            await _eventPublisher.PublishPaymentCreatedAsync(new PaymentEventData
            {
                PaymentIntentId = paymentIntent.Id,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                Status = paymentIntent.Status,
                Metadata = request.Metadata,
                DatabaseRecord = transaction
            }, cancellationToken);

            _logger.LogInformation("Payment intent created successfully. PaymentIntentId: {PaymentIntentId}, Status: {Status}, Amount: {Amount} {Currency}", 
                paymentIntent.Id, paymentIntent.Status, paymentIntent.Amount, paymentIntent.Currency);

            // Emit metrics
            
            var tags = new Dictionary<string, string>
            {
                ["status"] = paymentIntent.Status,
                ["currency"] = paymentIntent.Currency
            };

            return new PaymentResponse
            {
                Id = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                ClientSecret = paymentIntent.ClientSecret,
                CreatedAt = paymentIntent.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Stripe API error while creating payment intent. " +
                "Amount: {Amount} {Currency}, " +
                "HttpStatusCode: {HttpStatusCode}, " +
                "StripeErrorType: {ErrorType}, " +
                "StripeErrorCode: {ErrorCode}, " +
                "StripeErrorParam: {ErrorParam}, " +
                "StripeErrorMessage: {ErrorMessage}", 
                request.Amount, request.Currency, 
                ex.HttpStatusCode, 
                ex.StripeError?.Type,
                ex.StripeError?.Code,
                ex.StripeError?.Param,
                ex.StripeError?.Message);
            
            // Emit error metrics
            var errorTags = new Dictionary<string, string>
            {
                ["error_type"] = ex.StripeError?.Type ?? "unknown",
                ["error_code"] = ex.StripeError?.Code ?? "unknown",
                ["currency"] = request.Currency
            };
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating payment intent. Amount: {Amount} {Currency}", 
                request.Amount, request.Currency);
            throw;
        }
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            throw new ArgumentException("Payment intent ID is required", nameof(paymentIntentId));
        }
        
        // Sanitize payment intent ID to prevent log forging
        var sanitizedId = paymentIntentId.Replace("\n", "").Replace("\r", "");
        
        _logger.LogInformation("Retrieving payment status for PaymentIntentId: {PaymentIntentId}", sanitizedId);
        
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(paymentIntentId, cancellationToken: cancellationToken);

            // Update transaction in repository if it exists
            var transaction = await _transactionRepository.GetByStripePaymentIntentIdAsync(paymentIntentId, cancellationToken);
            if (transaction != null)
            {
                transaction.Status = paymentIntent.Status;
                transaction.UpdatedAt = DateTime.UtcNow;
                
                // Update extended data if enabled
                if (_options.Database?.CaptureExtendedData == true)
                {
                    PopulateExtendedData(transaction, paymentIntent);
                }
                
                await _transactionRepository.UpdateAsync(transaction, cancellationToken);
            }

            _logger.LogInformation("Payment status retrieved. PaymentIntentId: {PaymentIntentId}, Status: {Status}", 
                sanitizedId, paymentIntent.Status);

            return new PaymentStatus
            {
                Id = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                CreatedAt = paymentIntent.Created,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Stripe API error while retrieving payment status. " +
                "PaymentIntentId: {PaymentIntentId}, " +
                "HttpStatusCode: {HttpStatusCode}, " +
                "StripeErrorType: {ErrorType}, " +
                "StripeErrorCode: {ErrorCode}, " +
                "StripeErrorParam: {ErrorParam}, " +
                "StripeErrorMessage: {ErrorMessage}", 
                sanitizedId, 
                ex.HttpStatusCode,
                ex.StripeError?.Type,
                ex.StripeError?.Code,
                ex.StripeError?.Param,
                ex.StripeError?.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving payment status. PaymentIntentId: {PaymentIntentId}", 
                sanitizedId);
            throw;
        }
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check");
        
        var health = new HealthResponse
        {
            Timestamp = DateTime.UtcNow,
            Websockets = "enabled" // WebSockets are always enabled
        };

        // Check if API key is configured
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Health check failed: Stripe API key not configured");
            health.Status = "unhealthy";
            health.Stripe = "not_configured";
            health.Webhooks = string.IsNullOrWhiteSpace(_options.WebhookSecret) ? "not_configured" : "enabled";
            return health;
        }

        // Check webhook configuration
        health.Webhooks = string.IsNullOrWhiteSpace(_options.WebhookSecret) ? "not_configured" : "enabled";

        // Test Stripe API connectivity by retrieving account balance
        try
        {
            await _balanceService.GetAsync(cancellationToken: cancellationToken);
            health.Stripe = "connected";
            health.Status = "healthy";
            
            _logger.LogInformation("Health check completed successfully. Status: {Status}, Stripe: {Stripe}, Webhooks: {Webhooks}, Websockets: {Websockets}",
                health.Status, health.Stripe, health.Webhooks, health.Websockets);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Health check failed due to Stripe API error. " +
                "Status: {Status}, " +
                "Stripe: {Stripe}, " +
                "HttpStatusCode: {HttpStatusCode}, " +
                "StripeErrorType: {ErrorType}, " +
                "StripeErrorCode: {ErrorCode}, " +
                "StripeErrorMessage: {ErrorMessage}",
                "unhealthy", 
                "disconnected", 
                ex.HttpStatusCode,
                ex.StripeError?.Type,
                ex.StripeError?.Code,
                ex.StripeError?.Message);
            health.Stripe = "disconnected";
            health.Status = "unhealthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed due to unexpected error. Status: {Status}, Stripe: {Stripe}",
                "unhealthy", "disconnected");
            health.Stripe = "disconnected";
            health.Status = "unhealthy";
        }

        return health;
    }

    public async Task<RefundResponse> ProcessRefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
        {
            throw new ArgumentException("Payment intent ID is required", nameof(request.PaymentIntentId));
        }

        _logger.LogInformation("Processing refund request for PaymentIntentId: {PaymentIntentId}, Amount: {Amount}",
            request.PaymentIntentId, request.Amount ?? 0);

        try
        {
            // First, retrieve the payment intent to check its status
            _logger.LogDebug("Retrieving payment intent status before refund. PaymentIntentId: {PaymentIntentId}", 
                request.PaymentIntentId);
            
            var paymentIntent = await _paymentIntentService.GetAsync(request.PaymentIntentId, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Payment intent retrieved for refund. PaymentIntentId: {PaymentIntentId}, Status: {Status}, Amount: {Amount}",
                paymentIntent.Id, paymentIntent.Status, paymentIntent.Amount);
            
            // Check if the payment intent has been successfully charged
            if (paymentIntent.Status != "succeeded")
            {
                _logger.LogWarning("Cannot refund payment intent with status '{Status}'. PaymentIntentId: {PaymentIntentId}. " +
                    "Only payment intents with status 'succeeded' can be refunded. Current status indicates: {StatusDescription}",
                    paymentIntent.Status, request.PaymentIntentId, GetPaymentIntentStatusDescription(paymentIntent.Status));
                
                throw new InvalidOperationException(
                    $"Cannot refund payment intent '{request.PaymentIntentId}' with status '{paymentIntent.Status}'. " +
                    $"Only payment intents with status 'succeeded' can be refunded. {GetPaymentIntentStatusDescription(paymentIntent.Status)}");
            }
            
            // Create refund options
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Reason = request.Reason,
                Metadata = request.Metadata
            };

            // If amount specified, use it; otherwise Stripe will refund the full amount
            if (request.Amount.HasValue)
            {
                refundOptions.Amount = request.Amount.Value;
            }

            _logger.LogDebug("Creating refund with Stripe API. PaymentIntentId: {PaymentIntentId}, Amount: {Amount}, Reason: {Reason}",
                request.PaymentIntentId, request.Amount ?? paymentIntent.Amount, request.Reason);

            // Process the refund through Stripe
            var refund = await _refundService.CreateAsync(refundOptions, cancellationToken: cancellationToken);

            // Update transaction in repository if it exists
            var transaction = await _transactionRepository.GetByStripePaymentIntentIdAsync(request.PaymentIntentId, cancellationToken);
            if (transaction != null)
            {
                transaction.Status = $"refunded_{refund.Status}";
                transaction.UpdatedAt = DateTime.UtcNow;
                await _transactionRepository.UpdateAsync(transaction, cancellationToken);

                // Publish database updated event
                await _eventPublisher.PublishDatabaseRecordUpdatedAsync(new DatabaseEventData
                {
                    PaymentIntentId = request.PaymentIntentId,
                    OperationType = "Update",
                    Record = transaction
                }, cancellationToken);
            }

            // Publish refund events
            var refundEventData = new RefundEventData
            {
                PaymentIntentId = request.PaymentIntentId,
                RefundId = refund.Id,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason,
                Metadata = request.Metadata,
                DatabaseRecord = transaction
            };

            if (refund.Status == "succeeded")
            {
                await _eventPublisher.PublishRefundSucceededAsync(refundEventData, cancellationToken);
            }
            else if (refund.Status == "failed")
            {
                await _eventPublisher.PublishRefundFailedAsync(refundEventData, cancellationToken);
            }
            else
            {
                // For pending or processing states
                await _eventPublisher.PublishRefundInitiatedAsync(refundEventData, cancellationToken);
            }

            _logger.LogInformation("Refund processed successfully. RefundId: {RefundId}, PaymentIntentId: {PaymentIntentId}, Status: {Status}, Amount: {Amount}",
                refund.Id, request.PaymentIntentId, refund.Status, refund.Amount);

            // Emit metrics
            
            if (!string.IsNullOrEmpty(refund.Reason))
            {
                var refundTags = new Dictionary<string, string>
                {
                    ["reason"] = refund.Reason,
                    ["currency"] = refund.Currency,
                    ["status"] = refund.Status
                };
            }

            return new RefundResponse
            {
                Id = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                Status = refund.Status,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Reason = refund.Reason,
                CreatedAt = refund.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Stripe API error while processing refund. " +
                "PaymentIntentId: {PaymentIntentId}, " +
                "HttpStatusCode: {HttpStatusCode}, " +
                "StripeErrorType: {ErrorType}, " +
                "StripeErrorCode: {ErrorCode}, " +
                "StripeErrorParam: {ErrorParam}, " +
                "StripeErrorMessage: {ErrorMessage}, " +
                "DeclineCode: {DeclineCode}", 
                request.PaymentIntentId, 
                ex.HttpStatusCode,
                ex.StripeError?.Type,
                ex.StripeError?.Code,
                ex.StripeError?.Param,
                ex.StripeError?.Message,
                ex.StripeError?.DeclineCode);
            
            // Emit error metrics
            var errorTags = new Dictionary<string, string>
            {
                ["error_type"] = ex.StripeError?.Type ?? "unknown",
                ["error_code"] = ex.StripeError?.Code ?? "unknown"
            };
            
            throw new InvalidOperationException($"Failed to process refund: {ex.StripeError?.Message ?? ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing refund. PaymentIntentId: {PaymentIntentId}",
                request.PaymentIntentId);
            throw;
        }
    }

    public async Task<PaymentStatus> ConfirmPaymentAsync(PaymentConfirmRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
        {
            throw new ArgumentException("Payment intent ID is required", nameof(request.PaymentIntentId));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            throw new ArgumentException("Payment method ID is required", nameof(request.PaymentMethodId));
        }

        _logger.LogInformation("Confirming payment intent with test payment method. PaymentIntentId: {PaymentIntentId}, PaymentMethodId: {PaymentMethodId}",
            request.PaymentIntentId, request.PaymentMethodId);

        try
        {
            // Confirm the payment intent with the test payment method token
            var confirmOptions = new PaymentIntentConfirmOptions
            {
                PaymentMethod = request.PaymentMethodId
            };

            // Add return_url if SuccessUrl is configured (required for redirect-based payment methods)
            if (!string.IsNullOrEmpty(_options.SuccessUrl))
            {
                confirmOptions.ReturnUrl = _options.SuccessUrl;
            }

            _logger.LogDebug("Confirming payment intent. PaymentIntentId: {PaymentIntentId}, PaymentMethodId: {PaymentMethodId}",
                request.PaymentIntentId, request.PaymentMethodId);

            var paymentIntent = await _paymentIntentService.ConfirmAsync(
                request.PaymentIntentId,
                confirmOptions,
                cancellationToken: cancellationToken);

            // Update transaction in repository
            var transaction = await _transactionRepository.GetByStripePaymentIntentIdAsync(request.PaymentIntentId, cancellationToken);
            if (transaction != null)
            {
                transaction.Status = paymentIntent.Status;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _transactionRepository.UpdateAsync(transaction, cancellationToken);
            }

            _logger.LogInformation("Payment intent confirmed successfully. PaymentIntentId: {PaymentIntentId}, Status: {Status}",
                paymentIntent.Id, paymentIntent.Status);

            return new PaymentStatus
            {
                Id = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                CreatedAt = paymentIntent.Created,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Stripe API error while confirming payment intent. " +
                "PaymentIntentId: {PaymentIntentId}, " +
                "PaymentMethodId: {PaymentMethodId}, " +
                "HttpStatusCode: {HttpStatusCode}, " +
                "StripeErrorType: {ErrorType}, " +
                "StripeErrorCode: {ErrorCode}, " +
                "StripeErrorParam: {ErrorParam}, " +
                "StripeErrorMessage: {ErrorMessage}",
                request.PaymentIntentId,
                request.PaymentMethodId,
                ex.HttpStatusCode,
                ex.StripeError?.Type,
                ex.StripeError?.Code,
                ex.StripeError?.Param,
                ex.StripeError?.Message);
            throw new InvalidOperationException($"Failed to confirm payment: {ex.StripeError?.Message ?? ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while confirming payment intent. PaymentIntentId: {PaymentIntentId}",
                request.PaymentIntentId);
            throw;
        }
    }
    
    /// <summary>
    /// Gets a human-readable description of the payment intent status
    /// </summary>
    private static string GetPaymentIntentStatusDescription(string status)
    {
        return status switch
        {
            "requires_payment_method" => "The payment intent requires a payment method to be attached.",
            "requires_confirmation" => "The payment intent requires confirmation.",
            "requires_action" => "The payment intent requires additional action (e.g., 3D Secure authentication).",
            "processing" => "The payment is currently being processed.",
            "requires_capture" => "The payment has been authorized but not yet captured.",
            "canceled" => "The payment intent has been canceled.",
            "succeeded" => "The payment has been successfully completed.",
            _ => $"Unknown status: {status}"
        };
    }
    
    /// <summary>
    /// Populates extended transaction data from Stripe PaymentIntent
    /// </summary>
    private void PopulateExtendedData(TransactionEntity transaction, PaymentIntent paymentIntent)
    {
        transaction.CustomerId = paymentIntent.CustomerId;
        transaction.Description = paymentIntent.Description;
        transaction.ReceiptEmail = paymentIntent.ReceiptEmail;
        transaction.CapturedAmount = paymentIntent.AmountCapturable;
        transaction.ApplicationFeeAmount = paymentIntent.ApplicationFeeAmount;
        
        // Calculate refunded amount (amount received minus capturable)
        if (paymentIntent.AmountCapturable > 0)
        {
            transaction.RefundedAmount = paymentIntent.AmountReceived - paymentIntent.AmountCapturable;
        }

        // Extract payment method details if available
        if (paymentIntent.PaymentMethod != null)
        {
            if (paymentIntent.PaymentMethod is PaymentMethod pm)
            {
                transaction.PaymentMethodId = pm.Id;
                transaction.PaymentMethodType = pm.Type;
                
                // Extract card details if payment method is a card
                if (pm.Type == "card" && pm.Card != null)
                {
                    transaction.CardLast4 = pm.Card.Last4;
                    transaction.CardBrand = pm.Card.Brand;
                }
            }
        }
        else if (!string.IsNullOrEmpty(paymentIntent.PaymentMethodId))
        {
            transaction.PaymentMethodId = paymentIntent.PaymentMethodId;
        }

        // Extract customer email from latest charge if available
        if (string.IsNullOrEmpty(transaction.CustomerEmail))
        {
            // Note: Charges data is only populated when expanding the PaymentIntent
            // For now, we'll rely on other sources for customer email
            _logger.LogDebug("Extended data captured for PaymentIntent {PaymentIntentId}", paymentIntent.Id);
        }
    }
}
