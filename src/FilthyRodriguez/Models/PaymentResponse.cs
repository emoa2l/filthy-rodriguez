namespace FilthyRodriguez.Models;

public class PaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public DateTime CreatedAt { get; set; }
}
