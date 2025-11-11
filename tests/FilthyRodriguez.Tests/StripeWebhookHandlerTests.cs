using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Handlers;
using FilthyRodriguez.Services;
using System.Text;

namespace FilthyRodriguez.Tests;

public class StripeWebhookHandlerTests
{
    private readonly Mock<ILogger<StripeWebhookHandler>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IStripeWebhookNotifier> _mockNotifier;
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly StripePaymentOptions _options;

    public StripeWebhookHandlerTests()
    {
        _mockLogger = new Mock<ILogger<StripeWebhookHandler>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockNotifier = new Mock<IStripeWebhookNotifier>();
        _mockRepository = new Mock<ITransactionRepository>();
        _options = new StripePaymentOptions
        {
            ApiKey = "sk_test_123",
            WebhookSecret = "whsec_test_secret"
        };
    }

    [Fact]
    public void Constructor_InitializesHandler()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var handler = new StripeWebhookHandler(
            options,
            _mockNotifier.Object,
            Enumerable.Empty<IStripeWebhookHandler>(),
            _mockRepository.Object,
            _mockLogger.Object,
            _mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task HandleWebhookAsync_WithEmptyWebhookSecret_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(new StripePaymentOptions
        {
            ApiKey = "sk_test_123",
            WebhookSecret = "" // Empty webhook secret
        });
        var handler = new StripeWebhookHandler(
            options,
            _mockNotifier.Object,
            Enumerable.Empty<IStripeWebhookHandler>(),
            _mockRepository.Object,
            _mockLogger.Object, _mockLoggerFactory.Object);

        var context = new DefaultHttpContext();
        var requestBody = "{\"type\":\"payment_intent.succeeded\"}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        context.Request.Headers["Stripe-Signature"] = "test_signature";

        // Act
        var result = await handler.HandleWebhookAsync(context.Request, CancellationToken.None);

        // Assert
        Assert.Null(result); // Should return null for invalid signature
    }

    [Fact]
    public async Task HandleWebhookAsync_WithMissingStripeSignatureHeader_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(_options);
        var handler = new StripeWebhookHandler(
            options,
            _mockNotifier.Object,
            Enumerable.Empty<IStripeWebhookHandler>(),
            _mockRepository.Object,
            _mockLogger.Object, _mockLoggerFactory.Object);

        var context = new DefaultHttpContext();
        var requestBody = "{\"type\":\"payment_intent.succeeded\"}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        // No Stripe-Signature header

        // Act
        var result = await handler.HandleWebhookAsync(context.Request, CancellationToken.None);

        // Assert
        Assert.Null(result); // Should return null when signature header is missing
    }

    [Fact]
    public async Task HandleWebhookAsync_WithInvalidSignature_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(_options);
        var handler = new StripeWebhookHandler(
            options,
            _mockNotifier.Object,
            Enumerable.Empty<IStripeWebhookHandler>(),
            _mockRepository.Object,
            _mockLogger.Object, _mockLoggerFactory.Object);

        var context = new DefaultHttpContext();
        var requestBody = "{\"type\":\"payment_intent.succeeded\"}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        context.Request.Headers["Stripe-Signature"] = "invalid_signature";

        // Act
        var result = await handler.HandleWebhookAsync(context.Request, CancellationToken.None);

        // Assert
        Assert.Null(result); // Should return null for invalid signature
    }

    [Fact]
    public void GetEvent_WithNonExistentEventId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = "evt_nonexistent_" + Guid.NewGuid();

        // Act
        var result = StripeWebhookHandler.GetEvent(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRecentEvents_ReturnsEvents()
    {
        // Act
        var events = StripeWebhookHandler.GetRecentEvents(10);

        // Assert
        Assert.NotNull(events);
        // The collection might be empty or have events from other tests
        Assert.IsAssignableFrom<IEnumerable<Event>>(events);
    }

    [Fact]
    public void GetRecentEvents_WithCountParameter_ReturnsRequestedCount()
    {
        // Act
        var events = StripeWebhookHandler.GetRecentEvents(5);

        // Assert
        Assert.NotNull(events);
        Assert.True(events.Count() <= 5); // Should not return more than requested
    }
}
