namespace FilthyRodriguez.Models;

/// <summary>
/// Request to confirm a payment intent with a test payment method
/// </summary>
public class PaymentConfirmRequest
{
    /// <summary>
    /// Payment intent ID to confirm
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Stripe test payment method ID (e.g., pm_card_visa, pm_card_mastercard).
    /// See https://stripe.com/docs/testing#cards for available test tokens.
    /// Common test tokens:
    /// - pm_card_visa: Visa card (success)
    /// - pm_card_visa_debit: Visa debit card (success)
    /// - pm_card_mastercard: Mastercard (success)
    /// - pm_card_amex: American Express (success)
    /// - pm_card_chargeDeclined: Card will be declined
    /// - pm_card_authenticationRequired: Requires 3D Secure authentication
    /// </summary>
    public string PaymentMethodId { get; set; } = "pm_card_visa";
}
