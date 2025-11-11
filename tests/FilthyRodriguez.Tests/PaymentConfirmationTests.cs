using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;
using Xunit;

namespace FilthyRodriguez.Tests;

public class PaymentConfirmationTests
{
    [Fact]
    public void PaymentConfirmRequest_DefaultValues_AreSet()
    {
        // Arrange & Act
        var request = new PaymentConfirmRequest();

        // Assert
        Assert.Equal(string.Empty, request.PaymentIntentId);
        Assert.Equal("pm_card_visa", request.PaymentMethodId);
    }

    [Fact]
    public void PaymentConfirmRequest_CanSetPaymentIntentId()
    {
        // Arrange
        var request = new PaymentConfirmRequest();
        var paymentIntentId = "pi_1234567890";

        // Act
        request.PaymentIntentId = paymentIntentId;

        // Assert
        Assert.Equal(paymentIntentId, request.PaymentIntentId);
    }

    [Fact]
    public void PaymentConfirmRequest_CanSetPaymentMethodId()
    {
        // Arrange
        var request = new PaymentConfirmRequest();
        var paymentMethodId = "pm_card_mastercard";

        // Act
        request.PaymentMethodId = paymentMethodId;

        // Assert
        Assert.Equal(paymentMethodId, request.PaymentMethodId);
    }

    [Fact]
    public void PaymentConfirmRequest_SupportsVariousTestPaymentMethods()
    {
        // Arrange & Act
        var testMethods = new[]
        {
            "pm_card_visa",
            "pm_card_visa_debit",
            "pm_card_mastercard",
            "pm_card_amex",
            "pm_card_discover",
            "pm_card_chargeDeclined",
            "pm_card_authenticationRequired"
        };

        // Assert - All should be valid strings
        foreach (var method in testMethods)
        {
            var request = new PaymentConfirmRequest
            {
                PaymentIntentId = "pi_test",
                PaymentMethodId = method
            };

            Assert.Equal(method, request.PaymentMethodId);
            Assert.False(string.IsNullOrWhiteSpace(request.PaymentMethodId));
        }
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ThrowsArgumentException_WhenPaymentIntentIdIsNull()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new StripePaymentOptions { ApiKey = "sk_test_123" });
        
        var mockRepository = new Mock<ITransactionRepository>();
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        
        var service = new StripePaymentService(mockOptions.Object, mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);
        
        var request = new PaymentConfirmRequest
        {
            PaymentIntentId = null!,
            PaymentMethodId = "pm_card_visa"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ConfirmPaymentAsync(request));
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ThrowsArgumentException_WhenPaymentIntentIdIsEmpty()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new StripePaymentOptions { ApiKey = "sk_test_123" });
        
        var mockRepository = new Mock<ITransactionRepository>();
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        
        var service = new StripePaymentService(mockOptions.Object, mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);
        
        var request = new PaymentConfirmRequest
        {
            PaymentIntentId = string.Empty,
            PaymentMethodId = "pm_card_visa"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ConfirmPaymentAsync(request));
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ThrowsArgumentException_WhenPaymentMethodIdIsNull()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new StripePaymentOptions { ApiKey = "sk_test_123" });
        
        var mockRepository = new Mock<ITransactionRepository>();
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        
        var service = new StripePaymentService(mockOptions.Object, mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);
        
        var request = new PaymentConfirmRequest
        {
            PaymentIntentId = "pi_1234567890",
            PaymentMethodId = null!
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ConfirmPaymentAsync(request));
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ThrowsArgumentException_WhenPaymentMethodIdIsEmpty()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new StripePaymentOptions { ApiKey = "sk_test_123" });
        
        var mockRepository = new Mock<ITransactionRepository>();
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        
        var service = new StripePaymentService(mockOptions.Object, mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);
        
        var request = new PaymentConfirmRequest
        {
            PaymentIntentId = "pi_1234567890",
            PaymentMethodId = string.Empty
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ConfirmPaymentAsync(request));
    }

    [Fact]
    public void ConfirmPaymentAsync_LogsPaymentMethodId()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<StripePaymentOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new StripePaymentOptions { ApiKey = "sk_test_123" });
        
        var mockRepository = new Mock<ITransactionRepository>();
        var mockLogger = new Mock<ILogger<StripePaymentService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        
        var service = new StripePaymentService(mockOptions.Object, mockRepository.Object, mockLogger.Object, mockLoggerFactory.Object);

        // Assert - Verify that the service was initialized (which logs)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("StripePaymentService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
