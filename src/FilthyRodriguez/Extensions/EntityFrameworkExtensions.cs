using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FilthyRodriguez.Configuration;
using FilthyRodriguez.Data;
using FilthyRodriguez.Services;

namespace FilthyRodriguez.Extensions;

/// <summary>
/// Extension methods for configuring Entity Framework Core database persistence
/// </summary>
public static class EntityFrameworkExtensions
{
    /// <summary>
    /// Adds Entity Framework Core persistence with automatic configuration from appsettings.json
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection WithEntityFramework(this IServiceCollection services)
    {
        // Replace in-memory repository with EF Core repository
        services.AddScoped<ITransactionRepository, EfCoreTransactionRepository>();

        // Register DbContext
        services.AddDbContext<StripePaymentDbContext>((serviceProvider, options) =>
        {
            var stripeOptions = serviceProvider.GetRequiredService<IOptions<StripePaymentOptions>>().Value;
            var databaseOptions = stripeOptions.Database;

            if (databaseOptions == null || !databaseOptions.Enabled)
            {
                throw new InvalidOperationException("Database configuration is not enabled. Set Database.Enabled = true in configuration.");
            }

            if (string.IsNullOrEmpty(databaseOptions.ConnectionString))
            {
                throw new InvalidOperationException("Database connection string is required when Entity Framework is enabled.");
            }

            ConfigureDbContextOptions(options, databaseOptions);
        });

        // Register DatabaseOptions for DbContext constructor
        services.AddScoped(serviceProvider =>
        {
            var stripeOptions = serviceProvider.GetRequiredService<IOptions<StripePaymentOptions>>().Value;
            return stripeOptions.Database ?? new DatabaseOptions();
        });

        return services;
    }

    /// <summary>
    /// Adds Entity Framework Core persistence with explicit DbContext options configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="optionsAction">Action to configure DbContext options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection WithEntityFramework(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        // Replace in-memory repository with EF Core repository
        services.AddScoped<ITransactionRepository, EfCoreTransactionRepository>();

        // Register DbContext with custom options
        services.AddDbContext<StripePaymentDbContext>((serviceProvider, options) =>
        {
            optionsAction(options);
        });

        // Register DatabaseOptions with defaults
        services.AddScoped(serviceProvider =>
        {
            var stripeOptions = serviceProvider.GetRequiredService<IOptions<StripePaymentOptions>>().Value;
            return stripeOptions.Database ?? new DatabaseOptions();
        });

        return services;
    }

    private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, DatabaseOptions databaseOptions)
    {
        switch (databaseOptions.Provider.ToLowerInvariant())
        {
            case "sqlserver":
                options.UseSqlServer(databaseOptions.ConnectionString);
                break;

            case "postgresql":
            case "postgres":
                options.UseNpgsql(databaseOptions.ConnectionString);
                break;

            case "mysql":
                options.UseMySql(
                    databaseOptions.ConnectionString,
                    ServerVersion.AutoDetect(databaseOptions.ConnectionString));
                break;

            case "sqlite":
                options.UseSqlite(databaseOptions.ConnectionString);
                break;

            default:
                throw new NotSupportedException($"Database provider '{databaseOptions.Provider}' is not supported. Supported providers: SqlServer, PostgreSQL, MySQL, SQLite");
        }
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<StripePaymentDbContext>();
        
        if (dbContext != null)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
    }

    /// <summary>
    /// Applies any pending migrations to the database
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<StripePaymentDbContext>();
        
        if (dbContext != null)
        {
            await dbContext.Database.MigrateAsync();
        }
    }
}
