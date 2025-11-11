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
/// Multi-database integration tests that verify FilthyRodriguez works with all supported database providers.
/// These tests require Docker containers to be running. Run: docker-compose -f docker-compose.test.yml up -d
/// </summary>
public class MultiDatabaseIntegrationTests
{
    private const string SqlServerConnectionString = "Server=localhost,1433;Database=filthy_rodriguez_test;User Id=sa;Password=FilthyTest123!;TrustServerCertificate=True;";
    private const string PostgresConnectionString = "Host=localhost;Port=5432;Database=filthy_rodriguez_test;Username=testuser;Password=FilthyTest123!;";
    private const string MySqlConnectionString = "Server=localhost;Port=3306;Database=filthy_rodriguez_test;Uid=testuser;Pwd=FilthyTest123!;";
    private const string SqliteConnectionString = "DataSource=:memory:";

    [Fact]
    public async Task SQLite_CreateAndRetrieveTransaction()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("SQLite", SqliteConnectionString);
        
        try
        {
            // Keep connection open for in-memory SQLite
            dbContext.Database.OpenConnection();
            await dbContext.Database.EnsureCreatedAsync();

            var transaction = CreateTestTransaction("pi_sqlite_test_001");

            // Act
            await repository.CreateAsync(transaction);
            var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_sqlite_test_001");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("pi_sqlite_test_001", retrieved.StripePaymentIntentId);
            Assert.Equal(5000L, retrieved.Amount);
            Assert.Equal("usd", retrieved.Currency);
        }
        finally
        {
            dbContext.Database.CloseConnection();
            dbContext.Dispose();
        }
    }

    [Fact(Skip = "Requires Docker: docker-compose -f docker-compose.test.yml up -d sqlserver")]
    public async Task SQLServer_CreateAndRetrieveTransaction()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("SQLServer", SqlServerConnectionString);
        
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            var transaction = CreateTestTransaction("pi_sqlserver_test_001");

            // Act
            await repository.CreateAsync(transaction);
            var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_sqlserver_test_001");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("pi_sqlserver_test_001", retrieved.StripePaymentIntentId);
            Assert.Equal(5000L, retrieved.Amount);
            
            // Cleanup
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            dbContext.Dispose();
        }
    }

    [Fact(Skip = "Requires Docker: docker-compose -f docker-compose.test.yml up -d postgres")]
    public async Task PostgreSQL_CreateAndRetrieveTransaction()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("PostgreSQL", PostgresConnectionString);
        
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            var transaction = CreateTestTransaction("pi_postgres_test_001");

            // Act
            await repository.CreateAsync(transaction);
            var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_postgres_test_001");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("pi_postgres_test_001", retrieved.StripePaymentIntentId);
            Assert.Equal(5000L, retrieved.Amount);
            
            // Cleanup
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            dbContext.Dispose();
        }
    }

    [Fact(Skip = "Requires Docker: docker-compose -f docker-compose.test.yml up -d mysql")]
    public async Task MySQL_CreateAndRetrieveTransaction()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("MySQL", MySqlConnectionString);
        
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            var transaction = CreateTestTransaction("pi_mysql_test_001");

            // Act
            await repository.CreateAsync(transaction);
            var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_mysql_test_001");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("pi_mysql_test_001", retrieved.StripePaymentIntentId);
            Assert.Equal(5000L, retrieved.Amount);
            
            // Cleanup
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            dbContext.Dispose();
        }
    }

    [Fact(Skip = "Requires Docker: docker-compose -f docker-compose.test.yml up -d")]
    public async Task AllDatabases_ConcurrentOperations_NoConflicts()
    {
        // This test verifies that multiple database providers can be used independently
        var databases = new[]
        {
            ("SQLServer", SqlServerConnectionString),
            ("PostgreSQL", PostgresConnectionString),
            ("MySQL", MySqlConnectionString)
        };

        var tasks = databases.Select(async (db, index) =>
        {
            var (provider, connectionString) = db;
            var (repository, dbContext) = CreateRepositoryForProvider(provider, connectionString);
            
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                
                var transaction = CreateTestTransaction($"pi_{provider.ToLower()}_concurrent_{index}");
                await repository.CreateAsync(transaction);
                
                var retrieved = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
                Assert.NotNull(retrieved);
                
                await dbContext.Database.EnsureDeletedAsync();
            }
            finally
            {
                dbContext.Dispose();
            }
        }).ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task SQLite_UpdateTransaction_PersistsChanges()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("SQLite", SqliteConnectionString);
        
        try
        {
            dbContext.Database.OpenConnection();
            await dbContext.Database.EnsureCreatedAsync();

            var transaction = CreateTestTransaction("pi_sqlite_update_001");
            await repository.CreateAsync(transaction);

            // Act - Update status
            var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_sqlite_update_001");
            Assert.NotNull(retrieved);
            
            retrieved.Status = "succeeded";
            retrieved.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(retrieved);

            // Assert
            var updated = await repository.GetByStripePaymentIntentIdAsync("pi_sqlite_update_001");
            Assert.NotNull(updated);
            Assert.Equal("succeeded", updated.Status);
            Assert.True(updated.UpdatedAt > updated.CreatedAt);
        }
        finally
        {
            dbContext.Database.CloseConnection();
            dbContext.Dispose();
        }
    }

    [Fact(Skip = "Requires Docker: docker-compose -f docker-compose.test.yml up -d")]
    public async Task AllDatabases_ExtendedDataFields_PersistCorrectly()
    {
        var databases = new[]
        {
            ("SQLServer", SqlServerConnectionString),
            ("PostgreSQL", PostgresConnectionString),
            ("MySQL", MySqlConnectionString)
        };

        foreach (var (provider, connectionString) in databases)
        {
            var (repository, dbContext) = CreateRepositoryForProvider(provider, connectionString);
            
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                
                // Create transaction with extended data
                var transaction = new TransactionEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StripePaymentIntentId = $"pi_{provider.ToLower()}_extended_001",
                    Status = "succeeded",
                    Amount = 12500,
                    Currency = "usd",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Extended fields
                    CustomerId = "cus_test_123",
                    CustomerEmail = "test@example.com",
                    PaymentMethodId = "pm_test_card",
                    PaymentMethodType = "card",
                    CardLast4 = "4242",
                    CardBrand = "visa",
                    Description = "Test payment for extended data",
                    ReceiptEmail = "receipt@example.com",
                    CapturedAmount = 12500,
                    RefundedAmount = 0,
                    ApplicationFeeAmount = 125
                };
                
                await repository.CreateAsync(transaction);
                
                // Assert all extended fields persisted
                var retrieved = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
                Assert.NotNull(retrieved);
                Assert.Equal("cus_test_123", retrieved.CustomerId);
                Assert.Equal("test@example.com", retrieved.CustomerEmail);
                Assert.Equal("pm_test_card", retrieved.PaymentMethodId);
                Assert.Equal("card", retrieved.PaymentMethodType);
                Assert.Equal("4242", retrieved.CardLast4);
                Assert.Equal("visa", retrieved.CardBrand);
                Assert.Equal("Test payment for extended data", retrieved.Description);
                Assert.Equal("receipt@example.com", retrieved.ReceiptEmail);
                Assert.Equal(12500L, retrieved.CapturedAmount);
                Assert.Equal(0L, retrieved.RefundedAmount);
                Assert.Equal(125L, retrieved.ApplicationFeeAmount);
                
                await dbContext.Database.EnsureDeletedAsync();
            }
            finally
            {
                dbContext.Dispose();
            }
        }
    }

    private (ITransactionRepository repository, StripePaymentDbContext dbContext) CreateRepositoryForProvider(
        string provider, 
        string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FilthyRodriguez:ApiKey"] = "sk_test_fake_key_for_testing",
                ["FilthyRodriguez:WebhookSecret"] = "whsec_fake_secret_for_testing",
                ["FilthyRodriguez:Database:Enabled"] = "true",
                ["FilthyRodriguez:Database:ConnectionString"] = connectionString,
                ["FilthyRodriguez:Database:Provider"] = provider
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFilthyRodriguez(configuration)
            .WithEntityFramework();

        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<ITransactionRepository>();
        var dbContext = serviceProvider.GetRequiredService<StripePaymentDbContext>();

        return (repository, dbContext);
    }

    private TransactionEntity CreateTestTransaction(string paymentIntentId)
    {
        return new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = paymentIntentId,
            Status = "requires_payment_method",
            Amount = 5000,
            Currency = "usd",
            ClientSecret = $"{paymentIntentId}_secret_abc123",
            Metadata = "{\"test\":\"data\"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
