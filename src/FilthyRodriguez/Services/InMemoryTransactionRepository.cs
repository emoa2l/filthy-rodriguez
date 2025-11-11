using FilthyRodriguez.Models;
using System.Collections.Concurrent;

namespace FilthyRodriguez.Services;

/// <summary>
/// In-memory implementation of the transaction repository
/// </summary>
public class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<string, TransactionEntity> _transactions = new();
    private readonly ConcurrentDictionary<string, string> _paymentIntentIdToTransactionId = new();

    /// <inheritdoc/>
    public Task CreateAsync(TransactionEntity transaction, CancellationToken cancellationToken = default)
    {
        _transactions.TryAdd(transaction.Id, transaction);
        _paymentIntentIdToTransactionId.TryAdd(transaction.StripePaymentIntentId, transaction.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateAsync(TransactionEntity transaction, CancellationToken cancellationToken = default)
    {
        _transactions[transaction.Id] = transaction;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<TransactionEntity?> GetByStripePaymentIntentIdAsync(string stripePaymentIntentId, CancellationToken cancellationToken = default)
    {
        if (_paymentIntentIdToTransactionId.TryGetValue(stripePaymentIntentId, out var transactionId))
        {
            _transactions.TryGetValue(transactionId, out var transaction);
            return Task.FromResult(transaction);
        }
        return Task.FromResult<TransactionEntity?>(null);
    }

    /// <inheritdoc/>
    public Task<TransactionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _transactions.TryGetValue(id, out var transaction);
        return Task.FromResult(transaction);
    }
}
