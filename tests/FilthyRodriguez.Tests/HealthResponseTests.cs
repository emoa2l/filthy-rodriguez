using Microsoft.Extensions.Configuration;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Services;
using FilthyRodriguez.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class HealthResponseTests
{
    [Fact]
    public void HealthResponse_HasRequiredProperties()
    {
        // Arrange & Act
        var health = new HealthResponse
        {
            Status = "healthy",
            Stripe = "connected",
            Webhooks = "enabled",
            Websockets = "enabled",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("healthy", health.Status);
        Assert.Equal("connected", health.Stripe);
        Assert.Equal("enabled", health.Webhooks);
        Assert.Equal("enabled", health.Websockets);
        Assert.NotEqual(default(DateTime), health.Timestamp);
    }

    [Fact]
    public async Task GetHealthAsync_WithoutApiKey_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "" }, // Empty API key
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();

        services.AddLogging();
        services.Configure<StripePaymentOptions>(configuration.GetSection(StripePaymentOptions.SectionName));
        services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
        services.AddSingleton<IMetricsSink, LoggingMetricsSink>();
        services.AddSingleton<IPaymentMetrics, PaymentMetricsService>();
        services.AddSingleton<IStripePaymentService, StripePaymentService>();
        var serviceProvider = services.BuildServiceProvider();
        var paymentService = serviceProvider.GetRequiredService<IStripePaymentService>();

        // Act
        var health = await paymentService.GetHealthAsync();

        // Assert
        Assert.Equal("unhealthy", health.Status);
        Assert.Equal("not_configured", health.Stripe);
        Assert.Equal("enabled", health.Webhooks);
        Assert.Equal("enabled", health.Websockets);
        Assert.NotEqual(default(DateTime), health.Timestamp);
    }

    [Fact]
    public async Task GetHealthAsync_WithoutWebhookSecret_ReportsNotConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "" }, // Empty API key
                { "FilthyRodriguez:WebhookSecret", "" } // Empty webhook secret
            })
            .Build();

        services.AddLogging();
        services.Configure<StripePaymentOptions>(configuration.GetSection(StripePaymentOptions.SectionName));
        services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
        services.AddSingleton<IMetricsSink, LoggingMetricsSink>();
        services.AddSingleton<IPaymentMetrics, PaymentMetricsService>();
        services.AddSingleton<IStripePaymentService, StripePaymentService>();
        var serviceProvider = services.BuildServiceProvider();
        var paymentService = serviceProvider.GetRequiredService<IStripePaymentService>();

        // Act
        var health = await paymentService.GetHealthAsync();

        // Assert
        Assert.Equal("unhealthy", health.Status);
        Assert.Equal("not_configured", health.Stripe);
        Assert.Equal("not_configured", health.Webhooks);
        Assert.Equal("enabled", health.Websockets);
    }

    [Fact]
    public async Task GetHealthAsync_WithInvalidApiKey_ReturnsDisconnected()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_invalid_key" }, // Invalid API key
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();

        services.AddLogging();
        services.Configure<StripePaymentOptions>(configuration.GetSection(StripePaymentOptions.SectionName));
        services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
        services.AddSingleton<IMetricsSink, LoggingMetricsSink>();
        services.AddSingleton<IPaymentMetrics, PaymentMetricsService>();
        services.AddSingleton<IStripePaymentService, StripePaymentService>();
        var serviceProvider = services.BuildServiceProvider();
        var paymentService = serviceProvider.GetRequiredService<IStripePaymentService>();

        // Act
        var health = await paymentService.GetHealthAsync();

        // Assert
        Assert.Equal("unhealthy", health.Status);
        Assert.Equal("disconnected", health.Stripe); // Should fail to connect with invalid key
        Assert.Equal("enabled", health.Webhooks);
        Assert.Equal("enabled", health.Websockets);
    }
}
