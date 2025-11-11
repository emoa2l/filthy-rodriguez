namespace FilthyRodriguez.Models;

public class PaymentRequest
{
    public long Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
