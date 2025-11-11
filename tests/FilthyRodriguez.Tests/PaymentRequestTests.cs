using FilthyRodriguez.Models;

namespace FilthyRodriguez.Tests;

public class PaymentRequestTests
{
    [Fact]
    public void PaymentRequest_DefaultCurrency_IsUsd()
    {
        // Arrange & Act
        var request = new PaymentRequest { Amount = 1000 };

        // Assert
        Assert.Equal("usd", request.Currency);
    }

    [Fact]
    public void PaymentRequest_CanSetProperties()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act
        var request = new PaymentRequest
        {
            Amount = 2000,
            Currency = "eur",
            Description = "Test payment",
            Metadata = metadata
        };

        // Assert
        Assert.Equal(2000, request.Amount);
        Assert.Equal("eur", request.Currency);
        Assert.Equal("Test payment", request.Description);
        Assert.Equal(metadata, request.Metadata);
    }
}
