using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;

namespace ApiTester;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Stripe Payment Plugin API Tester - Test all API endpoints");

        // Global options
        var urlOption = new Option<string>(
            aliases: new[] { "--url", "-u" },
            getDefaultValue: () => "http://localhost:5120/api/stripe",
            description: "Base URL for the API");

        rootCommand.AddGlobalOption(urlOption);

        // Payment command
        var paymentCommand = new Command("payment", "Create a payment intent");
        var amountOption = new Option<int>(
            aliases: new[] { "--amount", "-a" },
            getDefaultValue: () => 2000,
            description: "Amount in cents (e.g., 2000 = $20.00)");
        var currencyOption = new Option<string>(
            aliases: new[] { "--currency", "-c" },
            getDefaultValue: () => "usd",
            description: "Currency code (e.g., usd, eur, gbp)");
        var descriptionOption = new Option<string>(
            aliases: new[] { "--description", "-d" },
            getDefaultValue: () => "Test payment from API Tester",
            description: "Payment description");

        paymentCommand.AddOption(amountOption);
        paymentCommand.AddOption(currencyOption);
        paymentCommand.AddOption(descriptionOption);

        paymentCommand.SetHandler(async (url, amount, currency, description) =>
        {
            await CreatePayment(url, amount, currency, description);
        }, urlOption, amountOption, currencyOption, descriptionOption);

        // Status command
        var statusCommand = new Command("status", "Get payment status");
        var paymentIdOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Payment Intent ID (e.g., pi_xxx...)") { IsRequired = true };

        statusCommand.AddOption(paymentIdOption);
        statusCommand.SetHandler(async (url, id) =>
        {
            await GetPaymentStatus(url, id);
        }, urlOption, paymentIdOption);

        // Refund command
        var refundCommand = new Command("refund", "Process a refund");
        var refundPaymentIdOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Payment Intent ID to refund") { IsRequired = true };
        var refundAmountOption = new Option<int?>(
            aliases: new[] { "--amount", "-a" },
            description: "Amount to refund in cents (omit for full refund)");
        var refundReasonOption = new Option<string>(
            aliases: new[] { "--reason", "-r" },
            getDefaultValue: () => "requested_by_customer",
            description: "Refund reason (requested_by_customer, duplicate, fraudulent)");

        refundCommand.AddOption(refundPaymentIdOption);
        refundCommand.AddOption(refundAmountOption);
        refundCommand.AddOption(refundReasonOption);

        refundCommand.SetHandler(async (url, id, amount, reason) =>
        {
            await ProcessRefund(url, id, amount, reason);
        }, urlOption, refundPaymentIdOption, refundAmountOption, refundReasonOption);

        // Confirm command
        var confirmCommand = new Command("confirm", "Confirm a payment intent with test payment method");
        var confirmPaymentIdOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Payment Intent ID to confirm") { IsRequired = true };
        var confirmPaymentMethodOption = new Option<string>(
            aliases: new[] { "--payment-method", "-pm" },
            getDefaultValue: () => "pm_card_visa",
            description: "Test payment method ID (pm_card_visa=Visa success, pm_card_mastercard=Mastercard, pm_card_amex=Amex, pm_card_chargeDeclined=declined, pm_card_authenticationRequired=requires 3DS)");

        confirmCommand.AddOption(confirmPaymentIdOption);
        confirmCommand.AddOption(confirmPaymentMethodOption);

        confirmCommand.SetHandler(async (url, id, paymentMethod) =>
        {
            await ConfirmPayment(url, id, paymentMethod);
        }, urlOption, confirmPaymentIdOption, confirmPaymentMethodOption);

        // Health command
        var healthCommand = new Command("health", "Check API health");
        healthCommand.SetHandler(async (url) =>
        {
            await CheckHealth(url);
        }, urlOption);

        // Interactive command - runs all tests in sequence
        var interactiveCommand = new Command("interactive", "Run interactive test session");
        interactiveCommand.SetHandler(async (url) =>
        {
            await RunInteractiveSession(url);
        }, urlOption);

        rootCommand.AddCommand(paymentCommand);
        rootCommand.AddCommand(statusCommand);
        rootCommand.AddCommand(confirmCommand);
        rootCommand.AddCommand(refundCommand);
        rootCommand.AddCommand(healthCommand);
        rootCommand.AddCommand(interactiveCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task CreatePayment(string baseUrl, int amount, string currency, string description)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  💳 Creating Payment Intent                               ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        var request = new
        {
            amount = amount,
            currency = currency,
            description = description,
            metadata = new Dictionary<string, string>
            {
                { "test_id", Guid.NewGuid().ToString() },
                { "created_by", "ApiTester" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            }
        };

        Console.WriteLine($"Request URL: {baseUrl}/payment");
        Console.WriteLine($"Amount:      {FormatAmount(amount, currency)}");
        Console.WriteLine($"Currency:    {currency.ToUpper()}");
        Console.WriteLine($"Description: {description}");
        Console.WriteLine();

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/payment", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Success!");
                Console.ResetColor();
                Console.WriteLine();
                
                var id = result.GetProperty("id").GetString();
                var status = result.GetProperty("status").GetString();
                var clientSecret = result.GetProperty("clientSecret").GetString();
                
                Console.WriteLine($"Payment Intent ID: {id}");
                Console.WriteLine($"Status:            {status}");
                Console.WriteLine($"Client Secret:     {clientSecret?[..20]}...");
                Console.WriteLine();
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("💡 Next steps:");
                Console.WriteLine($"   - Confirm payment: dotnet run -- confirm --id {id}");
                Console.WriteLine($"   - Check status: dotnet run -- status --id {id}");
                Console.WriteLine($"   - Process refund: dotnet run -- refund --id {id}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine(content);
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }

    static async Task GetPaymentStatus(string baseUrl, string paymentId)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  🔍 Getting Payment Status                                ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.WriteLine($"Request URL:       {baseUrl}/status/{paymentId}");
        Console.WriteLine($"Payment Intent ID: {paymentId}");
        Console.WriteLine();

        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/status/{paymentId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Success!");
                Console.ResetColor();
                Console.WriteLine();
                
                var id = result.GetProperty("id").GetString();
                var status = result.GetProperty("status").GetString();
                var amount = result.GetProperty("amount").GetInt64();
                var currency = result.GetProperty("currency").GetString();
                
                Console.WriteLine($"Payment Intent ID: {id}");
                Console.WriteLine($"Status:            {status}");
                Console.WriteLine($"Amount:            {FormatAmount(amount, currency)}");
                Console.WriteLine($"Currency:          {currency?.ToUpper()}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine(content);
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }

    static async Task ConfirmPayment(string baseUrl, string paymentId, string paymentMethodId)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ✅ Confirming Payment Intent                             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        var request = new
        {
            paymentIntentId = paymentId,
            paymentMethodId = paymentMethodId
        };

        Console.WriteLine($"Request URL:         {baseUrl}/confirm");
        Console.WriteLine($"Payment Intent ID:   {paymentId}");
        Console.WriteLine($"Payment Method ID:   {paymentMethodId}");
        Console.WriteLine();

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/confirm", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Success!");
                Console.ResetColor();
                Console.WriteLine();
                
                var id = result.GetProperty("id").GetString();
                var status = result.GetProperty("status").GetString();
                var amount = result.GetProperty("amount").GetInt64();
                var currency = result.GetProperty("currency").GetString();
                
                Console.WriteLine($"Payment Intent ID: {id}");
                Console.WriteLine($"Status:            {status}");
                Console.WriteLine($"Amount:            {FormatAmount(amount, currency)}");
                Console.WriteLine($"Currency:          {currency?.ToUpper()}");
                Console.WriteLine();
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("💡 Tip: Check your webhook subscriber to see the event notifications!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine(content);
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }

    static async Task ProcessRefund(string baseUrl, string paymentId, int? amount, string reason)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  💸 Processing Refund                                     ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        var request = new
        {
            paymentIntentId = paymentId,
            amount = amount,
            reason = reason
        };

        Console.WriteLine($"Request URL:       {baseUrl}/refund");
        Console.WriteLine($"Payment Intent ID: {paymentId}");
        Console.WriteLine($"Amount:            {(amount.HasValue ? $"{amount.Value / 100.0:F2}" : "Full refund")}");
        Console.WriteLine($"Reason:            {reason}");
        Console.WriteLine();

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/refund", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Success!");
                Console.ResetColor();
                Console.WriteLine();
                
                var id = result.GetProperty("id").GetString();
                var status = result.GetProperty("status").GetString();
                var refundAmount = result.GetProperty("amount").GetInt64();
                var currency = result.GetProperty("currency").GetString();
                
                Console.WriteLine($"Refund ID:    {id}");
                Console.WriteLine($"Status:       {status}");
                Console.WriteLine($"Amount:       {FormatAmount(refundAmount, currency)}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine(content);
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }

    static async Task CheckHealth(string baseUrl)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  🏥 Checking API Health                                   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.WriteLine($"Request URL: {baseUrl}/health");
        Console.WriteLine();

        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/health");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var status = result.GetProperty("status").GetString();
            
            if (status == "healthy")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Healthy!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  Unhealthy");
            }
            Console.ResetColor();
            Console.WriteLine();
            
            Console.WriteLine($"Status:    {status}");
            Console.WriteLine($"Timestamp: {result.GetProperty("timestamp").GetString()}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }

    static async Task RunInteractiveSession(string baseUrl)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║       🧪 Stripe Payment Plugin - Interactive Test Suite      ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
");
        Console.ResetColor();
        Console.WriteLine($"API Base URL: {baseUrl}");
        Console.WriteLine();
        Console.WriteLine("This will run through all API endpoints in sequence:");
        Console.WriteLine("  1. Health check");
        Console.WriteLine("  2. Create payment");
        Console.WriteLine("  3. Check payment status");
        Console.WriteLine("  4. Process refund");
        Console.WriteLine();
        Console.Write("Press Enter to continue or Ctrl+C to cancel...");
        Console.ReadLine();

        // 1. Health check
        await CheckHealth(baseUrl);
        Console.Write("Press Enter to continue...");
        Console.ReadLine();

        // 2. Create payment
        var paymentId = await CreatePaymentInteractive(baseUrl);
        if (string.IsNullOrEmpty(paymentId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to create payment. Aborting test session.");
            Console.ResetColor();
            return;
        }
        
        Console.Write("Press Enter to continue...");
        Console.ReadLine();

        // 3. Check status
        await GetPaymentStatus(baseUrl, paymentId);
        Console.Write("Press Enter to continue...");
        Console.ReadLine();

        // 4. Process refund
        await ProcessRefund(baseUrl, paymentId, null, "requested_by_customer");
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Interactive test session completed!");
        Console.ResetColor();
    }

    static async Task<string?> CreatePaymentInteractive(string baseUrl)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  💳 Creating Payment Intent                               ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        var request = new
        {
            amount = 2000,
            currency = "usd",
            description = "Interactive test payment",
            metadata = new Dictionary<string, string>
            {
                { "test_id", Guid.NewGuid().ToString() },
                { "session", "interactive" }
            }
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/payment", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                var id = result.GetProperty("id").GetString();
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Payment created successfully!");
                Console.ResetColor();
                Console.WriteLine($"Payment Intent ID: {id}");
                Console.WriteLine();
                
                return id;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine(content);
                Console.ResetColor();
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    static string FormatAmount(long amount, string? currency)
    {
        decimal displayAmount = amount / 100.0m;
        return currency?.ToLower() switch
        {
            "usd" => $"${displayAmount:F2}",
            "eur" => $"€{displayAmount:F2}",
            "gbp" => $"£{displayAmount:F2}",
            _ => $"{displayAmount:F2} {currency?.ToUpper() ?? "USD"}"
        };
    }
}
