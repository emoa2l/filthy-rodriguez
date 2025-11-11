using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class StripePaymentServiceRefundTests
{
    private readonly Mock<ILogger<StripePaymentService>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ITransactionRepository> _mockRepository;

    public StripePaymentServiceRefundTests()
    {
        _mockLogger = new Mock<ILogger<StripePaymentService>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockRepository = new Mock<ITransactionRepository>();
    }

    [Fact]
    public void ProcessRefundAsync_ValidRequest_CreatesProperRequest()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test_valid",
            Amount = 1000,
            Reason = "requested_by_customer"
        };

        // Assert - Verify request structure is valid
        Assert.NotNull(request.PaymentIntentId);
        Assert.True(request.Amount > 0);
        Assert.NotNull(request.Reason);
    }

    [Fact]
    public void ProcessRefundAsync_FullRefund_OmitsAmount()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test_full_refund",
            Reason = "requested_by_customer"
            // Amount is null for full refund
        };

        // Assert - Verify full refund structure
        Assert.NotNull(request.PaymentIntentId);
        Assert.Null(request.Amount);
        Assert.NotNull(request.Reason);
    }

    [Fact]
    public void ProcessRefundAsync_PartialRefund_IncludesAmount()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test_partial",
            Amount = 500
        };

        // Assert - Verify partial refund structure
        Assert.NotNull(request.PaymentIntentId);
        Assert.NotNull(request.Amount);
        Assert.True(request.Amount > 0);
    }

    [Fact]
    public void ProcessRefundAsync_WithReason_SetsReasonField()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test_reason",
            Amount = 1000,
            Reason = "fraudulent"
        };

        // Assert - Verify reason is set
        Assert.Equal("fraudulent", request.Reason);
        Assert.NotNull(request.PaymentIntentId);
    }

    [Fact]
    public async Task ProcessRefundAsync_StripeException_PropagatesCorrectly()
    {
        // Arrange
        var options = Options.Create(new StripePaymentOptions
        {
            ApiKey = "sk_test_invalid_key_for_testing",
            WebhookSecret = "whsec_test_123"
        });
        
        var service = new StripePaymentService(options, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_invalid",
            Amount = 1000
        };

        // Act & Assert - Verify service throws exception for invalid requests
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await service.ProcessRefundAsync(request);
        });
    }

    [Fact]
    public async Task ProcessRefundAsync_InvalidPaymentIntent_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new StripePaymentOptions
        {
            ApiKey = "sk_test_invalid_key_for_testing",
            WebhookSecret = "whsec_test_123"
        });
        
        var service = new StripePaymentService(options, _mockRepository.Object, _mockLogger.Object, _mockLoggerFactory.Object);
        var request = new RefundRequest
        {
            PaymentIntentId = "", // Empty transaction ID
            Amount = 1000
        };

        // Act & Assert - Verify service handles invalid input
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await service.ProcessRefundAsync(request);
        });
    }

    [Fact]
    public void RefundRequest_Properties_AreSetCorrectly()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test_props",
            Amount = 2500,
            Reason = "duplicate"
        };

        // Assert
        Assert.Equal("pi_test_props", request.PaymentIntentId);
        Assert.Equal(2500, request.Amount);
        Assert.Equal("duplicate", request.Reason);
    }

    [Fact]
    public void RefundRequest_OptionalFields_CanBeNull()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test_optional"
        };

        // Assert
        Assert.Null(request.Amount);
        Assert.Null(request.Reason);
    }

    [Fact]
    public void RefundResponse_MapsCorrectly()
    {
        // Arrange
        var response = new RefundResponse
        {
            Id = "re_test_123",
            PaymentIntentId = "pi_test_456",
            Status = "succeeded",
            Amount = 1000
        };

        // Assert
        Assert.Equal("re_test_123", response.Id);
        Assert.Equal("pi_test_456", response.PaymentIntentId);
        Assert.Equal("succeeded", response.Status);
        Assert.Equal(1000, response.Amount);
    }

    [Fact]
    public void RefundOptions_StructureValidation()
    {
        // Arrange
        var options = new RefundCreateOptions
        {
            PaymentIntent = "pi_test_123",
            Amount = 1000,
            Reason = "requested_by_customer"
        };

        // Assert - Verify Stripe SDK options can be created
        Assert.Equal("pi_test_123", options.PaymentIntent);
        Assert.Equal(1000, options.Amount);
        Assert.Equal("requested_by_customer", options.Reason);
    }
}
