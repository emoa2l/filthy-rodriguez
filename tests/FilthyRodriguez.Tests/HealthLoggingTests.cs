using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class HealthLoggingTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;

    public HealthLoggingTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
    }
    [Fact]
    public async Task GetHealthAsync_LogsHealthCheckStart()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var options = Options.Create(new StripePaymentOptions 
        { 
            ApiKey = "sk_test_valid_key",
            WebhookSecret = "whsec_123"
        });
        var service = new StripePaymentService(options, _mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);

        // Act
        try
        {
            await service.GetHealthAsync();
        }
        catch
        {
            // We expect this to fail due to invalid API key, but we're testing logging
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performing health check")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHealthAsync_WithoutApiKey_LogsWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var options = Options.Create(new StripePaymentOptions 
        { 
            ApiKey = "", // Empty API key
            WebhookSecret = "whsec_123"
        });
        var service = new StripePaymentService(options, _mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);

        // Act
        await service.GetHealthAsync();

        // Assert - Verify LogInformation for start
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performing health check")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Assert - Verify LogWarning for missing API key
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stripe API key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHealthAsync_WithInvalidApiKey_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var options = Options.Create(new StripePaymentOptions 
        { 
            ApiKey = "sk_test_invalid_key",
            WebhookSecret = "whsec_123"
        });
        var service = new StripePaymentService(options, _mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);

        // Act
        await service.GetHealthAsync();

        // Assert - Verify LogInformation for start
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performing health check")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Assert - Verify LogError is called (either for Stripe API error or unexpected error)
        // In test environments without internet, this may be an unexpected error
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Health check failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHealthAsync_WithNullApiKey_LogsWarningAndNoError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var options = Options.Create(new StripePaymentOptions 
        { 
            ApiKey = null!, // Null API key
            WebhookSecret = ""
        });
        var service = new StripePaymentService(options, _mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);

        // Act
        await service.GetHealthAsync();

        // Assert - Verify LogWarning is called
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stripe API key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Assert - Verify LogError is NOT called (no error should occur with missing key)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetHealthAsync_VerifiesLoggingOccursInCorrectOrder()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var logMessages = new List<string>();

        // Capture log messages
        mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((level, id, state, ex, formatter) =>
            {
                logMessages.Add(state.ToString()!);
            });

        var options = Options.Create(new StripePaymentOptions 
        { 
            ApiKey = "",
            WebhookSecret = "whsec_123"
        });
        var service = new StripePaymentService(options, _mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);

        // Act
        await service.GetHealthAsync();

        // Assert - Verify logging order
        Assert.Contains("Performing health check", logMessages[1]); // First log after initialization
        Assert.Contains("Stripe API key not configured", logMessages[2]); // Second log
    }
}
