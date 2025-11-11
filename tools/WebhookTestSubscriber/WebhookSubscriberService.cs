using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FilthyRodriguez.Abstractions;
using Stripe;

namespace WebhookTestSubscriber;

/// <summary>
/// Background service that subscribes to Stripe webhook events and logs them to console
/// </summary>
public class WebhookSubscriberService : BackgroundService
{
    private readonly IStripeWebhookNotifier _notifier;
    private readonly ILogger<WebhookSubscriberService> _logger;

    public WebhookSubscriberService(
        IStripeWebhookNotifier notifier,
        ILogger<WebhookSubscriberService> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎧 Webhook Test Subscriber started - listening for events...");
        _logger.LogInformation("Press Ctrl+C to stop");
        _logger.LogInformation("");

        // Subscribe to webhook events
        _notifier.WebhookReceived += OnWebhookReceived;

        // Keep the service running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("🛑 Webhook Test Subscriber shutting down...");
        }
        finally
        {
            // Unsubscribe from events
            _notifier.WebhookReceived -= OnWebhookReceived;
        }
    }

    private void OnWebhookReceived(object? sender, StripeWebhookEventArgs e)
    {
        var eventType = e.Event.Type;
        var eventId = e.Event.Id;
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  [{timestamp}] Webhook Event Received");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Event ID:   {eventId}");
        Console.WriteLine($"Event Type: {eventType}");
        Console.ResetColor();

        // Display payment intent details if available
        if (e.Event.Data.Object is PaymentIntent paymentIntent)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("💳 Payment Intent Details:");
            Console.WriteLine($"   ID:       {paymentIntent.Id}");
            Console.WriteLine($"   Status:   {paymentIntent.Status}");
            Console.WriteLine($"   Amount:   {FormatAmount(paymentIntent.Amount, paymentIntent.Currency)}");
            Console.WriteLine($"   Currency: {paymentIntent.Currency.ToUpper()}");
            
            if (!string.IsNullOrEmpty(paymentIntent.Description))
            {
                Console.WriteLine($"   Description: {paymentIntent.Description}");
            }

            if (paymentIntent.Metadata != null && paymentIntent.Metadata.Count > 0)
            {
                Console.WriteLine("   Metadata:");
                foreach (var kvp in paymentIntent.Metadata)
                {
                    Console.WriteLine($"      {kvp.Key}: {kvp.Value}");
                }
            }
            Console.ResetColor();
        }

        // Display refund details if available
        if (e.Event.Data.Object is Refund refund)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine();
            Console.WriteLine("💸 Refund Details:");
            Console.WriteLine($"   ID:              {refund.Id}");
            Console.WriteLine($"   Status:          {refund.Status}");
            Console.WriteLine($"   Amount:          {FormatAmount(refund.Amount, refund.Currency)}");
            Console.WriteLine($"   Currency:        {refund.Currency.ToUpper()}");
            Console.WriteLine($"   Payment Intent:  {refund.PaymentIntentId}");
            
            if (!string.IsNullOrEmpty(refund.Reason))
            {
                Console.WriteLine($"   Reason:          {refund.Reason}");
            }
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Waiting for next event...");
        Console.ResetColor();

        _logger.LogInformation("Processed webhook event {EventId} ({EventType})", eventId, eventType);
    }

    private static string FormatAmount(long amount, string currency)
    {
        // Most currencies use 2 decimal places (cents)
        decimal displayAmount = amount / 100.0m;
        return currency.ToLower() switch
        {
            "usd" => $"${displayAmount:F2}",
            "eur" => $"€{displayAmount:F2}",
            "gbp" => $"£{displayAmount:F2}",
            _ => $"{displayAmount:F2} {currency.ToUpper()}"
        };
    }
}
