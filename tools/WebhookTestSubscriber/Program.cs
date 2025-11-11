using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FilthyRodriguez.Extensions;
using WebhookTestSubscriber;

var builder = WebApplication.CreateBuilder(args);

// Configure the Stripe Payment Plugin
builder.Services.AddFilthyRodriguez(builder.Configuration);

// Register our webhook subscriber service
builder.Services.AddHostedService<WebhookSubscriberService>();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Map Stripe payment endpoints (including webhook)
app.MapStripePaymentEndpoints("/api/stripe");

// Map WebSocket endpoint
app.MapStripeWebSocket("/stripe/ws");

// Display startup information
var port = app.Urls.FirstOrDefault() ?? "http://localhost:5000";

Console.Clear();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║      🎧 Stripe Webhook Test Subscriber & Event Handler       ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
");
Console.ResetColor();

Console.WriteLine("This tool provides TWO ways to test Stripe webhooks:");
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("1️⃣  WEBHOOK ENDPOINT (API)");
Console.ResetColor();
Console.WriteLine($"   Endpoint: {port}/api/stripe/webhook");
Console.WriteLine("   This endpoint receives webhook POST requests from Stripe");
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("2️⃣  EVENT SUBSCRIBER (Console Output)");
Console.ResetColor();
Console.WriteLine("   This subscriber listens to all webhook events and displays them");
Console.WriteLine("   Watch this console for real-time event notifications!");
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.ResetColor();

Console.WriteLine();
Console.WriteLine("📋 SETUP INSTRUCTIONS:");
Console.WriteLine();
Console.WriteLine("1. Install Stripe CLI (if not installed):");
Console.WriteLine("   https://stripe.com/docs/stripe-cli#install");
Console.WriteLine();
Console.WriteLine("2. Login to Stripe CLI:");
Console.WriteLine("   stripe login");
Console.WriteLine();
Console.WriteLine("3. Forward webhooks to this application:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"   stripe listen --forward-to {port}/api/stripe/webhook");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("4. Copy the webhook signing secret (whsec_...) from Stripe CLI");
Console.WriteLine();
Console.WriteLine("5. Update appsettings.json with your webhook secret");
Console.WriteLine();
Console.WriteLine("6. Restart this application");
Console.WriteLine();
Console.WriteLine("7. Trigger a test event:");
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("   stripe trigger payment_intent.succeeded");
Console.ResetColor();
Console.WriteLine();

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("✨ Ready! Waiting for webhook events...");
Console.ResetColor();
Console.WriteLine();

app.Run();
