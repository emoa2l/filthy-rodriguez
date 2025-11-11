using FilthyRodriguez.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using FilthyRodriguez.Handlers;
using System.Net.WebSockets;

namespace FilthyRodriguez.Tests;

public class StripeWebSocketHandlerTests
{
    private readonly Mock<ILogger<StripeWebSocketHandler>> _mockLogger;

    public StripeWebSocketHandlerTests()
    {
        _mockLogger = new Mock<ILogger<StripeWebSocketHandler>>();
    }

    [Fact]
    public void Constructor_InitializesHandler_Successfully()
    {
        // Act
        var handler = new StripeWebSocketHandler(_mockLogger.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task HandleWebSocketAsync_LogsConnection_WhenCalled()
    {
        // Arrange
        var handler = new StripeWebSocketHandler(_mockLogger.Object);
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act
        await handler.HandleWebSocketAsync(mockWebSocket.Object, CancellationToken.None);

        // Assert
        // Verify connection was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WebSocket connection established")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify connection closed was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WebSocket connection closed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebSocketAsync_HandlesException_Gracefully()
    {
        // Arrange
        var handler = new StripeWebSocketHandler(_mockLogger.Object);
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        mockWebSocket.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await handler.HandleWebSocketAsync(mockWebSocket.Object, CancellationToken.None);

        // Assert
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in WebSocket connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify connection closed was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WebSocket connection closed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPaymentUpdateAsync_DoesNotThrow_WithNoSubscribers()
    {
        // Arrange
        var paymentId = "pi_test123";
        var update = new { status = "succeeded", amount = 1000 };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await StripeWebSocketHandler.NotifyPaymentUpdateAsync(paymentId, update));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyPaymentUpdateAsync_HandlesNullUpdate_Gracefully()
    {
        // Arrange
        var paymentId = "pi_test123";
        object? update = null;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await StripeWebSocketHandler.NotifyPaymentUpdateAsync(paymentId, update!));

        // Note: Should not throw, even with null update
        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyPaymentUpdateAsync_HandlesEmptyPaymentId_Gracefully()
    {
        // Arrange
        var paymentId = "";
        var update = new { status = "succeeded" };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await StripeWebSocketHandler.NotifyPaymentUpdateAsync(paymentId, update));

        Assert.Null(exception);
    }

    [Fact]
    public void Handler_SupportsMultipleInstances()
    {
        // Arrange & Act
        var handler1 = new StripeWebSocketHandler(_mockLogger.Object);
        var handler2 = new StripeWebSocketHandler(_mockLogger.Object);

        // Assert
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotEqual(handler1, handler2);
    }
}
