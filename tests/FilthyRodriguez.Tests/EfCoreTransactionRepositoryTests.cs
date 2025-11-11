using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Data;
using FilthyRodriguez.Models;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class EfCoreTransactionRepositoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly StripePaymentDbContext _dbContext;
    private readonly EfCoreTransactionRepository _repository;

    public EfCoreTransactionRepositoryTests()
    {
        var services = new ServiceCollection();

        // Configure in-memory SQLite database
        var databaseOptions = new DatabaseOptions
        {
            Enabled = true,
            ConnectionString = "DataSource=:memory:",
            Provider = "SQLite",
            TableName = "stripe_transactions",
            FieldMapping = new FieldMappingOptions()
        };

        services.AddDbContext<StripePaymentDbContext>(options =>
            options.UseSqlite(databaseOptions.ConnectionString));

        services.AddSingleton(databaseOptions);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<StripePaymentDbContext>();
        
        // Open connection for in-memory database
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _repository = new EfCoreTransactionRepository(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_CreatesTransaction()
    {
        // Arrange
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = "pi_test_123",
            Status = "requires_payment_method",
            Amount = 1000,
            Currency = "usd",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.CreateAsync(transaction);

        // Assert
        var retrieved = await _repository.GetByIdAsync(transaction.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(transaction.Id, retrieved.Id);
        Assert.Equal(transaction.StripePaymentIntentId, retrieved.StripePaymentIntentId);
        Assert.Equal(transaction.Status, retrieved.Status);
        Assert.Equal(transaction.Amount, retrieved.Amount);
        Assert.Equal(transaction.Currency, retrieved.Currency);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTransaction()
    {
        // Arrange
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = "pi_test_456",
            Status = "requires_payment_method",
            Amount = 2000,
            Currency = "usd",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(transaction);

        // Act
        transaction.Status = "succeeded";
        transaction.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(transaction);

        // Assert
        var retrieved = await _repository.GetByIdAsync(transaction.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("succeeded", retrieved.Status);
    }

    [Fact]
    public async Task GetByStripePaymentIntentIdAsync_ReturnsTransaction()
    {
        // Arrange
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid().ToString(),
            StripePaymentIntentId = "pi_test_789",
            Status = "requires_payment_method",
            Amount = 3000,
            Currency = "eur",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(transaction);

        // Act
        var retrieved = await _repository.GetByStripePaymentIntentIdAsync("pi_test_789");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(transaction.Id, retrieved.Id);
        Assert.Equal(transaction.StripePaymentIntentId, retrieved.StripePaymentIntentId);
    }

    [Fact]
    public async Task GetByStripePaymentIntentIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var retrieved = await _repository.GetByStripePaymentIntentIdAsync("pi_test_nonexistent");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var retrieved = await _repository.GetByIdAsync("nonexistent_id");

        // Assert
        Assert.Null(retrieved);
    }

    public void Dispose()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
