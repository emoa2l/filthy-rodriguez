# FilthyRodriguez Database Configuration Guide

> ⚠️ **Experimental Feature**: Database persistence is currently experimental and under active development. The API, configuration schema, and database schema may change in future releases. Test thoroughly in non-production environments before deploying to production.

This guide covers all database configuration options for the FilthyRodriguez Stripe Payment Plugin.

## Overview

The Stripe Payment Plugin supports optional database persistence using Entity Framework Core. This is **completely optional** - the plugin works perfectly fine without it using in-memory storage.

## Table of Contents

- [Quick Start](#quick-start)
- [Database Providers](#database-providers)
- [Schema Configuration](#schema-configuration)
- [Migration Strategies](#migration-strategies)
- [Custom Field Mapping](#custom-field-mapping)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Quick Start

### 1. Configure Database in appsettings.json

```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "Database": {
      "Enabled": true,
      "ConnectionString": "YOUR_CONNECTION_STRING",
      "Provider": "SqlServer"
    }
  }
}
```

### 2. Enable Entity Framework in Program.cs

```csharp
using FilthyRodriguez.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithEntityFramework();

var app = builder.Build();

// Optional: Create database automatically (development only)
await app.Services.EnsureDatabaseCreatedAsync();

app.UseWebSockets();
app.MapStripePaymentEndpoints("/api/stripe");
app.MapStripeWebSocket("/stripe/ws");

app.Run();
```

### 3. That's It!

The plugin will now automatically persist all payment transactions to your database.

## Database Providers

### SQL Server

**Connection String:**
```
Server=localhost;Database=payments;Trusted_Connection=true;TrustServerCertificate=true;
```

**Configuration:**
```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Server=localhost;Database=payments;Trusted_Connection=true;",
    "Provider": "SqlServer"
  }
}
```

**Data Types:**
- Strings: NVARCHAR
- Numbers: BIGINT
- Timestamps: DATETIME2

### PostgreSQL

**Connection String:**
```
Host=localhost;Database=payments;Username=appuser;Password=secret
```

**Configuration:**
```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Host=localhost;Database=payments;Username=appuser;Password=secret",
    "Provider": "PostgreSQL"
  }
}
```

**Data Types:**
- Strings: VARCHAR / TEXT
- Numbers: BIGINT
- Timestamps: TIMESTAMP

**Note:** Provider can be specified as "PostgreSQL" or "Postgres"

### MySQL

**Connection String:**
```
Server=localhost;Database=payments;User=appuser;Password=secret;
```

**Configuration:**
```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Server=localhost;Database=payments;User=appuser;Password=secret;",
    "Provider": "MySQL"
  }
}
```

**Data Types:**
- Strings: VARCHAR / TEXT
- Numbers: BIGINT
- Timestamps: DATETIME

**Note:** The plugin uses Pomelo.EntityFrameworkCore.MySql with auto-detected server version.

### SQLite

**Connection String:**
```
Data Source=payments.db
```

**Configuration:**
```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "Data Source=payments.db",
    "Provider": "SQLite"
  }
}
```

**Data Types:**
- Strings: TEXT
- Numbers: INTEGER
- Timestamps: TEXT (ISO 8601)

**Best For:**
- Local development
- Testing
- Small-scale deployments
- Embedded scenarios

## Schema Configuration

### Default Schema

The default table structure (using default field names):

```sql
CREATE TABLE stripe_transactions (
    transaction_id NVARCHAR(100) PRIMARY KEY,
    stripe_pi_id NVARCHAR(100) NOT NULL,
    payment_status NVARCHAR(50) NOT NULL,
    amount_cents BIGINT NOT NULL,
    currency_code NVARCHAR(3) NOT NULL,
    client_secret NVARCHAR(500),
    metadata_json NVARCHAR(MAX),
    created_timestamp DATETIME2 NOT NULL,
    updated_timestamp DATETIME2 NOT NULL
);

-- Indexes for common queries
CREATE INDEX IX_stripe_transactions_stripe_pi_id ON stripe_transactions(stripe_pi_id);
CREATE INDEX IX_stripe_transactions_payment_status ON stripe_transactions(payment_status);
CREATE INDEX IX_stripe_transactions_created_timestamp ON stripe_transactions(created_timestamp);
```

### Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `transaction_id` | String (100) | Unique internal transaction identifier (GUID) |
| `stripe_pi_id` | String (100) | Stripe Payment Intent ID (starts with `pi_`) |
| `payment_status` | String (50) | Current payment status (e.g., "succeeded", "requires_payment_method") |
| `amount_cents` | Number | Amount in smallest currency unit (cents for USD) |
| `currency_code` | String (3) | ISO 4217 currency code (e.g., "usd", "eur") |
| `client_secret` | String (500) | Stripe client secret for payment confirmation |
| `metadata_json` | String | JSON-serialized metadata from payment request |
| `created_timestamp` | DateTime | Transaction creation timestamp (UTC) |
| `updated_timestamp` | DateTime | Last update timestamp (UTC) |

### Extended Data Capture

**NEW:** Enable `CaptureExtendedData` to store additional transaction details from Stripe.

**Configuration:**
```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "...",
    "Provider": "SQLite",
    "CaptureExtendedData": true
  }
}
```

When enabled, the following additional fields are captured and stored:

| Field | Type | Description |
|-------|------|-------------|
| `customer_id` | String (100) | Stripe Customer ID (if payment is associated with a customer) |
| `customer_email` | String (255) | Customer email address |
| `payment_method_id` | String (100) | Payment method ID used for transaction |
| `payment_method_type` | String (50) | Payment method type (card, bank_account, etc.) |
| `card_last4` | String (4) | Last 4 digits of card (if payment method is card) |
| `card_brand` | String (50) | Card brand (visa, mastercard, amex, etc.) |
| `description` | String (1000) | Transaction description |
| `receipt_email` | String (255) | Email address where receipt was sent |
| `captured_amount` | Number | Amount actually captured (may differ from authorized) |
| `refunded_amount` | Number | Total amount refunded |
| `application_fee_amount` | Number | Application fee (for platform/marketplace scenarios) |

**Extended Schema Example (SQLite):**
```sql
CREATE TABLE stripe_transactions (
    -- Core fields
    transaction_id TEXT PRIMARY KEY,
    stripe_pi_id TEXT NOT NULL,
    payment_status TEXT NOT NULL,
    amount_cents INTEGER NOT NULL,
    currency_code TEXT NOT NULL,
    client_secret TEXT,
    metadata_json TEXT,
    created_timestamp TEXT NOT NULL,
    updated_timestamp TEXT NOT NULL,
    
    -- Extended fields (when CaptureExtendedData = true)
    customer_id TEXT,
    customer_email TEXT,
    payment_method_id TEXT,
    payment_method_type TEXT,
    card_last4 TEXT,
    card_brand TEXT,
    description TEXT,
    receipt_email TEXT,
    captured_amount INTEGER,
    refunded_amount INTEGER,
    application_fee_amount INTEGER
);

-- Additional indexes for extended data
CREATE INDEX IX_stripe_transactions_customer_id ON stripe_transactions(customer_id);
CREATE INDEX IX_stripe_transactions_customer_email ON stripe_transactions(customer_email);
```

**Use Cases for Extended Data:**
- **Customer Analytics**: Track spending patterns by customer
- **Payment Method Analysis**: Understand which card brands/types are used
- **Financial Reporting**: Track fees, refunds, and net amounts
- **Compliance**: Maintain detailed audit trails
- **Customer Support**: Quick lookup by email or last 4 digits

**Performance Considerations:**
- Extended data adds ~11 additional columns
- Minimal performance impact (all fields are optional/nullable)
- Indexes on customer_id and customer_email for fast lookups
- No impact on existing in-memory storage mode

## Migration Strategies

### Strategy 1: Auto-Create (Development)

Automatically creates the database and schema on startup.

```csharp
var app = builder.Build();
await app.Services.EnsureDatabaseCreatedAsync();
app.Run();
```

**Pros:**
- Quick setup for development
- No manual steps required

**Cons:**
- Not recommended for production
- No version control of schema changes

### Strategy 2: EF Migrations (Recommended)

Use Entity Framework migrations for controlled schema deployment.

**Step 1: Install EF Core tools**
```bash
dotnet tool install --global dotnet-ef
```

**Step 2: Create migration**
```bash
dotnet ef migrations add InitialStripeSchema --project YourApp
```

**Step 3: Review migration** (in Migrations folder)

**Step 4: Apply to database**
```bash
dotnet ef database update --project YourApp
```

**For production deployments:**
```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.EnsureDatabaseCreatedAsync();
}
else
{
    await app.Services.MigrateDatabaseAsync();
}

app.Run();
```

**Pros:**
- Full control over schema changes
- Version control friendly
- Safe for production
- Reversible

### Strategy 3: Database-First (Existing Schema)

Use an existing database table with custom field mapping.

**Step 1: Create table manually**
```sql
CREATE TABLE payment_records (
    id VARCHAR(100) PRIMARY KEY,
    payment_intent VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL,
    amount BIGINT NOT NULL,
    currency CHAR(3) NOT NULL,
    secret VARCHAR(500),
    metadata TEXT,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);
```

**Step 2: Configure field mapping**
```json
{
  "Database": {
    "Enabled": true,
    "ConnectionString": "...",
    "Provider": "PostgreSQL",
    "TableName": "payment_records",
    "FieldMapping": {
      "Id": "id",
      "StripePaymentIntentId": "payment_intent",
      "Status": "status",
      "Amount": "amount",
      "Currency": "currency",
      "ClientSecret": "secret",
      "CreatedAt": "created_at",
      "UpdatedAt": "updated_at",
      "Metadata": "metadata"
    }
  }
}
```

**Pros:**
- Works with existing databases
- No schema changes needed
- Can share tables with other systems

## Custom Field Mapping

### Example 1: Snake Case Naming

```json
{
  "TableName": "stripe_payments",
  "FieldMapping": {
    "Id": "id",
    "StripePaymentIntentId": "stripe_payment_intent_id",
    "Status": "status",
    "Amount": "amount",
    "Currency": "currency",
    "ClientSecret": "client_secret",
    "CreatedAt": "created_at",
    "UpdatedAt": "updated_at",
    "Metadata": "metadata"
  }
}
```

### Example 2: Prefixed Fields

```json
{
  "TableName": "transactions",
  "FieldMapping": {
    "Id": "txn_id",
    "StripePaymentIntentId": "txn_stripe_id",
    "Status": "txn_status",
    "Amount": "txn_amount",
    "Currency": "txn_currency",
    "ClientSecret": "txn_secret",
    "CreatedAt": "txn_created",
    "UpdatedAt": "txn_updated",
    "Metadata": "txn_metadata"
  }
}
```

### Example 3: Descriptive Names

```json
{
  "TableName": "PaymentTransactions",
  "FieldMapping": {
    "Id": "TransactionGuid",
    "StripePaymentIntentId": "StripeIntentId",
    "Status": "PaymentStatus",
    "Amount": "AmountInCents",
    "Currency": "CurrencyCode",
    "ClientSecret": "StripeClientSecret",
    "CreatedAt": "CreatedDateUtc",
    "UpdatedAt": "LastModifiedDateUtc",
    "Metadata": "JsonMetadata"
  }
}
```

## Best Practices

### Connection Strings

**Development:**
```json
{
  "ConnectionString": "Data Source=dev_payments.db"
}
```

**Staging:**
```json
{
  "ConnectionString": "Server=staging-db.internal;Database=payments;User=app;Password=${DB_PASSWORD}"
}
```

**Production:**
```json
{
  "ConnectionString": "${DATABASE_CONNECTION_STRING}"
}
```

Use environment variables for sensitive connection strings:
```bash
export DATABASE_CONNECTION_STRING="Server=prod-db;Database=payments;User=app;Password=secure123"
```

### Connection Pooling

Enable connection pooling for better performance:

**SQL Server:**
```
Server=localhost;Database=payments;User=app;Password=secret;Max Pool Size=200;Min Pool Size=10;
```

**PostgreSQL:**
```
Host=localhost;Database=payments;Username=app;Password=secret;Maximum Pool Size=200;Minimum Pool Size=10;
```

**MySQL:**
```
Server=localhost;Database=payments;User=app;Password=secret;MaximumPoolSize=200;MinimumPoolSize=10;
```

### Indexes

Create indexes for frequently queried fields:

```sql
-- Find by Stripe Payment Intent ID (most common)
CREATE INDEX IX_stripe_pi_id ON stripe_transactions(stripe_pi_id);

-- Filter by status
CREATE INDEX IX_payment_status ON stripe_transactions(payment_status);

-- Time-based queries
CREATE INDEX IX_created_timestamp ON stripe_transactions(created_timestamp);
CREATE INDEX IX_updated_timestamp ON stripe_transactions(updated_timestamp);

-- Composite index for status + time queries
CREATE INDEX IX_status_created ON stripe_transactions(payment_status, created_timestamp);
```

### Backup and Retention

**Backup Strategy:**
```sql
-- Archive old transactions (older than 7 years)
CREATE TABLE stripe_transactions_archive LIKE stripe_transactions;

INSERT INTO stripe_transactions_archive
SELECT * FROM stripe_transactions
WHERE created_timestamp < DATE_SUB(NOW(), INTERVAL 7 YEAR);

DELETE FROM stripe_transactions
WHERE created_timestamp < DATE_SUB(NOW(), INTERVAL 7 YEAR);
```

**Retention Policies:**
- Active transactions: Keep in main table
- Completed (>1 year): Consider archiving
- Failed/Cancelled (>6 months): Consider purging
- Always comply with regulatory requirements (PCI-DSS, GDPR, etc.)

### Performance Tuning

**For high-throughput scenarios:**

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithEntityFramework(options => 
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            // Enable retry on transient failures
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            
            // Use command timeout for long-running queries
            sqlOptions.CommandTimeout(60);
        });
    });
```

**Query optimization:**
- Add indexes on frequently filtered fields
- Use read replicas for status queries
- Consider partitioning for very large tables
- Monitor slow query logs

## Troubleshooting

### Issue: Database Connection Fails

**Symptoms:**
```
Unable to connect to database
```

**Solutions:**
1. Verify connection string is correct
2. Check database server is running
3. Verify network connectivity
4. Check firewall rules
5. Verify user credentials and permissions

### Issue: Table Not Found

**Symptoms:**
```
Invalid object name 'stripe_transactions'
```

**Solutions:**
1. Run `EnsureDatabaseCreatedAsync()` on startup
2. Apply migrations: `dotnet ef database update`
3. Create table manually
4. Verify `TableName` configuration matches actual table

### Issue: Column Not Found

**Symptoms:**
```
Invalid column name 'stripe_pi_id'
```

**Solutions:**
1. Verify `FieldMapping` matches database schema
2. Check for typos in field names
3. Ensure case sensitivity matches database collation
4. Recreate table with correct schema

### Issue: Permission Denied

**Symptoms:**
```
CREATE/ALTER permission denied
```

**Solutions:**
1. Grant necessary permissions to database user:
   ```sql
   GRANT CREATE, ALTER, INSERT, UPDATE, SELECT ON DATABASE payments TO appuser;
   ```
2. Use separate users for migrations (elevated) and runtime (restricted)

### Issue: Connection Pool Exhausted

**Symptoms:**
```
Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.
```

**Solutions:**
1. Increase pool size in connection string
2. Ensure connections are properly disposed
3. Check for connection leaks
4. Scale up database server resources

### Issue: Slow Queries

**Symptoms:**
- High response times
- Timeout errors

**Solutions:**
1. Add indexes on commonly filtered fields
2. Analyze query execution plans
3. Enable query statistics
4. Consider database-specific optimizations
5. Use connection pooling
6. Scale database resources

## Examples

### Complete SQL Server Setup

**1. appsettings.json:**
```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "Database": {
      "Enabled": true,
      "ConnectionString": "Server=localhost;Database=StripePayments;Trusted_Connection=true;TrustServerCertificate=true;",
      "Provider": "SqlServer"
    }
  }
}
```

**2. Program.cs:**
```csharp
using FilthyRodriguez.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithEntityFramework();

var app = builder.Build();

// Development: Auto-create database
if (app.Environment.IsDevelopment())
{
    await app.Services.EnsureDatabaseCreatedAsync();
}

app.UseWebSockets();
app.MapStripePaymentEndpoints("/api/stripe");
app.MapStripeWebSocket("/stripe/ws");

app.Run();
```

### Complete PostgreSQL Setup with Custom Names

**1. appsettings.json:**
```json
{
  "FilthyRodriguez": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "Database": {
      "Enabled": true,
      "ConnectionString": "Host=localhost;Database=payments;Username=paymentapp;Password=secret123",
      "Provider": "PostgreSQL",
      "TableName": "payment_records",
      "FieldMapping": {
        "Id": "id",
        "StripePaymentIntentId": "stripe_intent_id",
        "Status": "status",
        "Amount": "amount_cents",
        "Currency": "currency",
        "ClientSecret": "client_secret",
        "CreatedAt": "created_at",
        "UpdatedAt": "updated_at",
        "Metadata": "metadata"
      }
    }
  }
}
```

**2. Create Table Manually:**
```sql
CREATE TABLE payment_records (
    id VARCHAR(100) PRIMARY KEY,
    stripe_intent_id VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL,
    amount_cents BIGINT NOT NULL,
    currency VARCHAR(3) NOT NULL,
    client_secret VARCHAR(500),
    metadata TEXT,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

CREATE INDEX idx_stripe_intent ON payment_records(stripe_intent_id);
CREATE INDEX idx_status ON payment_records(status);
CREATE INDEX idx_created ON payment_records(created_at);
```

**3. Program.cs:**
```csharp
using FilthyRodriguez.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithEntityFramework();

var app = builder.Build();

app.UseWebSockets();
app.MapStripePaymentEndpoints("/api/stripe");
app.MapStripeWebSocket("/stripe/ws");

app.Run();
```

## Security Considerations

1. **Never commit connection strings** with real credentials to version control
2. **Use environment variables** for sensitive configuration
3. **Limit database user permissions** to only what's needed
4. **Enable SSL/TLS** for database connections in production
5. **Regularly rotate** database credentials
6. **Monitor** for suspicious database activity
7. **Backup** transaction data regularly
8. **Comply** with PCI-DSS requirements for payment data

## Support

For issues or questions about database configuration:
- Check this guide first
- Review error messages carefully
- Search GitHub issues
- Open a new issue with:
  - Database provider and version
  - Error messages (sanitize connection strings!)
  - Configuration (remove secrets!)
  - Steps to reproduce
