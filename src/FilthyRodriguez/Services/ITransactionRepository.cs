using FilthyRodriguez.Models;

namespace FilthyRodriguez.Services;

/// <summary>
/// Repository interface for managing payment transaction persistence
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Creates a new transaction record
    /// </summary>
    /// <param name="transaction">Transaction entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateAsync(TransactionEntity transaction, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing transaction record
    /// </summary>
    /// <param name="transaction">Transaction entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(TransactionEntity transaction, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a transaction by its Stripe Payment Intent ID
    /// </summary>
    /// <param name="stripePaymentIntentId">Stripe Payment Intent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction entity if found, null otherwise</returns>
    Task<TransactionEntity?> GetByStripePaymentIntentIdAsync(string stripePaymentIntentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a transaction by its ID
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction entity if found, null otherwise</returns>
    Task<TransactionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
