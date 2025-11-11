using FilthyRodriguez.Models;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Tests;

public class InMemoryTransactionRepositoryTests
{
    [Fact]
    public async Task CreateAsync_CreatesTransaction()
    {
        // Arrange
        var repository = new InMemoryTransactionRepository();
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
        await repository.CreateAsync(transaction);

        // Assert
        var retrieved = await repository.GetByIdAsync(transaction.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(transaction.Id, retrieved.Id);
        Assert.Equal(transaction.StripePaymentIntentId, retrieved.StripePaymentIntentId);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTransaction()
    {
        // Arrange
        var repository = new InMemoryTransactionRepository();
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

        await repository.CreateAsync(transaction);

        // Act
        transaction.Status = "succeeded";
        transaction.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(transaction);

        // Assert
        var retrieved = await repository.GetByIdAsync(transaction.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("succeeded", retrieved.Status);
    }

    [Fact]
    public async Task GetByStripePaymentIntentIdAsync_ReturnsTransaction()
    {
        // Arrange
        var repository = new InMemoryTransactionRepository();
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

        await repository.CreateAsync(transaction);

        // Act
        var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_test_789");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(transaction.Id, retrieved.Id);
        Assert.Equal(transaction.StripePaymentIntentId, retrieved.StripePaymentIntentId);
    }

    [Fact]
    public async Task GetByStripePaymentIntentIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var repository = new InMemoryTransactionRepository();

        // Act
        var retrieved = await repository.GetByStripePaymentIntentIdAsync("pi_test_nonexistent");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var repository = new InMemoryTransactionRepository();

        // Act
        var retrieved = await repository.GetByIdAsync("nonexistent_id");

        // Assert
        Assert.Null(retrieved);
    }
}
