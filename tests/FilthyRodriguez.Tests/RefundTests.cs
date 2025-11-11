using FilthyRodriguez.Models;

namespace FilthyRodriguez.Tests;

public class RefundTests
{
    [Fact]
    public void RefundRequest_HasRequiredProperties()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123",
            Amount = 1000,
            Reason = "requested_by_customer",
            Metadata = new Dictionary<string, string> { { "order_id", "12345" } }
        };

        // Assert
        Assert.Equal("pi_test123", request.PaymentIntentId);
        Assert.Equal(1000, request.Amount);
        Assert.Equal("requested_by_customer", request.Reason);
        Assert.NotNull(request.Metadata);
        Assert.Single(request.Metadata);
    }

    [Fact]
    public void RefundRequest_CanSetPartialRefund()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123",
            Amount = 500 // Partial refund
        };

        // Assert
        Assert.Equal(500, request.Amount);
    }

    [Fact]
    public void RefundRequest_FullRefund_AmountIsNull()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123"
            // Amount not specified = full refund
        };

        // Assert
        Assert.Null(request.Amount);
    }

    [Fact]
    public void RefundResponse_HasRequiredProperties()
    {
        // Arrange & Act
        var response = new RefundResponse
        {
            Id = "re_test123",
            PaymentIntentId = "pi_test456",
            Status = "succeeded",
            Amount = 1000,
            Currency = "usd",
            Reason = "requested_by_customer",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("re_test123", response.Id);
        Assert.Equal("pi_test456", response.PaymentIntentId);
        Assert.Equal("succeeded", response.Status);
        Assert.Equal(1000, response.Amount);
        Assert.Equal("usd", response.Currency);
        Assert.Equal("requested_by_customer", response.Reason);
        Assert.NotEqual(default(DateTime), response.CreatedAt);
    }

    [Fact]
    public void RefundRequest_WithMetadata_StoresCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "order_id", "12345" },
            { "customer_id", "cust_abc" },
            { "refund_reason", "defective_product" }
        };

        // Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123",
            Amount = 2500,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(3, request.Metadata.Count);
        Assert.Equal("12345", request.Metadata["order_id"]);
        Assert.Equal("cust_abc", request.Metadata["customer_id"]);
        Assert.Equal("defective_product", request.Metadata["refund_reason"]);
    }

    [Fact]
    public void RefundResponse_SupportsAllStripeStatuses()
    {
        // Arrange
        var statuses = new[] { "succeeded", "pending", "failed", "canceled" };

        // Act & Assert
        foreach (var status in statuses)
        {
            var response = new RefundResponse
            {
                Id = "re_test",
                PaymentIntentId = "pi_test",
                Status = status,
                Amount = 1000,
                Currency = "usd",
                CreatedAt = DateTime.UtcNow
            };

            Assert.Equal(status, response.Status);
        }
    }

    [Fact]
    public void RefundRequest_EmptyPaymentIntentId_IsValid()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = string.Empty
        };

        // Assert
        Assert.Empty(request.PaymentIntentId);
        // Validation should happen at service layer, not model
    }
}
