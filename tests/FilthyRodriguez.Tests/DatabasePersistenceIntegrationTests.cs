using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Data;
using FilthyRodriguez.Extensions;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

/// <summary>
/// End-to-end integration tests verifying the complete database persistence flow
/// </summary>
public class DatabasePersistenceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly StripePaymentDbContext _dbContext;

    public DatabasePersistenceIntegrationTests()
    {
        // Setup configuration with shared in-memory SQLite database
        // Using DataSource=:memory:?cache=shared allows multiple connections to share the same in-memory database
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FilthyRodriguez:ApiKey"] = "sk_test_fake_key_for_testing",
                ["FilthyRodriguez:WebhookSecret"] = "whsec_fake_secret_for_testing",
                ["FilthyRodriguez:Database:Enabled"] = "true",
                ["FilthyRodriguez:Database:ConnectionString"] = "DataSource=test_db_shared;Mode=Memory;Cache=Shared",
                ["FilthyRodriguez:Database:Provider"] = "SQLite"
            })
            .Build();

        // Setup services
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add Stripe plugin with EF
        services.AddFilthyRodriguez(configuration)
            .WithEntityFramework();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<StripePaymentDbContext>();
        
        // Initialize in-memory database and keep connection open
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void ServiceRegistration_ConfiguresEfCoreRepository()
    {
        // Arrange & Act
        var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();

        // Assert
        Assert.IsType<EfCoreTransactionRepository>(repository);
    }

    [Fact]
    public async Task TransactionLifecycle_CreateAndUpdate_PersistsToDatabase()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var transactionId = Guid.NewGuid().ToString();
        var paymentIntentId = "pi_test_lifecycle_123";

        // Act - Create transaction
        var transaction = new TransactionEntity
        {
            Id = transactionId,
            StripePaymentIntentId = paymentIntentId,
            Status = "requires_payment_method",
            Amount = 5000,
            Currency = "usd",
            ClientSecret = "pi_test_lifecycle_123_secret_abc",
            Metadata = "{\"order_id\":\"12345\"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(transaction);

        // Assert - Transaction created
        var retrieved = await repository.GetByStripePaymentIntentIdAsync(paymentIntentId);
        Assert.NotNull(retrieved);
        Assert.Equal(transactionId, retrieved.Id);
        Assert.Equal("requires_payment_method", retrieved.Status);

        // Act - Update transaction status (simulating webhook)
        retrieved.Status = "succeeded";
        retrieved.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(retrieved);

        // Assert - Transaction updated
        var updated = await repository.GetByStripePaymentIntentIdAsync(paymentIntentId);
        Assert.NotNull(updated);
        Assert.Equal("succeeded", updated.Status);
        Assert.True(updated.UpdatedAt > updated.CreatedAt);
    }

    [Fact]
    public async Task CustomFieldMapping_ReflectedInDatabase()
    {
        // This test verifies that the DbContext correctly maps entity properties
        // to database columns based on the FieldMapping configuration
        
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = "pi_test_mapping_456",
            Status = "processing",
            Amount = 7500,
            Currency = "eur",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await repository.CreateAsync(transaction);

        // Assert - Query directly from DbContext to verify persistence
        var fromDb = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == "pi_test_mapping_456");
        
        Assert.NotNull(fromDb);
        Assert.Equal(transaction.Amount, fromDb.Amount);
        Assert.Equal(transaction.Currency, fromDb.Currency);
    }

    [Fact]
    public async Task ConcurrentTransactions_AllPersisted()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Create multiple transactions concurrently
        // Each task gets its own scope to avoid DbContext concurrency issues
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                // Create a new scope for each concurrent operation
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                
                var transaction = new TransactionEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StripePaymentIntentId = $"pi_test_concurrent_{index}",
                    Status = "requires_payment_method",
                    Amount = 1000 * (index + 1),
                    Currency = "usd",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await repository.CreateAsync(transaction);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All transactions persisted
        var count = await _dbContext.Transactions.CountAsync();
        Assert.True(count >= 10, $"Expected at least 10 transactions, but found {count}");
    }

    [Fact]
    public async Task MetadataJsonSerialization_PreservesData()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var metadata = "{\"customer_id\":\"cus_123\",\"order_id\":\"order_456\",\"source\":\"web\"}";
        
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = "pi_test_metadata_789",
            Status = "requires_payment_method",
            Amount = 2500,
            Currency = "gbp",
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await repository.CreateAsync(transaction);

        // Assert
        var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_test_metadata_789");
        Assert.NotNull(retrieved);
        Assert.Equal(metadata, retrieved.Metadata);
    }

    [Fact]
    public async Task NullableFields_HandledCorrectly()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = "pi_test_nullable_101",
            Status = "canceled",
            Amount = 0,
            Currency = "usd",
            ClientSecret = null, // Nullable
            Metadata = null, // Nullable
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await repository.CreateAsync(transaction);

        // Assert
        var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_test_nullable_101");
        Assert.NotNull(retrieved);
        Assert.Null(retrieved.ClientSecret);
        Assert.Null(retrieved.Metadata);
    }

    public void Dispose()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
