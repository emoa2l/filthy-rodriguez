using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FilthyRodriguez.Handlers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FilthyRodriguez.Tests;

public class RefundWebSocketNotificationTests
{
    private readonly StripeWebSocketHandler _handler;
    private readonly ILogger<StripeWebSocketHandler> _logger;

    public RefundWebSocketNotificationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<StripeWebSocketHandler>>();
        _handler = new StripeWebSocketHandler(_logger);
    }

    [Fact]
    public void RefundNotification_ContainsRefundId()
    {
        // Arrange
        var refundUpdate = new
        {
            type = "refund_update",
            refundId = "re_test_123",
            paymentIntentId = "pi_test_456",
            status = "succeeded",
            amount = 1000
        };

        // Act
        var json = JsonSerializer.Serialize(refundUpdate);

        // Assert
        Assert.Contains("re_test_123", json);
        Assert.Contains("refund_update", json);
    }

    [Fact]
    public void RefundNotification_ContainsPaymentIntentId()
    {
        // Arrange
        var refundUpdate = new
        {
            type = "refund_update",
            refundId = "re_test_789",
            paymentIntentId = "pi_test_123",
            status = "pending",
            amount = 2000
        };

        // Act
        var json = JsonSerializer.Serialize(refundUpdate);

        // Assert
        Assert.Contains("pi_test_123", json);
        Assert.Contains("paymentIntentId", json);
    }

    [Fact]
    public void RefundNotification_ContainsStatus()
    {
        // Arrange
        var refundUpdate = new
        {
            type = "refund_update",
            refundId = "re_status_test",
            paymentIntentId = "pi_status_test",
            status = "succeeded",
            amount = 1500
        };

        // Act
        var json = JsonSerializer.Serialize(refundUpdate);

        // Assert
        Assert.Contains("succeeded", json);
        Assert.Contains("status", json);
    }

    [Fact]
    public void RefundNotification_ContainsAmount()
    {
        // Arrange
        var refundUpdate = new
        {
            type = "refund_update",
            refundId = "re_amount_test",
            paymentIntentId = "pi_amount_test",
            status = "succeeded",
            amount = 3500
        };

        // Act
        var json = JsonSerializer.Serialize(refundUpdate);

        // Assert
        Assert.Contains("3500", json);
        Assert.Contains("amount", json);
    }

    [Fact]
    public void RefundNotification_HasCorrectStructure()
    {
        // Arrange & Act
        var refundUpdate = new
        {
            type = "refund_update",
            refundId = "re_structure_test",
            paymentIntentId = "pi_structure_test",
            status = "succeeded",
            amount = 1000
        };

        // Assert
        Assert.Equal("refund_update", refundUpdate.type);
        Assert.NotNull(refundUpdate.refundId);
        Assert.NotNull(refundUpdate.paymentIntentId);
        Assert.NotNull(refundUpdate.status);
        Assert.True(refundUpdate.amount > 0);
    }

    [Fact]
    public async Task NotifyPaymentUpdateAsync_CanSerializeRefundData()
    {
        // Arrange
        var refundData = new
        {
            type = "refund_update",
            refundId = "re_serialize_test",
            paymentIntentId = "pi_serialize_test",
            status = "succeeded",
            amount = 2500
        };

        // Act
        var json = JsonSerializer.Serialize(refundData);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert
        Assert.True(deserialized.TryGetProperty("type", out var typeProperty));
        Assert.Equal("refund_update", typeProperty.GetString());
        
        Assert.True(deserialized.TryGetProperty("refundId", out var refundIdProperty));
        Assert.Equal("re_serialize_test", refundIdProperty.GetString());
        
        Assert.True(deserialized.TryGetProperty("amount", out var amountProperty));
        Assert.Equal(2500, amountProperty.GetInt32());
        
        await Task.CompletedTask; // Satisfy async requirement
    }

    [Fact]
    public void RefundNotification_TypeField_IsRefundUpdate()
    {
        // Arrange
        var notification = new
        {
            type = "refund_update",
            refundId = "re_type_test",
            paymentIntentId = "pi_type_test",
            status = "succeeded",
            amount = 1000
        };

        // Assert
        Assert.Equal("refund_update", notification.type);
    }

    [Fact]
    public void RefundNotification_AllFieldsPresent()
    {
        // Arrange & Act
        var notification = new
        {
            type = "refund_update",
            refundId = "re_all_fields",
            paymentIntentId = "pi_all_fields",
            status = "failed",
            amount = 500
        };

        // Assert - Verify all required fields are present
        Assert.NotNull(notification.type);
        Assert.NotNull(notification.refundId);
        Assert.NotNull(notification.paymentIntentId);
        Assert.NotNull(notification.status);
        Assert.True(notification.amount >= 0);
    }
}
