using FilthyRodriguez.Models;

namespace FilthyRodriguez.Tests;

public class RefundResponseTests
{
    [Fact]
    public void RefundResponse_CanSetProperties()
    {
        // Arrange & Act
        var response = new RefundResponse
        {
            Id = "re_test123",
            PaymentIntentId = "pi_test456",
            Status = "succeeded",
            Amount = 1000
        };

        // Assert
        Assert.Equal("re_test123", response.Id);
        Assert.Equal("pi_test456", response.PaymentIntentId);
        Assert.Equal("succeeded", response.Status);
        Assert.Equal(1000, response.Amount);
    }

    [Fact]
    public void RefundResponse_DefaultValues_AreEmpty()
    {
        // Arrange & Act
        var response = new RefundResponse();

        // Assert
        Assert.Equal(string.Empty, response.Id);
        Assert.Equal(string.Empty, response.PaymentIntentId);
        Assert.Equal(string.Empty, response.Status);
        Assert.Equal(0, response.Amount);
    }
}
