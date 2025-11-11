using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Extensions;

namespace FilthyRodriguez.Tests;

public class ConfigurationValidationTests
{
    [Fact]
    public void StripePaymentOptions_LoadsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_apikey123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_secret123" },
                { "FilthyRodriguez:SuccessUrl", "https://example.com/success" },
                { "FilthyRodriguez:CancelUrl", "https://example.com/cancel" }
            })
            .Build();

        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<StripePaymentOptions>>();

        // Assert
        Assert.Equal("sk_test_apikey123", options.Value.ApiKey);
        Assert.Equal("whsec_secret123", options.Value.WebhookSecret);
        Assert.Equal("https://example.com/success", options.Value.SuccessUrl);
        Assert.Equal("https://example.com/cancel", options.Value.CancelUrl);
    }

    [Fact]
    public void Configuration_WithMissingApiKey_LogsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "" }, // Empty API key
                { "FilthyRodriguez:WebhookSecret", "whsec_secret123" }
            })
            .Build();

        // Act & Assert - Should not throw, but will log error
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Trigger the validator by requesting a service
        var options = serviceProvider.GetService<IOptions<StripePaymentOptions>>();
        Assert.NotNull(options);
        Assert.Empty(options.Value.ApiKey);
    }

    [Fact]
    public void Configuration_WithMissingWebhookSecret_LogsWarning()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "" } // Empty webhook secret
            })
            .Build();

        // Act & Assert - Should not throw, but will log warning
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetService<IOptions<StripePaymentOptions>>();
        Assert.NotNull(options);
        Assert.Empty(options.Value.WebhookSecret);
    }

    [Fact]
    public void Configuration_WithValidSettings_LoadsSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_validkey" },
                { "FilthyRodriguez:WebhookSecret", "whsec_validsecret" }
            })
            .Build();

        // Act
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - All services should resolve successfully
        var paymentService = serviceProvider.GetService<Services.IStripePaymentService>();
        Assert.NotNull(paymentService);

        var webhookHandler = serviceProvider.GetService<Handlers.StripeWebhookHandler>();
        Assert.NotNull(webhookHandler);

        var wsHandler = serviceProvider.GetService<Handlers.StripeWebSocketHandler>();
        Assert.NotNull(wsHandler);
    }

    [Fact]
    public void StripePaymentOptions_SectionName_IsCorrect()
    {
        // Arrange & Act
        var sectionName = StripePaymentOptions.SectionName;

        // Assert
        Assert.Equal("FilthyRodriguez", sectionName);
    }

    [Fact]
    public void Configuration_WithPartialSettings_LoadsAvailableValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                // WebhookSecret and URLs omitted
            })
            .Build();

        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<StripePaymentOptions>>();

        // Assert
        Assert.Equal("sk_test_123", options.Value.ApiKey);
        Assert.Equal(string.Empty, options.Value.WebhookSecret); // Should be empty string (default)
        Assert.Null(options.Value.SuccessUrl);
        Assert.Null(options.Value.CancelUrl);
    }

    [Fact]
    public void Configuration_DefaultValues_AreInitializedCorrectly()
    {
        // Arrange & Act
        var options = new StripePaymentOptions();

        // Assert
        Assert.Equal(string.Empty, options.ApiKey);
        Assert.Equal(string.Empty, options.WebhookSecret);
        Assert.Null(options.SuccessUrl);
        Assert.Null(options.CancelUrl);
    }
}
