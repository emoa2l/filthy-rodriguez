using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Stripe;
using FilthyRodriguez.Abstractions;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Handlers;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Extensions;

/// <summary>
/// Extension methods for registering Stripe Payment Plugin services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Stripe Payment Plugin services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFilthyRodriguez(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StripePaymentOptions>(configuration.GetSection(StripePaymentOptions.SectionName));
        services.Configure<MetricsOptions>(configuration.GetSection("StripePayment:Metrics"));
        
        // Register in-memory repository by default
        services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
        
        // Register event publisher (empty listener list by default)
        services.AddSingleton<PaymentEventPublisher>();
        
        services.AddSingleton<IStripePaymentService, StripePaymentService>();
        services.AddSingleton<IStripeWebhookNotifier, StripeWebhookNotifier>();
        services.AddSingleton<StripeWebhookHandler>();
        services.AddSingleton<StripeWebSocketHandler>();
        
        // Register metrics services (default with logging sink)
        services.AddSingleton<IMetricsSink, LoggingMetricsSink>();
        services.AddSingleton<IPaymentMetrics, PaymentMetricsService>();
        services.AddSingleton<IPaymentEventListener, MetricsEventListener>();
        
        // Validate configuration on first service creation
        services.AddSingleton<StripeConfigurationValidator>();
        
        return services;
    }
    
    /// <summary>
    /// Registers a callback function to be invoked when payment intent webhooks are received
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="callback">Callback function to invoke for payment intent events</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection WithWebhookCallback(
        this IServiceCollection services,
        Func<PaymentIntent, Event, Task> callback)
    {
        // Register the callback as a singleton
        services.TryAddSingleton(callback);
        return services;
    }
    
    /// <summary>
    /// Enables automatic discovery and registration of IStripeWebhookHandler implementations.
    /// This scans the service collection for all registered IStripeWebhookHandler services.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection WithWebhookHandlers(this IServiceCollection services)
    {
        // The handlers are injected via IEnumerable<IStripeWebhookHandler> in StripeWebhookHandler
        // So we don't need to do anything special here - DI will automatically collect all registered handlers
        // This method exists for discoverability and to match the API design in the issue
        return services;
    }
    
    /// <summary>
    /// Registers a payment event listener to receive notifications about all payment operations
    /// </summary>
    /// <typeparam name="TListener">The event listener implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPaymentEventListener<TListener>(this IServiceCollection services)
        where TListener : class, IPaymentEventListener
    {
        services.AddSingleton<IPaymentEventListener, TListener>();
        return services;
    }
    
    /// <summary>
    /// Registers a custom metrics sink for emitting metrics to external systems
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection WithMetricsSink<T>(this IServiceCollection services) where T : class, IMetricsSink
    {
        services.AddSingleton<IMetricsSink, T>();
        return services;
    }
    
    /// <summary>
    /// Disables metrics collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection WithoutMetrics(this IServiceCollection services)
    {
        services.Configure<MetricsOptions>(options => options.Enabled = false);
        return services;
    }
}

/// <summary>
/// Internal class to validate Stripe configuration on startup
/// </summary>
internal class StripeConfigurationValidator
{
    public StripeConfigurationValidator(IConfiguration configuration, ILogger<StripeConfigurationValidator> logger)
    {
        var options = configuration.GetSection(StripePaymentOptions.SectionName).Get<StripePaymentOptions>();
        
        if (options == null || string.IsNullOrEmpty(options.ApiKey))
        {
            logger.LogError("Stripe API Key is not configured. Please set StripePayment:ApiKey in configuration.");
        }
        else
        {
            logger.LogInformation("Stripe Payment Plugin configured successfully");
        }
        
        if (options == null || string.IsNullOrEmpty(options.WebhookSecret))
        {
            logger.LogWarning("Stripe Webhook Secret is not configured. Webhook signature verification will fail.");
        }
    }
}
