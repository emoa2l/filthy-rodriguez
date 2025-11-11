using FilthyRodriguez.Extensions;
using FilthyRodriguez.Abstractions;
using HtmlTestApp;

var builder = WebApplication.CreateBuilder(args);

// Register the console event listener to log all events
builder.Services.AddSingleton<IPaymentEventListener, ConsoleEventListener>();

// Add Stripe Payment Plugin with Entity Framework (if database is enabled)
var config = builder.Configuration.GetSection("FilthyRodriguez:Database");
var dbEnabled = config.GetValue<bool>("Enabled");

if (dbEnabled)
{
    builder.Services.AddFilthyRodriguez(builder.Configuration)
        .WithEntityFramework();
}
else
{
    builder.Services.AddFilthyRodriguez(builder.Configuration);
}

var app = builder.Build();

// Ensure database is created (when database is enabled)
if (dbEnabled)
{
    try
    {
        await app.Services.EnsureDatabaseCreatedAsync();
        app.Logger.LogInformation("✓ Database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to initialize database. Continuing with in-memory storage.");
        // If database fails, we'll fall back to in-memory (though this may cause runtime errors)
    }
}

// Enable WebSockets for real-time updates
app.UseWebSockets();

// Serve static files (HTML, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

// Map Stripe payment endpoints
app.MapStripePaymentEndpoints("/api/stripe");

// Map Stripe WebSocket endpoint for real-time updates
app.MapStripeWebSocket("/stripe/ws");

var dbMode = dbEnabled ? "SQLite Database" : "In-Memory";
app.Logger.LogInformation(@"
╔══════════════════════════════════════════════════════════════╗
║           Stripe Payment HTML Test App                       ║
╚══════════════════════════════════════════════════════════════╝

🚀 Application started successfully!
💾 Storage Mode: {DbMode}

📄 Open your browser and navigate to:
   http://localhost:5000/

🔧 SETUP (if not done already):
   1. Get your Stripe test API key from: https://dashboard.stripe.com/test/apikeys
   2. Update appsettings.json with your API key
   3. Restart the application

🗄️  DATABASE TESTING:
   Run with: dotnet run --environment=Sqlite
   Database: filthy_rodriguez_test.db

💳 This demo includes:
   ✓ Credit card payment form
   ✓ Automatic redirect on success/cancel
   ✓ Payment confirmation pages
   ✓ Real-time WebSocket updates
   ✓ Test card support
   ✓ Optional database persistence

🧪 Test Cards:
   Success: 4242 4242 4242 4242
   Declined: 4000 0000 0000 0002
   3D Secure: 4000 0025 0000 3155
", dbMode);

app.Run();
