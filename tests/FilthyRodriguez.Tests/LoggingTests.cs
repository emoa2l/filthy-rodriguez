using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FilthyRodriguez.Extensions;
using FilthyRodriguez.Handlers;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class LoggingTests
{
    [Fact]
    public void StripePaymentService_HasLoggerInjected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();
        
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var paymentService = serviceProvider.GetService<IStripePaymentService>();

        // Assert
        Assert.NotNull(paymentService);
        Assert.IsType<StripePaymentService>(paymentService);
    }

    [Fact]
    public void StripeWebhookHandler_HasLoggerInjected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();
        
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var webhookHandler = serviceProvider.GetService<StripeWebhookHandler>();

        // Assert
        Assert.NotNull(webhookHandler);
    }

    [Fact]
    public void StripeWebSocketHandler_HasLoggerInjected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();
        
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var wsHandler = serviceProvider.GetService<StripeWebSocketHandler>();

        // Assert
        Assert.NotNull(wsHandler);
    }

    [Fact]
    public void AllServices_CanBeResolved_WithLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();
        
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Verify all services can be resolved without throwing
        var paymentService = serviceProvider.GetService<IStripePaymentService>();
        Assert.NotNull(paymentService);
        
        var webhookHandler = serviceProvider.GetService<StripeWebhookHandler>();
        Assert.NotNull(webhookHandler);
        
        var wsHandler = serviceProvider.GetService<StripeWebSocketHandler>();
        Assert.NotNull(wsHandler);
    }
}
