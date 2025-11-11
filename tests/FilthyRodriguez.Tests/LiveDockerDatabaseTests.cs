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
/// Live Docker integration tests. Run these manually when Docker containers are available.
/// Start containers with: docker-compose -f docker-compose.test.yml up -d
/// </summary>
public class LiveDockerDatabaseTests
{
    private const string PostgresConnectionString = "Host=localhost;Port=5432;Database=filthy_rodriguez_test;Username=testuser;Password=FilthyTest123!;";
    private const string MySqlConnectionString = "Server=localhost;Port=3306;Database=filthy_rodriguez_test;Uid=testuser;Pwd=FilthyTest123!;";
    private const string SqlServerConnectionString = "Server=localhost,1433;Database=filthy_rodriguez_test;User Id=sa;Password=FilthyTest123!;TrustServerCertificate=True;";

    [Fact]
    public async Task PostgreSQL_FullLifecycleTest()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("PostgreSQL", PostgresConnectionString);
        
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            
            // Create
            var transaction = new TransactionEntity
            {
                Id = Guid.NewGuid().ToString(),
                StripePaymentIntentId = $"pi_postgres_live_{DateTime.UtcNow.Ticks}",
                Status = "requires_payment_method",
                Amount = 7500,
                Currency = "usd",
                ClientSecret = "pi_test_secret",
                Metadata = "{\"test\":\"postgres\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Extended fields
                CustomerId = "cus_postgres_123",
                CustomerEmail = "postgres@test.com",
                PaymentMethodType = "card",
                CardLast4 = "4242",
                CardBrand = "visa"
            };

            await repository.CreateAsync(transaction);

            // Retrieve
            var retrieved = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
            Assert.NotNull(retrieved);
            Assert.Equal("requires_payment_method", retrieved.Status);
            Assert.Equal("postgres@test.com", retrieved.CustomerEmail);
            Assert.Equal("4242", retrieved.CardLast4);

            // Update
            retrieved.Status = "succeeded";
            retrieved.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(retrieved);

            var updated = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
            Assert.NotNull(updated);
            Assert.Equal("succeeded", updated.Status);
            
            // Cleanup
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            dbContext.Dispose();
        }
    }

    [Fact]
    public async Task MySQL_FullLifecycleTest()
    {
        // Arrange
        var (repository, dbContext) = CreateRepositoryForProvider("MySQL", MySqlConnectionString);
        
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            
            // Create
            var transaction = new TransactionEntity
            {
                Id = Guid.NewGuid().ToString(),
                StripePaymentIntentId = $"pi_mysql_live_{DateTime.UtcNow.Ticks}",
                Status = "processing",
                Amount = 12000,
                Currency = "eur",
                ClientSecret = "pi_mysql_secret",
                Metadata = "{\"test\":\"mysql\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Extended fields
                CustomerId = "cus_mysql_456",
                CustomerEmail = "mysql@test.com",
                PaymentMethodType = "card",
                CardLast4 = "5555",
                CardBrand = "mastercard",
                Description = "MySQL test payment"
            };

            await repository.CreateAsync(transaction);

            // Retrieve
            var retrieved = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
            Assert.NotNull(retrieved);
            Assert.Equal("processing", retrieved.Status);
            Assert.Equal("mysql@test.com", retrieved.CustomerEmail);
            Assert.Equal("mastercard", retrieved.CardBrand);
            Assert.Equal("MySQL test payment", retrieved.Description);

            // Update
            retrieved.Status = "succeeded";
            retrieved.CapturedAmount = 12000;
            retrieved.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(retrieved);

            var updated = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
            Assert.NotNull(updated);
            Assert.Equal("succeeded", updated.Status);
            Assert.Equal(12000L, updated.CapturedAmount);
            
            // Cleanup
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            dbContext.Dispose();
        }
    }

    [Fact]
    public async Task PostgreSQL_ConcurrentWrites()
    {
        // Test concurrent writes don't cause conflicts
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var (repository, dbContext) = CreateRepositoryForProvider("PostgreSQL", PostgresConnectionString);
            
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                
                var transaction = new TransactionEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StripePaymentIntentId = $"pi_concurrent_{i}_{DateTime.UtcNow.Ticks}",
                    Status = "succeeded",
                    Amount = 1000 * (i + 1),
                    Currency = "usd",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await repository.CreateAsync(transaction);
                
                var retrieved = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
                Assert.NotNull(retrieved);
                Assert.Equal(1000 * (i + 1), retrieved.Amount);
            }
            finally
            {
                dbContext.Dispose();
            }
        });

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task MySQL_RefundScenario()
    {
        // Test a refund workflow
        var (repository, dbContext) = CreateRepositoryForProvider("MySQL", MySqlConnectionString);
        
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            
            // Create successful payment
            var transaction = new TransactionEntity
            {
                Id = Guid.NewGuid().ToString(),
                StripePaymentIntentId = $"pi_refund_test_{DateTime.UtcNow.Ticks}",
                Status = "succeeded",
                Amount = 25000,
                Currency = "usd",
                CapturedAmount = 25000,
                RefundedAmount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.CreateAsync(transaction);

            // Simulate partial refund
            var retrieved = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
            Assert.NotNull(retrieved);
            
            retrieved.RefundedAmount = 10000;
            retrieved.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(retrieved);

            // Verify refund
            var refunded = await repository.GetByStripePaymentIntentIdAsync(transaction.StripePaymentIntentId);
            Assert.NotNull(refunded);
            Assert.Equal(25000L, refunded.CapturedAmount);
            Assert.Equal(10000L, refunded.RefundedAmount);
            
            // Cleanup
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            dbContext.Dispose();
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
}
