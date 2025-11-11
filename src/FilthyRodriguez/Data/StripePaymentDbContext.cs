using Microsoft.EntityFrameworkCore;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Models;

namespace FilthyRodriguez.Data;

/// <summary>
/// Entity Framework DbContext for Stripe payment transactions
/// </summary>
public class StripePaymentDbContext : DbContext
{
    private readonly DatabaseOptions _databaseOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripePaymentDbContext"/> class
    /// </summary>
    /// <param name="options">DbContext options</param>
    /// <param name="databaseOptions">Database configuration options</param>
    public StripePaymentDbContext(
        DbContextOptions<StripePaymentDbContext> options,
        DatabaseOptions databaseOptions) : base(options)
    {
        _databaseOptions = databaseOptions;
    }

    /// <summary>
    /// Transaction entities
    /// </summary>
    public DbSet<TransactionEntity> Transactions { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<TransactionEntity>();
        var fieldMapping = _databaseOptions.FieldMapping;

        // Configure table name
        entity.ToTable(_databaseOptions.TableName);

        // Configure column names based on field mapping
        entity.Property(t => t.Id)
            .HasColumnName(fieldMapping.Id)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(t => t.StripePaymentIntentId)
            .HasColumnName(fieldMapping.StripePaymentIntentId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(t => t.Status)
            .HasColumnName(fieldMapping.Status)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(t => t.Amount)
            .HasColumnName(fieldMapping.Amount)
            .IsRequired();

        entity.Property(t => t.Currency)
            .HasColumnName(fieldMapping.Currency)
            .HasMaxLength(3)
            .IsRequired();

        entity.Property(t => t.ClientSecret)
            .HasColumnName(fieldMapping.ClientSecret)
            .HasMaxLength(500);

        entity.Property(t => t.Metadata)
            .HasColumnName(fieldMapping.Metadata);

        entity.Property(t => t.CreatedAt)
            .HasColumnName(fieldMapping.CreatedAt)
            .IsRequired();

        entity.Property(t => t.UpdatedAt)
            .HasColumnName(fieldMapping.UpdatedAt)
            .IsRequired();

        // Configure extended fields (optional)
        if (_databaseOptions.CaptureExtendedData)
        {
            entity.Property(t => t.CustomerId)
                .HasColumnName(fieldMapping.CustomerId)
                .HasMaxLength(100);

            entity.Property(t => t.CustomerEmail)
                .HasColumnName(fieldMapping.CustomerEmail)
                .HasMaxLength(255);

            entity.Property(t => t.PaymentMethodId)
                .HasColumnName(fieldMapping.PaymentMethodId)
                .HasMaxLength(100);

            entity.Property(t => t.PaymentMethodType)
                .HasColumnName(fieldMapping.PaymentMethodType)
                .HasMaxLength(50);

            entity.Property(t => t.CardLast4)
                .HasColumnName(fieldMapping.CardLast4)
                .HasMaxLength(4);

            entity.Property(t => t.CardBrand)
                .HasColumnName(fieldMapping.CardBrand)
                .HasMaxLength(50);

            entity.Property(t => t.Description)
                .HasColumnName(fieldMapping.Description)
                .HasMaxLength(1000);

            entity.Property(t => t.ReceiptEmail)
                .HasColumnName(fieldMapping.ReceiptEmail)
                .HasMaxLength(255);

            entity.Property(t => t.CapturedAmount)
                .HasColumnName(fieldMapping.CapturedAmount);

            entity.Property(t => t.RefundedAmount)
                .HasColumnName(fieldMapping.RefundedAmount);

            entity.Property(t => t.ApplicationFeeAmount)
                .HasColumnName(fieldMapping.ApplicationFeeAmount);
        }

        // Configure primary key
        entity.HasKey(t => t.Id);

        // Configure indexes
        entity.HasIndex(t => t.StripePaymentIntentId)
            .HasDatabaseName($"IX_{_databaseOptions.TableName}_{fieldMapping.StripePaymentIntentId}");

        entity.HasIndex(t => t.Status)
            .HasDatabaseName($"IX_{_databaseOptions.TableName}_{fieldMapping.Status}");

        entity.HasIndex(t => t.CreatedAt)
            .HasDatabaseName($"IX_{_databaseOptions.TableName}_{fieldMapping.CreatedAt}");

        // Configure extended field indexes
        if (_databaseOptions.CaptureExtendedData)
        {
            entity.HasIndex(t => t.CustomerId)
                .HasDatabaseName($"IX_{_databaseOptions.TableName}_{fieldMapping.CustomerId}");

            entity.HasIndex(t => t.CustomerEmail)
                .HasDatabaseName($"IX_{_databaseOptions.TableName}_{fieldMapping.CustomerEmail}");
        }
    }
}
