using Microsoft.EntityFrameworkCore;
using FilthyRodriguez.Data;
using FilthyRodriguez.Models;

namespace FilthyRodriguez.Services;

/// <summary>
/// Entity Framework Core implementation of the transaction repository
/// </summary>
public class EfCoreTransactionRepository : ITransactionRepository
{
    private readonly StripePaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreTransactionRepository"/> class
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public EfCoreTransactionRepository(StripePaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(TransactionEntity transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(TransactionEntity transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Update(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TransactionEntity?> GetByStripePaymentIntentIdAsync(string stripePaymentIntentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == stripePaymentIntentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TransactionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .FindAsync(new object[] { id }, cancellationToken);
    }
}
