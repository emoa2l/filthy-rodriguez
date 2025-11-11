# Stripe Payment Plugin for .NET 8

A drop-in, framework-agnostic Stripe payment plugin for .NET 8 applications. This library provides a simple, JSON-based interface for handling Stripe payments with automatic webhook processing and real-time status updates via WebSockets or polling.

## Features

- 🎯 **Framework-agnostic**: JSON in/out only, works with any .NET 8 application
- 🔄 **Automatic webhook handling**: Built-in Stripe webhook verification and processing
- 📡 **Dual status checking**: Support for both polling (REST API) and real-time updates (WebSockets)
- ⚙️ **Simple configuration**: Configure via `appsettings.json`
- 📦 **Self-contained**: Minimal dependencies, easy to integrate
- 🚀 **Simple to use**: Just a few lines of code to get started

## Installation

Add the library to your project:

```bash
dotnet add reference path/to/FilthyRodriguez.csproj
```

Or add it to your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/FilthyRodriguez/FilthyRodriguez.csproj" />
</ItemGroup>
```

## Quick Start

### 1. Configuration

Add Stripe configuration to your `appsettings.json`:

```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_your_stripe_api_key_here",
    "WebhookSecret": "whsec_your_webhook_secret_here",
    "SuccessUrl": "https://yourapp.com/success",
    "CancelUrl": "https://yourapp.com/cancel"
  }
}
```

### 2. Setup in Program.cs

```csharp
using FilthyRodriguez.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Stripe Payment Plugin
builder.Services.AddFilthyRodriguez(builder.Configuration);

var app = builder.Build();

// Enable WebSockets (required for real-time updates)
app.UseWebSockets();

// Map Stripe payment endpoints
app.MapStripePaymentEndpoints("/api/stripe");

// Map Stripe WebSocket endpoint
app.MapStripeWebSocket("/stripe/ws");

app.Run();
```

That's it! Your application now has these endpoints:
- `POST /api/stripe/payment` - Create a payment
- `GET /api/stripe/status/{id}` - Check payment status
- `POST /api/stripe/confirm` - Confirm payment with test card (testing only)
- `POST /api/stripe/refund` - Process a refund
- `POST /api/stripe/webhook` - Stripe webhook endpoint (auto-registered)
- `GET /api/stripe/health` - Health check endpoint
- `WS /stripe/ws` - WebSocket for real-time updates

## API Endpoints

### POST /api/stripe/payment

Create a new payment intent.

**Request:**
```json
{
  "amount": 2000,
  "currency": "usd",
  "description": "Test payment",
  "metadata": {
    "order_id": "12345"
  }
}
```

**Response:**
```json
{
  "id": "pi_1234567890",
  "status": "requires_payment_method",
  "amount": 2000,
  "currency": "usd",
  "clientSecret": "pi_1234567890_secret_...",
  "createdAt": "2025-11-10T18:00:00Z"
}
```

### GET /api/stripe/status/{id}

Check the status of a payment intent.

**Response:**
```json
{
  "id": "pi_1234567890",
  "status": "succeeded",
  "amount": 2000,
  "currency": "usd",
  "createdAt": "2025-11-10T18:00:00Z",
  "updatedAt": "2025-11-10T18:01:00Z"
}
```

### POST /api/stripe/confirm

Confirm a payment intent with a test payment method. **For testing only** - triggers webhook events!

**Request:**
```json
{
  "paymentIntentId": "pi_1234567890",
  "paymentMethodId": "pm_card_visa"
}
```

**Response:**
```json
{
  "id": "pi_1234567890",
  "status": "succeeded",
  "amount": 2000,
  "currency": "usd",
  "createdAt": "2025-11-10T18:00:00Z",
  "updatedAt": "2025-11-10T18:01:00Z"
}
```

**Notes:**
- This endpoint is designed for testing/development with Stripe test payment method tokens
- Uses secure test payment method tokens instead of raw card data for better security
- Confirming a payment changes its status from `requires_payment_method` to `succeeded` (or `requires_action` for 3DS cards)
- Status changes trigger webhook events (`payment_intent.processing`, `payment_intent.succeeded`, etc.)
- WebSocket clients subscribed to the payment intent receive real-time updates
- See [Stripe Test Payment Methods](https://stripe.com/docs/testing#cards) for available test tokens

**Available Test Payment Method Tokens:**
- `pm_card_visa` - Visa card (success)
- `pm_card_visa_debit` - Visa debit card (success)
- `pm_card_mastercard` - Mastercard (success)
- `pm_card_amex` - American Express (success)
- `pm_card_discover` - Discover (success)
- `pm_card_chargeDeclined` - Card will be declined
- `pm_card_authenticationRequired` - Requires 3D Secure authentication

### GET /api/stripe/health

Health check endpoint to verify plugin configuration and Stripe connectivity. Useful for monitoring and debugging.

**Response (Healthy - 200 OK):**
```json
{
  "status": "healthy",
  "stripe": "connected",
  "webhooks": "enabled",
  "websockets": "enabled",
  "timestamp": "2025-11-10T20:00:00Z"
}
```

**Response (Unhealthy - 503 Service Unavailable):**
```json
{
  "status": "unhealthy",
  "stripe": "disconnected",
  "webhooks": "not_configured",
  "websockets": "enabled",
  "timestamp": "2025-11-10T20:00:00Z"
}
```

**Status Values:**
- `status`: `"healthy"` or `"unhealthy"`
- `stripe`: `"connected"`, `"disconnected"`, or `"not_configured"`
- `webhooks`: `"enabled"` or `"not_configured"`
- `websockets`: `"enabled"` (always enabled)

### POST /api/stripe/refund

Process a refund for a payment intent. Supports both full and partial refunds.

**Request (Full Refund):**
```json
{
  "paymentIntentId": "pi_1234567890",
  "reason": "requested_by_customer"
}
```

**Request (Partial Refund):**
```json
{
  "paymentIntentId": "pi_1234567890",
  "amount": 1000,
  "reason": "requested_by_customer",
  "metadata": {
    "refund_reason": "damaged_product"
  }
}
```

**Response:**
```json
{
  "id": "re_1234567890",
  "paymentIntentId": "pi_1234567890",
  "status": "succeeded",
  "amount": 1000,
  "currency": "usd",
  "reason": "requested_by_customer",
  "createdAt": "2025-11-10T20:00:00Z"
}
```

**Notes:**
- Omit `amount` for a full refund
- Include `amount` for a partial refund (in cents)
- WebSocket clients subscribed to the payment intent will receive real-time refund notifications
- Transaction status in the database is updated to `refunded_{status}`

### POST /api/stripe/webhook

Stripe webhook endpoint (configured in your Stripe dashboard). This endpoint:
- Verifies webhook signatures
- Processes payment events
- Notifies WebSocket clients of updates
- Triggers registered webhook notification handlers

## Webhook Notifications

The plugin provides three flexible ways to receive webhook notifications:

### 1. Callback Pattern (Simple)

Perfect for simple scenarios where you want to quickly react to payment events:

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithWebhookCallback(async (paymentIntent, stripeEvent) =>
    {
        // Your code executes when webhook received
        _logger.LogInformation("Payment {Id} status: {Status}", 
            paymentIntent.Id, paymentIntent.Status);
        
        // Call your own services
        await _orderService.UpdateOrderStatus(paymentIntent.Metadata["order_id"]);
    });
```

### 2. EventHandler Pattern (Standard .NET)

Familiar .NET event pattern for subscribing to specific webhook events:

```csharp
public class PaymentNotificationService : IHostedService
{
    private readonly IStripeWebhookNotifier _notifier;

    public PaymentNotificationService(IStripeWebhookNotifier notifier)
    {
        _notifier = notifier;
        
        // Subscribe to specific events
        _notifier.PaymentIntentSucceeded += OnPaymentSucceeded;
        _notifier.PaymentIntentFailed += OnPaymentFailed;
        _notifier.WebhookReceived += OnAnyWebhook;
    }

    private async void OnPaymentSucceeded(object? sender, PaymentIntentEventArgs e)
    {
        _logger.LogInformation("Payment succeeded: {Id}", e.PaymentIntent.Id);
        await _orderService.CompleteOrder(e.PaymentIntent.Metadata["order_id"]);
    }

    private async void OnPaymentFailed(object? sender, PaymentIntentEventArgs e)
    {
        _logger.LogError("Payment failed: {Id}", e.PaymentIntent.Id);
        await _orderService.CancelOrder(e.PaymentIntent.Metadata["order_id"]);
    }
}

// Register the service
builder.Services.AddHostedService<PaymentNotificationService>();
```

Available events on `IStripeWebhookNotifier`:
- `PaymentIntentCreated`
- `PaymentIntentSucceeded`
- `PaymentIntentFailed`
- `PaymentIntentCanceled`
- `PaymentIntentProcessing`
- `WebhookReceived` (all webhook events)

### 3. IStripeWebhookHandler Interface (Advanced)

Best for complex scenarios with multiple handlers or team collaboration:

```csharp
// Implement the handler interface
public class OrderWebhookHandler : IStripeWebhookHandler
{
    private readonly IOrderService _orderService;

    public OrderWebhookHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent, Event webhookEvent)
    {
        await _orderService.CompleteOrder(paymentIntent.Metadata["order_id"]);
    }

    public async Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent, Event webhookEvent)
    {
        await _orderService.CancelOrder(paymentIntent.Metadata["order_id"]);
    }
}

// Register multiple handlers - all will be invoked
builder.Services.AddSingleton<IStripeWebhookHandler, OrderWebhookHandler>();
builder.Services.AddSingleton<IStripeWebhookHandler, AnalyticsWebhookHandler>();
builder.Services.WithWebhookHandlers(); // Optional, for clarity
```

**Benefits of multiple handlers:**
- Separation of concerns (orders, analytics, notifications)
- Different teams can maintain different handlers
- Easy to add/remove functionality without modifying existing code

### Choosing the Right Pattern

| Pattern | Use When | Pros | Cons |
|---------|----------|------|------|
| **Callback** | Simple, single use case | Quick to implement, minimal code | Less flexible, harder to test |
| **EventHandler** | Standard .NET patterns preferred | Familiar pattern, easy to understand | async void handlers (fire-and-forget) |
| **IStripeWebhookHandler** | Multiple handlers or complex logic | Testable, composable, DI-friendly | More boilerplate code |

**Recommendation:** Start with Callback for prototypes, use IStripeWebhookHandler for production apps

### Webhook Configuration

Configure webhook notification behavior in `appsettings.json`:

```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "WebhookNotifications": {
      "Enabled": true,
      "ContinueOnError": true,
      "TimeoutSeconds": 30,
      "RetryFailedCallbacks": false,
      "MaxRetries": 3
    }
  }
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable webhook notifications | `true` |
| `ContinueOnError` | Continue processing if handler fails | `true` |
| `TimeoutSeconds` | Max time for handler execution | `30` |
| `RetryFailedCallbacks` | Retry failed handlers | `false` |
| `MaxRetries` | Max retry attempts | `3` |

### WS /stripe/ws

WebSocket endpoint for real-time payment updates.

**Subscribe to a payment:**
```json
{
  "action": "subscribe",
  "paymentId": "pi_1234567890"
}
```

**Response:**
```json
{
  "type": "subscribed",
  "paymentId": "pi_1234567890"
}
```

**Payment update notification:**
```json
{
  "type": "payment_update",
  "paymentId": "pi_1234567890",
  "data": {
    "id": "pi_1234567890",
    "status": "succeeded",
    "amount": 2000,
    "currency": "usd"
  }
}
```

**Refund update notification:**
```json
{
  "type": "payment_update",
  "paymentId": "pi_1234567890",
  "data": {
    "type": "refund_update",
    "refundId": "re_1234567890",
    "paymentIntentId": "pi_1234567890",
    "status": "succeeded",
    "amount": 1000
  }
}
```

## Usage Examples

### Creating a Payment (cURL)

```bash
curl -X POST http://localhost:5000/api/stripe/payment \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 2000,
    "currency": "usd",
    "description": "Test payment"
  }'
```

### Checking Payment Status (cURL)

```bash
curl http://localhost:5000/api/stripe/status/pi_1234567890
```

### Processing a Refund (cURL)

**Full Refund:**
```bash
curl -X POST http://localhost:5000/api/stripe/refund \
  -H "Content-Type: application/json" \
  -d '{
    "paymentIntentId": "pi_1234567890",
    "reason": "requested_by_customer"
  }'
```

**Partial Refund:**
```bash
curl -X POST http://localhost:5000/api/stripe/refund \
  -H "Content-Type: application/json" \
  -d '{
    "paymentIntentId": "pi_1234567890",
    "amount": 1000,
    "reason": "requested_by_customer"
  }'
```

### Health Check (cURL)

```bash
curl http://localhost:5000/api/stripe/health
```

### WebSocket Client (JavaScript)

```javascript
const ws = new WebSocket('ws://localhost:5000/stripe/ws');

ws.onopen = () => {
  // Subscribe to payment updates
  ws.send(JSON.stringify({
    action: 'subscribe',
    paymentId: 'pi_1234567890'
  }));
};

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  
  if (data.type === 'payment_update') {
    console.log('Payment updated:', data.data);
    // Handle payment status update
  }
};
```

## Configuration Options

| Option | Description | Required |
|--------|-------------|----------|
| `ApiKey` | Your Stripe API key (starts with `sk_test_` or `sk_live_`) | Yes |
| `WebhookSecret` | Stripe webhook signing secret (starts with `whsec_`) | Yes |
| `SuccessUrl` | URL to redirect after successful payment | No |
| `CancelUrl` | URL to redirect after cancelled payment | No |
| `Database` | Optional database configuration for persistence | No |

## Database Persistence (Optional)

> ⚠️ **Experimental Feature**: Database persistence is currently experimental and under active development. The API and configuration options may change in future releases. Use with caution in production environments.

The plugin supports optional Entity Framework Core persistence for storing payment transactions in a database. This feature is **opt-in** and the plugin works perfectly fine without it using in-memory storage.

### Why Use Database Persistence?

- ✅ **Survive app restarts** - Transactions persist across deployments
- ✅ **Complete audit trail** - Track full transaction lifecycle
- ✅ **Query capabilities** - Use SQL to query payment data
- ✅ **Compliance** - Meet audit and regulatory requirements
- ✅ **Analytics** - Connect to BI tools for reporting
- ✅ **Flexible schema** - Use existing database tables with custom field names

### Supported Databases

- SQL Server
- PostgreSQL
- MySQL
- SQLite

### Basic Configuration

Add database configuration to your `appsettings.json`:

```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "Database": {
      "Enabled": true,
      "ConnectionString": "Server=localhost;Database=payments;Trusted_Connection=true;",
      "Provider": "SqlServer"
    }
  }
}
```

Then enable Entity Framework in your `Program.cs`:

```csharp
using FilthyRodriguez.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Stripe Payment Plugin with database persistence
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithEntityFramework();

var app = builder.Build();

// Optional: Create database on startup (development only)
// await app.Services.EnsureDatabaseCreatedAsync();

app.UseWebSockets();
app.MapStripePaymentEndpoints("/api/stripe");
app.MapStripeWebSocket("/stripe/ws");

app.Run();
```

### Database Providers

#### SQL Server

```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Server=localhost;Database=payments;Trusted_Connection=true;",
    "Provider": "SqlServer"
  }
}
```

#### PostgreSQL

```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Host=localhost;Database=payments;Username=app;Password=secret",
    "Provider": "PostgreSQL"
  }
}
```

#### MySQL

```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Server=localhost;Database=payments;User=app;Password=secret;",
    "Provider": "MySQL"
  }
}
```

#### SQLite (Development/Testing)

```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Data Source=payments.db",
    "Provider": "SQLite"
  }
}
```

### Custom Table and Field Names

You can customize table and field names to match your existing database schema:

```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "...",
    "Provider": "SqlServer",
    "TableName": "payment_transactions",
    "FieldMapping": {
      "Id": "id",
      "StripePaymentIntentId": "stripe_intent_id",
      "Status": "status",
      "Amount": "amount",
      "Currency": "currency",
      "ClientSecret": "client_secret",
      "CreatedAt": "created_at",
      "UpdatedAt": "updated_at",
      "Metadata": "metadata_json"
    }
  }
}
```

### Database Schema

The default database table structure is:

```sql
CREATE TABLE stripe_transactions (
    transaction_id NVARCHAR(100) PRIMARY KEY,
    stripe_pi_id NVARCHAR(100) NOT NULL,
    payment_status NVARCHAR(50) NOT NULL,
    amount_cents BIGINT NOT NULL,
    currency_code NVARCHAR(3) NOT NULL,
    client_secret NVARCHAR(500),
    metadata_json NVARCHAR(MAX),
    created_timestamp DATETIME2 NOT NULL,
    updated_timestamp DATETIME2 NOT NULL
);

CREATE INDEX IX_stripe_transactions_stripe_pi_id ON stripe_transactions(stripe_pi_id);
CREATE INDEX IX_stripe_transactions_payment_status ON stripe_transactions(payment_status);
CREATE INDEX IX_stripe_transactions_created_timestamp ON stripe_transactions(created_timestamp);
```

### Creating the Database

You have several options for creating the database:

#### Option 1: Auto-create on startup (Development only)

```csharp
var app = builder.Build();

// Creates database if it doesn't exist
await app.Services.EnsureDatabaseCreatedAsync();

app.Run();
```

#### Option 2: Apply migrations (Production recommended)

```bash
# Add EF Core tools
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add InitialCreate --project YourApp

# Apply migration
dotnet ef database update --project YourApp
```

#### Option 3: Use existing database

Simply create the table manually with your preferred column names and configure the field mapping in `appsettings.json`.

For more advanced integration scenarios including safeguarding your tables, using table prefixes, custom schemas, and sharing databases, see the **[Database Integration Guide](DATABASE_INTEGRATION.md)**.

### Advanced Configuration

#### Explicit DbContext Configuration

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithEntityFramework(options => 
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    });
```

#### Connection Pooling

The plugin uses Entity Framework Core's built-in connection pooling. For high-throughput scenarios, adjust the pool size in your connection string:

```
Server=localhost;Database=payments;Max Pool Size=200;
```

## Logging

The plugin uses structured logging via `ILogger<T>` to provide comprehensive insights into payment processing:

### What Gets Logged

- **Payment Creation**: Transaction ID, amount, currency, and status
- **Webhook Events**: Event type, event ID, and payment intent details
- **WebSocket Events**: Connection/disconnection, subscriptions, and notifications
- **Errors**: Detailed error information with context (Stripe error codes, transaction IDs)
- **Configuration**: Validation warnings on startup if API keys are missing

### Log Levels

- **Information**: Normal operations (payments, webhooks, connections)
- **Warning**: Non-critical issues (invalid signatures, configuration warnings)
- **Error**: Failures that need attention (API errors, exceptions)
- **Debug**: Detailed diagnostic information

### Configuration

Logging is automatically configured via ASP.NET Core. You can customize it in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "FilthyRodriguez": "Debug",
      "FilthyRodriguez.Services": "Information",
      "FilthyRodriguez.Handlers": "Information"
    }
  }
}
```

### Example Log Output

```
info: FilthyRodriguez.Services.StripePaymentService[0]
      Creating payment intent for amount 2000 usd
info: FilthyRodriguez.Services.StripePaymentService[0]
      Payment intent created successfully. PaymentIntentId: pi_1234567890, Status: requires_payment_method, Amount: 2000 usd
info: FilthyRodriguez.Handlers.StripeWebhookHandler[0]
      Webhook event received. EventId: evt_1234567890, EventType: payment_intent.succeeded
info: FilthyRodriguez.Handlers.StripeWebSocketHandler[0]
      WebSocket connection established. ConnectionId: abc123
```

### Security

The plugin is designed with security in mind:
- ✅ API keys are **never** logged
- ✅ Webhook secrets are **never** logged
- ✅ Client secrets are **never** logged
- ✅ Only non-sensitive transaction metadata is logged

## Project Structure

```
src/FilthyRodriguez/
├── Configuration/          # Configuration models
├── Models/                 # Request/response models
├── Services/               # Payment service implementation
├── Handlers/               # Webhook and WebSocket handlers
├── Data/                   # Repository interfaces and implementations
├── Abstractions/           # Core interfaces
└── Extensions/             # DI and endpoint extension methods

examples/HtmlTestApp/
├── Program.cs             # Simple HTML test app demonstrating full payment flow
├── wwwroot/               # Static HTML files
│   ├── index.html         # Payment form with WebSocket integration
│   ├── success.html       # Success page
│   ├── cancel.html        # Cancel page
│   └── refund.html        # Refund page for testing refunds
└── README.md              # HTML test app documentation

tools/ApiTester/
├── Program.cs             # CLI tool for testing all API endpoints
└── README.md              # ApiTester documentation

tools/WebhookTestSubscriber/
├── Program.cs             # Tool for testing webhook notifications
├── WebhookSubscriberService.cs
└── README.md              # Webhook subscriber documentation
```

## Development

### Build the solution

```bash
dotnet build
```

### Run the HTML Test App

The HTML Test App provides a complete demonstration of the payment flow:

```bash
cd examples/HtmlTestApp
dotnet run
```
Open your browser to `http://localhost:5000` to see:
- Beautiful payment form UI with product summary
- Quick test button for instant testing with Stripe test cards
- Automatic redirect to success/cancel pages
- Real-time WebSocket updates for payment status
- Payment confirmation pages with detailed information
- Refund page for testing full and partial refunds

See [examples/HtmlTestApp/README.md](examples/HtmlTestApp/README.md) for detailed instructions.

### Testing with ApiTester (Recommended)

The easiest way to test the complete payment flow including webhooks:

```bash
# 1. Start the WebhookTestSubscriber (displays webhook events in console)
cd tools/WebhookTestSubscriber
dotnet run

# 2. In another terminal, use ApiTester to create and confirm a payment
cd tools/ApiTester

# Create a payment intent
dotnet run -- payment --amount 2000 --description "Test payment"
# Copy the payment intent ID from output (e.g., pi_XXXXXXXXXXXXX)

# Confirm the payment with a test card (triggers webhooks!)
dotnet run -- confirm --id pi_XXXXXXXXXXXXX --card 4242424242424242
# Watch the WebhookTestSubscriber console for events!

# Check the final status
dotnet run -- status --id pi_XXXXXXXXXXXXX
```

**Available Test Cards:**
- `4242424242424242` - Visa (succeeds)
- `4000000000000002` - Generic decline
- `4000002500003155` - Requires 3D Secure authentication
- `4000000000009995` - Insufficient funds decline

See [Stripe Test Cards](https://stripe.com/docs/testing#cards) for more options.

### Testing with Stripe CLI

For testing with Stripe's webhook event simulator:

1. Install [Stripe CLI](https://stripe.com/docs/stripe-cli)
2. Login: `stripe login`
3. Forward webhooks to your local server:
   ```bash
   stripe listen --forward-to http://localhost:5120/api/stripe/webhook
   ```
4. Copy the webhook signing secret (`whsec_...`) provided by the CLI and update your `appsettings.json`
5. Restart the application
6. Trigger a test webhook event:
   ```bash
   stripe trigger payment_intent.succeeded
   ```

## Dependencies

### Required
- **Stripe.net** - Official Stripe SDK for .NET
- **Microsoft.AspNetCore.App** (FrameworkReference) - ASP.NET Core framework

### Optional (for database persistence)
- **Microsoft.EntityFrameworkCore** (8.0.0+)
- **Microsoft.EntityFrameworkCore.SqlServer** (8.0.0+) - For SQL Server
- **Npgsql.EntityFrameworkCore.PostgreSQL** (8.0.0+) - For PostgreSQL
- **Pomelo.EntityFrameworkCore.MySql** (8.0.0+) - For MySQL
- **Microsoft.EntityFrameworkCore.Sqlite** (8.0.0+) - For SQLite

> **Note**: The plugin includes EF Core packages but works perfectly without database persistence. If you don't configure a database, transactions are stored in-memory only.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and questions, please use the GitHub issue tracker.
