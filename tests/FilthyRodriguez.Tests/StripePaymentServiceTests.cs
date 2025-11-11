using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class StripePaymentServiceTests
{
    private readonly Mock<ILogger<StripePaymentService>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly StripePaymentOptions _options;

    public StripePaymentServiceTests()
    {
        _mockLogger = new Mock<ILogger<StripePaymentService>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockRepository = new Mock<ITransactionRepository>();
        _options = new StripePaymentOptions
        {
            ApiKey = "sk_test_fake_key_for_testing",
            WebhookSecret = "whsec_test_fake_secret"
        };
    }

    [Fact]
    public void Constructor_InitializesService_WithValidOptions()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);

        // Act
        var service = new StripePaymentService(mockOptions.Object, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(service);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("StripePaymentService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentAsync_LogsInformation_WhenCalled()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);
        var service = new StripePaymentService(mockOptions.Object, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);

        var request = new PaymentRequest
        {
            Amount = 1000,
            Currency = "usd",
            Description = "Test payment"
        };

        // Act & Assert
        // Note: This will call the real Stripe API with a fake key and will fail
        // In a real scenario, we would need to mock the Stripe SDK or use a test mode key
        await Assert.ThrowsAnyAsync<Exception>(() => service.CreatePaymentAsync(request));

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating payment intent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentStatusAsync_LogsInformation_WhenCalled()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);
        var service = new StripePaymentService(mockOptions.Object, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);

        var paymentIntentId = "pi_test123";

        // Act & Assert
        // Note: This will call the real Stripe API with a fake key and will fail
        await Assert.ThrowsAnyAsync<Exception>(() => service.GetPaymentStatusAsync(paymentIntentId));

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving payment status")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessRefundAsync_LogsInformation_WhenCalled()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);
        var service = new StripePaymentService(mockOptions.Object, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);

        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123",
            Amount = 500,
            Reason = "requested_by_customer"
        };

        // Act & Assert
        // Note: This will call the real Stripe API with a fake key and will fail
        await Assert.ThrowsAnyAsync<Exception>(() => service.ProcessRefundAsync(request));

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing refund")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessRefundAsync_LogsError_WhenStripeExceptionOccurs()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);
        var service = new StripePaymentService(mockOptions.Object, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);

        var request = new RefundRequest
        {
            PaymentIntentId = "pi_invalid",
            Amount = 500
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => service.ProcessRefundAsync(request));

        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
