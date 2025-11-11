using FilthyRodriguez.Models;

namespace FilthyRodriguez.Services;

/// <summary>
/// Service for managing Stripe payments
/// </summary>
public interface IStripePaymentService
{
    /// <summary>
    /// Creates a new payment intent
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response with client secret</returns>
    Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current status of a payment intent
    /// </summary>
    /// <param name="paymentIntentId">Payment intent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current payment status</returns>
    Task<PaymentStatus> GetPaymentStatusAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check to verify plugin configuration and connectivity
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status response</returns>
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a refund for a payment intent
    /// </summary>
    /// <param name="request">Refund request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund response with refund details</returns>
    /// <exception cref="ArgumentException">Thrown when payment intent ID is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when refund cannot be processed</exception>
    Task<RefundResponse> ProcessRefundAsync(RefundRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a payment intent with a test payment method token (for testing only).
    /// Uses Stripe's test payment method tokens (e.g., pm_card_visa) instead of raw card data for security.
    /// See https://stripe.com/docs/testing#cards for available test tokens.
    /// </summary>
    /// <param name="request">Payment confirmation request with test payment method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment status after confirmation</returns>
    /// <exception cref="ArgumentException">Thrown when payment intent ID or payment method ID is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when confirmation fails</exception>
    Task<PaymentStatus> ConfirmPaymentAsync(PaymentConfirmRequest request, CancellationToken cancellationToken = default);
}
