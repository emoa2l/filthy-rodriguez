using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FilthyRodriguez.Handlers;

public class StripeWebSocketHandler
{
    private readonly ILogger<StripeWebSocketHandler> _logger;
    private static readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _paymentSubscriptions = new();
    private static ILogger<StripeWebSocketHandler>? _staticLogger;

    public StripeWebSocketHandler(ILogger<StripeWebSocketHandler> logger)
    {
        _logger = logger;
        _staticLogger ??= logger; // Set static logger for use in static methods
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);
        
        _logger.LogInformation("WebSocket connection established. ConnectionId: {ConnectionId}", connectionId);

        try
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket close message received. ConnectionId: {ConnectionId}", connectionId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessageAsync(connectionId, message, webSocket, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket connection. ConnectionId: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            RemoveSubscriptions(connectionId);
            _logger.LogInformation("WebSocket connection closed. ConnectionId: {ConnectionId}", connectionId);
        }
    }

    private async Task ProcessMessageAsync(string connectionId, string message, WebSocket webSocket, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(message);
            
            if (json.TryGetProperty("action", out var action))
            {
                var actionValue = action.GetString();
                
                if (actionValue == "subscribe" && json.TryGetProperty("paymentId", out var paymentId))
                {
                    var id = paymentId.GetString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        if (!_paymentSubscriptions.ContainsKey(id))
                        {
                            _paymentSubscriptions.TryAdd(id, new HashSet<string>());
                        }
                        _paymentSubscriptions[id].Add(connectionId);

                        _logger.LogInformation("Client subscribed to payment updates. ConnectionId: {ConnectionId}, PaymentIntentId: {PaymentIntentId}", 
                            connectionId, id);

                        var response = new { type = "subscribed", paymentId = id };
                        await SendMessageAsync(webSocket, response, cancellationToken);
                    }
                }
                else if (actionValue == "unsubscribe" && json.TryGetProperty("paymentId", out var unsubPaymentId))
                {
                    var id = unsubPaymentId.GetString();
                    if (!string.IsNullOrEmpty(id) && _paymentSubscriptions.ContainsKey(id))
                    {
                        _paymentSubscriptions[id].Remove(connectionId);
                        
                        _logger.LogInformation("Client unsubscribed from payment updates. ConnectionId: {ConnectionId}, PaymentIntentId: {PaymentIntentId}", 
                            connectionId, id);
                        
                        var response = new { type = "unsubscribed", paymentId = id };
                        await SendMessageAsync(webSocket, response, cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid WebSocket message format received. ConnectionId: {ConnectionId}", connectionId);
        }
    }

    public static async Task NotifyPaymentUpdateAsync(string paymentId, object update)
    {
        if (!_paymentSubscriptions.TryGetValue(paymentId, out var subscribers))
        {
            _staticLogger?.LogDebug("No subscribers found for payment update. PaymentIntentId: {PaymentIntentId}", paymentId);
            return;
        }

        _staticLogger?.LogInformation("Notifying {SubscriberCount} subscriber(s) of payment update. PaymentIntentId: {PaymentIntentId}", 
            subscribers.Count, paymentId);

        var message = new { type = "payment_update", paymentId, data = update };
        
        foreach (var connectionId in subscribers.ToList())
        {
            if (_connections.TryGetValue(connectionId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await SendMessageAsync(webSocket, message, CancellationToken.None);
                    _staticLogger?.LogDebug("Payment update sent successfully. ConnectionId: {ConnectionId}, PaymentIntentId: {PaymentIntentId}", 
                        connectionId, paymentId);
                }
                catch (Exception ex)
                {
                    _staticLogger?.LogWarning(ex, "Failed to send payment update to WebSocket client. ConnectionId: {ConnectionId}, PaymentIntentId: {PaymentIntentId}", 
                        connectionId, paymentId);
                }
            }
        }
    }

    private static async Task SendMessageAsync(WebSocket webSocket, object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private void RemoveSubscriptions(string connectionId)
    {
        foreach (var subscriptions in _paymentSubscriptions.Values)
        {
            subscriptions.Remove(connectionId);
        }
    }
}
