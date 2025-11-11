# Stripe Webhook Test Subscriber

A simple test tool that helps you verify Stripe webhook integration is working correctly.

## Features

This tool provides **TWO** ways to test webhooks:

1. **Webhook Endpoint** - Exposes an HTTP endpoint at `/api/stripe/webhook` that receives webhook POST requests from Stripe
2. **Event Subscriber** - Displays real-time webhook events in the console with formatted, color-coded output

## Quick Start

### 1. Configure the Tool

Edit `appsettings.json` and add your Stripe API key:

```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_YOUR_ACTUAL_KEY_HERE",
    "WebhookSecret": "whsec_will_be_updated_in_step_4"
  }
}
```

### 2. Start the Tool

```bash
cd tools/WebhookTestSubscriber
dotnet run
```

The tool will start and display instructions.

### 3. Forward Webhooks with Stripe CLI

In a separate terminal, run:

```bash
stripe listen --forward-to http://localhost:5000/api/stripe/webhook
```

### 4. Copy the Webhook Secret

Stripe CLI will display a webhook signing secret like:

```
Your webhook signing secret is whsec_abc123def456...
```

Copy this secret to your `appsettings.json`:

```json
{
  "FilthyRodriguez": {
    "WebhookSecret": "whsec_abc123def456..."
  }
}
```

### 5. Restart the Tool

Stop the tool (Ctrl+C) and start it again:

```bash
dotnet run
```

### 6. Test It!

Trigger a test webhook event:

```bash
stripe trigger payment_intent.succeeded
```

Or create a real payment:

```bash
curl -X POST http://localhost:5000/api/stripe/payment \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 2000,
    "currency": "usd",
    "description": "Test payment"
  }'
```

## What You'll See

When a webhook is received, you'll see formatted output like:

```
╔══════════════════════════════════════════════════════════════╗
║  [14:22:26] Webhook Event Received
╚══════════════════════════════════════════════════════════════╝
Event ID:   evt_3SS6Ht21kiZimfCZ1MO7qQTg
Event Type: payment_intent.succeeded

💳 Payment Intent Details:
   ID:       pi_3SS6QK21kiZimfCZ0eQrd8PE
   Status:   succeeded
   Amount:   $20.00
   Currency: USD
   Description: Test payment
   Metadata:
      order_id: 12345
      customer_email: test@example.com

Waiting for next event...
```

## Event Types Displayed

The subscriber displays detailed information for:

- **Payment Intents**: amount, status, description, metadata
- **Refunds**: amount, status, reason, associated payment intent
- **All Other Events**: event ID and type

## Troubleshooting

### "Webhook request rejected due to invalid signature"

This means the `WebhookSecret` in `appsettings.json` doesn't match what Stripe CLI is using. Make sure you:

1. Copied the complete secret from Stripe CLI (starts with `whsec_`)
2. Restarted the tool after updating the secret
3. The Stripe CLI is still running

### "Connection refused" when using Stripe CLI

Make sure:

1. The tool is running (`dotnet run`)
2. The port matches (default is 5000)
3. No firewall is blocking local connections

### No events appearing in console

Check that:

1. The tool shows "✨ Ready! Waiting for webhook events..."
2. You've triggered an event (`stripe trigger payment_intent.succeeded`)
3. Check the Stripe CLI terminal for any errors

## Configuration

All configuration is in `appsettings.json`:

```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "WebhookNotifications": {
      "Enabled": true,           // Must be true for events to be raised
      "ContinueOnError": true    // Continue processing even if a handler errors
    }
  }
}
```

## Integration with Your Application

This tool demonstrates the **EventHandler pattern** for webhook notifications. You can see similar integration in the HtmlTestApp (`examples/HtmlTestApp/Program.cs`), which demonstrates basic webhook handling.

To integrate this pattern into your own application:

1. Enable webhook notifications:
```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .EnableWebhookNotifications();
```

2. Create a background service that subscribes to events:
```csharp
public class MyWebhookHandler : BackgroundService
{
    private readonly IStripeWebhookNotifier _notifier;
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _notifier.WebhookReceived += OnWebhookReceived;
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private void OnWebhookReceived(object? sender, StripeWebhookEventArgs e)
    {
        // Handle the event
    }
}
```

3. Register your service:
```csharp
builder.Services.AddHostedService<MyWebhookHandler>();
```

## See Also

- [Stripe Webhook Documentation](https://stripe.com/docs/webhooks)
- [Stripe CLI Documentation](https://stripe.com/docs/stripe-cli)
- [Main Plugin README](../../README.md)
