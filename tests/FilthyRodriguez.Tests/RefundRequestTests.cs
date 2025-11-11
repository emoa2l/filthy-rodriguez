using FilthyRodriguez.Models;

namespace FilthyRodriguez.Tests;

public class RefundRequestTests
{
    [Fact]
    public void RefundRequest_CanSetProperties()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123",
            Amount = 1000,
            Reason = "requested_by_customer"
        };

        // Assert
        Assert.Equal("pi_test123", request.PaymentIntentId);
        Assert.Equal(1000, request.Amount);
        Assert.Equal("requested_by_customer", request.Reason);
    }

    [Fact]
    public void RefundRequest_AmountIsOptional()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123"
        };

        // Assert
        Assert.Equal("pi_test123", request.PaymentIntentId);
        Assert.Null(request.Amount);
    }

    [Fact]
    public void RefundRequest_ReasonIsOptional()
    {
        // Arrange & Act
        var request = new RefundRequest
        {
            PaymentIntentId = "pi_test123",
            Amount = 1000
        };

        // Assert
        Assert.Equal("pi_test123", request.PaymentIntentId);
        Assert.Equal(1000, request.Amount);
        Assert.Null(request.Reason);
    }
}
