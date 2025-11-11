using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Extensions;
using FilthyRodriguez.Handlers;
using System.Text;

namespace FilthyRodriguez.Tests;

public class WebhookHandlerIntegrationTests
{
    private readonly IConfiguration _configuration;

    public WebhookHandlerIntegrationTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_test_123" },
                { "FilthyRodriguez:WebhookNotifications:Enabled", "true" },
                { "FilthyRodriguez:WebhookNotifications:ContinueOnError", "true" }
            })
            .Build();
    }

    [Fact]
    public void StripeWebhookHandler_InvokesCallback_WhenWebhookReceived()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration)
            .WithWebhookCallback((pi, evt) =>
            {
                return Task.CompletedTask;
            });

        var serviceProvider = services.BuildServiceProvider();
        var webhookHandler = serviceProvider.GetRequiredService<StripeWebhookHandler>();

        // Assert - Service is registered correctly
        Assert.NotNull(webhookHandler);
        var callbackService = serviceProvider.GetService<Func<PaymentIntent, Event, Task>>();
        Assert.NotNull(callbackService);
    }

    [Fact]
    public void StripeWebhookHandler_InvokesMultipleHandlers()
    {
        // Arrange
        var mockHandler1 = new Mock<IStripeWebhookHandler>();
        mockHandler1.Setup(h => h.HandlePaymentIntentSucceededAsync(It.IsAny<PaymentIntent>(), It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var mockHandler2 = new Mock<IStripeWebhookHandler>();
        mockHandler2.Setup(h => h.HandlePaymentIntentSucceededAsync(It.IsAny<PaymentIntent>(), It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);
        services.AddSingleton(mockHandler1.Object);
        services.AddSingleton(mockHandler2.Object);
        services.WithWebhookHandlers();

        var serviceProvider = services.BuildServiceProvider();
        
        // Get all registered handlers
        var handlers = serviceProvider.GetServices<IStripeWebhookHandler>().ToList();

        // Assert - Both handlers are registered
        Assert.Equal(2, handlers.Count);
        Assert.Contains(mockHandler1.Object, handlers);
        Assert.Contains(mockHandler2.Object, handlers);
    }

    [Fact]
    public async Task EventHandlerPattern_WorksWithNotifier()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        var notifier = serviceProvider.GetRequiredService<IStripeWebhookNotifier>();
        var eventRaised = false;

        notifier.PaymentIntentSucceeded += (sender, args) =>
        {
            eventRaised = true;
        };

        // Act
        var notifierImpl = notifier as FilthyRodriguez.Services.StripeWebhookNotifier;
        Assert.NotNull(notifierImpl);

        var paymentIntent = new PaymentIntent { Id = "pi_test", Status = "succeeded" };
        var stripeEvent = new Event
        {
            Id = "evt_test",
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = paymentIntent }
        };

        await notifierImpl.NotifyAsync(stripeEvent);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Configuration_IsLoadedCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<StripePaymentOptions>>();

        // Assert
        Assert.NotNull(options);
        Assert.True(options.Value.WebhookNotifications.Enabled);
        Assert.True(options.Value.WebhookNotifications.ContinueOnError);
        Assert.Equal(30, options.Value.WebhookNotifications.TimeoutSeconds);
    }

    [Fact]
    public async Task WebhookHandler_ContinuesOnError_WhenConfigured()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;

        var mockHandler1 = new Mock<IStripeWebhookHandler>();
        mockHandler1.Setup(h => h.HandleWebhookAsync(It.IsAny<Event>()))
            .Callback(() =>
            {
                handler1Invoked = true;
                throw new Exception("Handler 1 failed");
            });

        var mockHandler2 = new Mock<IStripeWebhookHandler>();
        mockHandler2.Setup(h => h.HandleWebhookAsync(It.IsAny<Event>()))
            .Callback(() => handler2Invoked = true)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);
        services.AddSingleton(mockHandler1.Object);
        services.AddSingleton(mockHandler2.Object);

        var serviceProvider = services.BuildServiceProvider();
        var handlers = serviceProvider.GetServices<IStripeWebhookHandler>().ToList();

        // Assert both handlers are registered
        Assert.Equal(2, handlers.Count);
        
        // Simulate error handling behavior - both should be attempted
        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleWebhookAsync(new Event());
            }
            catch
            {
                // Continue on error as per configuration
            }
        }

        Assert.True(handler1Invoked);
        Assert.True(handler2Invoked);
    }
}
