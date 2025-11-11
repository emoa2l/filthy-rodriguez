using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Extensions;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFilthyRodriguez_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();

        // Act
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var paymentService = serviceProvider.GetService<IStripePaymentService>();
        Assert.NotNull(paymentService);
        Assert.IsType<StripePaymentService>(paymentService);
    }

    [Fact]
    public void AddFilthyRodriguez_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FilthyRodriguez:ApiKey", "sk_test_123" },
                { "FilthyRodriguez:WebhookSecret", "whsec_123" }
            })
            .Build();

        // Act
        services.AddFilthyRodriguez(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<StripePaymentOptions>>();
        Assert.NotNull(options);
        Assert.Equal("sk_test_123", options.Value.ApiKey);
        Assert.Equal("whsec_123", options.Value.WebhookSecret);
    }
}
