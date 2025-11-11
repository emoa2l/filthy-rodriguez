namespace FilthyRodriguez.Models;

/// <summary>
/// Health check response indicating plugin configuration and connectivity status
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status: "healthy" or "unhealthy"
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Stripe API connectivity status: "connected", "disconnected", or "not_configured"
    /// </summary>
    public string Stripe { get; set; } = string.Empty;

    /// <summary>
    /// Webhook configuration status: "enabled", "disabled", or "not_configured"
    /// </summary>
    public string Webhooks { get; set; } = string.Empty;

    /// <summary>
    /// WebSocket configuration status: "enabled"
    /// </summary>
    public string Websockets { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the health check
    /// </summary>
    public DateTime Timestamp { get; set; }
}
