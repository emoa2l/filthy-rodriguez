using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using FilthyRodriguez.Handlers;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;
using System.Net.WebSockets;
using System.Text.Json;

namespace FilthyRodriguez.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapStripePaymentEndpoints(this IEndpointRouteBuilder endpoints, string basePath = "/api/stripe")
    {
        var group = endpoints.MapGroup(basePath);

        // POST /api/stripe/payment - Create payment
        group.MapPost("/payment", async (HttpContext context, IStripePaymentService paymentService, ILogger<IStripePaymentService> logger) =>
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.LogInformation("Payment creation request received from {RemoteIpAddress}", remoteIp);
            
            var request = await context.Request.ReadFromJsonAsync<PaymentRequest>();
            if (request == null)
            {
                logger.LogWarning("Invalid payment request received from {RemoteIpAddress} - request body is null", 
                    remoteIp);
                return Results.BadRequest(new { error = "Invalid request body" });
            }

            try
            {
                logger.LogDebug("Processing payment request: Amount={Amount} {Currency}, Description={Description}", 
                    request.Amount, request.Currency, request.Description);
                
                var response = await paymentService.CreatePaymentAsync(request, context.RequestAborted);
                
                logger.LogInformation("Payment created successfully. PaymentIntentId={PaymentIntentId}, Status={Status}", 
                    response.Id, response.Status);
                
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing payment request. Amount={Amount} {Currency}", 
                    request.Amount, request.Currency);
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // GET /api/stripe/status/{id} - Get payment status
        group.MapGet("/status/{id}", async (string id, IStripePaymentService paymentService, ILogger<IStripePaymentService> logger, CancellationToken cancellationToken) =>
        {
            // Validate and sanitize the ID to prevent log forging
            if (string.IsNullOrWhiteSpace(id))
            {
                logger.LogWarning("Payment status request with empty ID");
                return Results.BadRequest(new { error = "Payment intent ID is required" });
            }
            
            var sanitizedId = id.Replace("\n", "").Replace("\r", "");
            logger.LogInformation("Payment status request received. PaymentIntentId={PaymentIntentId}", sanitizedId);
            
            try
            {
                var status = await paymentService.GetPaymentStatusAsync(id, cancellationToken);
                
                logger.LogInformation("Payment status retrieved successfully. PaymentIntentId={PaymentIntentId}, Status={Status}", 
                    sanitizedId, status.Status);
                
                return Results.Ok(status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving payment status. PaymentIntentId={PaymentIntentId}", sanitizedId);
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // POST /api/stripe/confirm - Confirm payment with test card
        group.MapPost("/confirm", async (HttpContext context, IStripePaymentService paymentService, ILogger<IStripePaymentService> logger) =>
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.LogInformation("Payment confirmation request received from {RemoteIpAddress}", remoteIp);
            
            var request = await context.Request.ReadFromJsonAsync<PaymentConfirmRequest>();
            if (request == null)
            {
                logger.LogWarning("Invalid payment confirmation request received from {RemoteIpAddress} - request body is null", 
                    remoteIp);
                return Results.BadRequest(new { error = "Invalid request body" });
            }

            try
            {
                logger.LogDebug("Processing payment confirmation request: PaymentIntentId={PaymentIntentId}", 
                    request.PaymentIntentId);
                
                var response = await paymentService.ConfirmPaymentAsync(request, context.RequestAborted);
                
                logger.LogInformation("Payment confirmed successfully. PaymentIntentId={PaymentIntentId}, Status={Status}", 
                    response.Id, response.Status);
                
                // Notify WebSocket clients about the status update
                var update = new
                {
                    id = response.Id,
                    status = response.Status,
                    amount = response.Amount,
                    currency = response.Currency
                };
                await StripeWebSocketHandler.NotifyPaymentUpdateAsync(response.Id, update);
                
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid payment confirmation parameters. PaymentIntentId={PaymentIntentId}", 
                    request?.PaymentIntentId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to confirm payment. PaymentIntentId={PaymentIntentId}", 
                    request?.PaymentIntentId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing payment confirmation. PaymentIntentId={PaymentIntentId}", 
                    request?.PaymentIntentId);
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        // GET /api/stripe/health - Health check endpoint
        group.MapGet("/health", async (IStripePaymentService paymentService, ILogger<IStripePaymentService> logger, CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Health check requested");
            var health = await paymentService.GetHealthAsync(cancellationToken);

            // Return 503 Service Unavailable if unhealthy, otherwise 200 OK
            if (health.Status == "unhealthy")
            {
                logger.LogWarning("Health check failed: {HealthStatus}", health.Status);
                return Results.Json(health, statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            logger.LogInformation("Health check succeeded: {HealthStatus}", health.Status);
            return Results.Ok(health);
        });

        // POST /api/stripe/refund - Process refund
        group.MapPost("/refund", async (HttpContext context, IStripePaymentService paymentService, ILogger<IStripePaymentService> logger) =>
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.LogInformation("Refund request received from {RemoteIpAddress}", remoteIp);
            
            var request = await context.Request.ReadFromJsonAsync<RefundRequest>();
            if (request == null)
            {
                logger.LogWarning("Invalid refund request received from {RemoteIpAddress} - request body is null", 
                    remoteIp);
                return Results.BadRequest(new { error = "Invalid request body" });
            }

            try
            {
                logger.LogInformation("Processing refund request. PaymentIntentId={PaymentIntentId}, Amount={Amount}, Reason={Reason}", 
                    request.PaymentIntentId, request.Amount, request.Reason);
                
                var response = await paymentService.ProcessRefundAsync(request, context.RequestAborted);

                // Notify WebSocket clients about the refund
                var update = new
                {
                    id = response.PaymentIntentId,
                    status = $"refunded_{response.Status}",
                    refundId = response.Id,
                    refundAmount = response.Amount,
                    refundStatus = response.Status
                };
                await StripeWebSocketHandler.NotifyPaymentUpdateAsync(response.PaymentIntentId, update);

                logger.LogInformation("Refund processed successfully. RefundId={RefundId}, PaymentIntentId={PaymentIntentId}, Amount={Amount}",
                    response.Id, response.PaymentIntentId, response.Amount);

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid refund request parameters. PaymentIntentId={PaymentIntentId}", 
                    request?.PaymentIntentId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to process refund. PaymentIntentId={PaymentIntentId}, Reason={Reason}", 
                    request?.PaymentIntentId, ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing refund request. PaymentIntentId={PaymentIntentId}", 
                    request?.PaymentIntentId);
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        // POST /api/stripe/webhook - Handle Stripe webhooks
        group.MapPost("/webhook", async (HttpContext context, StripeWebhookHandler webhookHandler, ILogger<StripeWebhookHandler> logger) =>
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.LogInformation("Webhook request received from {RemoteIpAddress}", remoteIp);
            
            var stripeEvent = await webhookHandler.HandleWebhookAsync(context.Request, context.RequestAborted);
            
            if (stripeEvent == null)
            {
                logger.LogWarning("Webhook request from {RemoteIpAddress} rejected due to invalid signature", remoteIp);
                return Results.BadRequest(new { error = "Invalid webhook signature" });
            }

            logger.LogInformation("Webhook event accepted. EventType={EventType}, EventId={EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            // Notify WebSocket clients if it's a payment intent event
            if (stripeEvent.Type.StartsWith("payment_intent."))
            {
                var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                if (paymentIntent != null)
                {
                    logger.LogInformation("Processing payment_intent webhook event. PaymentIntentId: {PaymentIntentId}, EventType: {EventType}", 
                        paymentIntent.Id, stripeEvent.Type);
                    
                    var update = new
                    {
                        id = paymentIntent.Id,
                        status = paymentIntent.Status,
                        amount = paymentIntent.Amount,
                        currency = paymentIntent.Currency
                    };
                    await StripeWebSocketHandler.NotifyPaymentUpdateAsync(paymentIntent.Id, update);
                }
            }

            // Notify WebSocket clients if it's a refund event
            if (stripeEvent.Type.StartsWith("charge.refund"))
            {
                var refund = stripeEvent.Data.Object as Stripe.Refund;
                if (refund != null)
                {
                    logger.LogInformation("Processing refund webhook event. RefundId: {RefundId}, EventType: {EventType}, PaymentIntentId: {PaymentIntentId}", 
                        refund.Id, stripeEvent.Type, refund.PaymentIntentId);
                    
                    var refundUpdate = new
                    {
                        type = "refund_update",
                        refundId = refund.Id,
                        paymentIntentId = refund.PaymentIntentId,
                        status = refund.Status,
                        amount = refund.Amount
                    };
                    
                    // Notify clients subscribed to the payment intent
                    if (!string.IsNullOrEmpty(refund.PaymentIntentId))
                    {
                        await StripeWebSocketHandler.NotifyPaymentUpdateAsync(refund.PaymentIntentId, refundUpdate);
                    }
                }
            }

            logger.LogInformation("Webhook event processed successfully. EventType={EventType}, EventId={EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            return Results.Ok(new { received = true });
        })
        .DisableAntiforgery(); // Webhooks use signature-based validation, not antiforgery tokens

        return endpoints;
    }

    public static IEndpointRouteBuilder MapStripeWebSocket(this IEndpointRouteBuilder endpoints, string path = "/stripe/ws")
    {
        endpoints.Map(path, async (HttpContext context, StripeWebSocketHandler wsHandler, ILogger<StripeWebSocketHandler> logger) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                logger.LogInformation("WebSocket request received");
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await wsHandler.HandleWebSocketAsync(webSocket, context.RequestAborted);
            }
            else
            {
                logger.LogWarning("Non-WebSocket request received at WebSocket endpoint");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });

        return endpoints;
    }
}
