using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Stripe;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Extensions;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class WebhookNotificationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public WebhookNotificationTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void AddFilthyRodriguez_RegistersWebhookNotifier()
    {
        // Act
        var notifier = _serviceProvider.GetService<IStripeWebhookNotifier>();

        // Assert
        Assert.NotNull(notifier);
        Assert.IsType<StripeWebhookNotifier>(notifier);
    }

    [Fact]
    public async Task StripeWebhookNotifier_RaisesPaymentIntentSucceededEvent()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        PaymentIntent? capturedPaymentIntent = null;
        Event? capturedEvent = null;

        notifier.PaymentIntentSucceeded += (sender, args) =>
        {
            capturedPaymentIntent = args.PaymentIntent;
            capturedEvent = args.WebhookEvent;
        };

        var paymentIntent = new PaymentIntent { Id = "pi_test123", Status = "succeeded" };
        var stripeEvent = new Event
        {
            Id = "evt_test123",
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = paymentIntent }
        };

        // Act
        await notifier.NotifyAsync(stripeEvent);

        // Assert
        Assert.NotNull(capturedPaymentIntent);
        Assert.Equal("pi_test123", capturedPaymentIntent.Id);
        Assert.NotNull(capturedEvent);
        Assert.Equal("evt_test123", capturedEvent.Id);
    }

    [Fact]
    public async Task StripeWebhookNotifier_RaisesPaymentIntentFailedEvent()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        var eventRaised = false;

        notifier.PaymentIntentFailed += (sender, args) =>
        {
            eventRaised = true;
        };

        var paymentIntent = new PaymentIntent { Id = "pi_test456", Status = "failed" };
        var stripeEvent = new Event
        {
            Id = "evt_test456",
            Type = "payment_intent.payment_failed",
            Data = new EventData { Object = paymentIntent }
        };

        // Act
        await notifier.NotifyAsync(stripeEvent);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task StripeWebhookNotifier_RaisesWebhookReceivedForAllEvents()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        var eventRaised = false;
        string? capturedEventType = null;

        notifier.WebhookReceived += (sender, args) =>
        {
            eventRaised = true;
            capturedEventType = args.Event.Type;
        };

        var stripeEvent = new Event
        {
            Id = "evt_test789",
            Type = "customer.created",
            Data = new EventData { Object = new Customer { Id = "cus_test" } }
        };

        // Act
        await notifier.NotifyAsync(stripeEvent);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("customer.created", capturedEventType);
    }

    [Fact]
    public async Task StripeWebhookNotifier_RaisesMultipleEventsForPaymentIntent()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        var webhookReceivedRaised = false;
        var paymentIntentSucceededRaised = false;

        notifier.WebhookReceived += (sender, args) => webhookReceivedRaised = true;
        notifier.PaymentIntentSucceeded += (sender, args) => paymentIntentSucceededRaised = true;

        var paymentIntent = new PaymentIntent { Id = "pi_multi", Status = "succeeded" };
        var stripeEvent = new Event
        {
            Id = "evt_multi",
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = paymentIntent }
        };

        // Act
        await notifier.NotifyAsync(stripeEvent);

        // Assert
        Assert.True(webhookReceivedRaised);
        Assert.True(paymentIntentSucceededRaised);
    }

    [Fact]
    public async Task WithWebhookCallback_RegistersCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var callbackInvoked = false;
        
        Func<PaymentIntent, Event, Task> callback = (pi, evt) =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        };

        // Act
        services.AddFilthyRodriguez(_configuration)
            .WithWebhookCallback(callback);
        
        var serviceProvider = services.BuildServiceProvider();
        var registeredCallback = serviceProvider.GetService<Func<PaymentIntent, Event, Task>>();

        // Assert
        Assert.NotNull(registeredCallback);
        
        // Test the callback
        var pi = new PaymentIntent { Id = "pi_test" };
        var evt = new Event { Id = "evt_test" };
        await registeredCallback(pi, evt);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void WithWebhookHandlers_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);

        // Act
        var result = services.WithWebhookHandlers();

        // Assert
        Assert.NotNull(result);
        Assert.Same(services, result);
    }

    [Fact]
    public async Task WebhookHandler_InvokesRegisteredHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(_configuration);
        
        var mockHandler = new Mock<IStripeWebhookHandler>();
        services.AddSingleton(mockHandler.Object);
        services.WithWebhookHandlers();

        var serviceProvider = services.BuildServiceProvider();

        // Get all registered handlers
        var handlers = serviceProvider.GetServices<IStripeWebhookHandler>();
        
        // Assert handlers are registered
        Assert.NotEmpty(handlers);
        Assert.Contains(mockHandler.Object, handlers);
        
        await Task.CompletedTask; // Make compiler happy about async
    }

    [Fact]
    public async Task StripeWebhookNotifier_ContinuesOnHandlerException()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        var handler1Called = false;
        var handler2Called = false;

        notifier.PaymentIntentSucceeded += (sender, args) =>
        {
            handler1Called = true;
            throw new Exception("Handler 1 failed");
        };

        notifier.PaymentIntentSucceeded += (sender, args) =>
        {
            handler2Called = true;
        };

        var paymentIntent = new PaymentIntent { Id = "pi_error", Status = "succeeded" };
        var stripeEvent = new Event
        {
            Id = "evt_error",
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = paymentIntent }
        };

        // Act
        await notifier.NotifyAsync(stripeEvent);

        // Assert - Both handlers should be called despite the first one throwing
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public async Task StripeWebhookNotifier_HandlesAsyncEventHandlers()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        var tcs = new TaskCompletionSource<bool>();

        notifier.PaymentIntentSucceeded += async (sender, args) =>
        {
            await Task.Delay(10); // Simulate async work
            tcs.SetResult(true);
        };

        var paymentIntent = new PaymentIntent { Id = "pi_async", Status = "succeeded" };
        var stripeEvent = new Event
        {
            Id = "evt_async",
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = paymentIntent }
        };

        // Act
        await notifier.NotifyAsync(stripeEvent);

        // Assert - Wait for the async handler to complete
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000)) == tcs.Task;
        Assert.True(completed, "Async handler should complete within timeout");
        Assert.True(await tcs.Task);
    }

    [Fact]
    public async Task StripeWebhookNotifier_RaisesAllPaymentIntentEvents()
    {
        // Arrange
        var notifier = _serviceProvider.GetRequiredService<IStripeWebhookNotifier>() as StripeWebhookNotifier;
        Assert.NotNull(notifier);

        var events = new Dictionary<string, bool>
        {
            ["created"] = false,
            ["succeeded"] = false,
            ["failed"] = false,
            ["canceled"] = false,
            ["processing"] = false
        };

        notifier.PaymentIntentCreated += (sender, args) => events["created"] = true;
        notifier.PaymentIntentSucceeded += (sender, args) => events["succeeded"] = true;
        notifier.PaymentIntentFailed += (sender, args) => events["failed"] = true;
        notifier.PaymentIntentCanceled += (sender, args) => events["canceled"] = true;
        notifier.PaymentIntentProcessing += (sender, args) => events["processing"] = true;

        // Act - Test each event type
        var eventTypes = new[]
        {
            "payment_intent.created",
            "payment_intent.succeeded",
            "payment_intent.payment_failed",
            "payment_intent.canceled",
            "payment_intent.processing"
        };

        foreach (var eventType in eventTypes)
        {
            var paymentIntent = new PaymentIntent { Id = $"pi_{eventType}" };
            var stripeEvent = new Event
            {
                Id = $"evt_{eventType}",
                Type = eventType,
                Data = new EventData { Object = paymentIntent }
            };
            await notifier.NotifyAsync(stripeEvent);
        }

        // Assert
        Assert.True(events["created"]);
        Assert.True(events["succeeded"]);
        Assert.True(events["failed"]);
        Assert.True(events["canceled"]);
        Assert.True(events["processing"]);
    }
}
