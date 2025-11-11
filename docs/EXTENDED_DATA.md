# Extended Data Capture - Feature Summary

## Overview

FilthyRodriguez now supports optional **Extended Data Capture** for database persistence. When enabled, the system automatically captures and stores additional transaction details from Stripe PaymentIntents beyond the basic transaction data.

## Quick Start

### Enable Extended Data Capture

In your `appsettings.json`:

```json
{
  "FilthyRodriguez": {
    "Database": {
      "Enabled": true,
      "ConnectionString": "Data Source=payments.db",
      "Provider": "SQLite",
      "CaptureExtendedData": true
    }
  }
}
```

### What Gets Captured

#### Standard Fields (Always Captured)
- Transaction ID
- Stripe Payment Intent ID
- Payment Status
- Amount & Currency
- Client Secret
- Metadata (JSON)
- Created/Updated Timestamps

#### Extended Fields (When CaptureExtendedData = true)
- **Customer Information**
  - Customer ID
  - Customer Email
  
- **Payment Method Details**
  - Payment Method ID
  - Payment Method Type (card, bank_account, etc.)
  - Card Last 4 Digits
  - Card Brand (visa, mastercard, amex, etc.)
  
- **Transaction Details**
  - Description
  - Receipt Email
  
- **Financial Information**
  - Captured Amount
  - Refunded Amount
  - Application Fee Amount

## Use Cases

### 1. Customer Analytics
Track customer spending patterns and identify your top customers:

```sql
SELECT customer_email, 
       COUNT(*) as transactions,
       SUM(amount_cents) / 100.0 as total_spent
FROM stripe_transactions
WHERE payment_status = 'succeeded'
  AND customer_email IS NOT NULL
GROUP BY customer_email
ORDER BY total_spent DESC;
```

### 2. Payment Method Analysis
Understand which card brands and payment types your customers prefer:

```sql
SELECT card_brand, 
       COUNT(*) as count,
       SUM(amount_cents) / 100.0 as volume
FROM stripe_transactions
WHERE card_brand IS NOT NULL
GROUP BY card_brand;
```

### 3. Financial Reporting
Track refunds and net revenue:

```sql
SELECT 
    DATE(created_timestamp) as date,
    SUM(amount_cents) / 100.0 as gross,
    SUM(COALESCE(refunded_amount, 0)) / 100.0 as refunds,
    SUM(amount_cents - COALESCE(refunded_amount, 0)) / 100.0 as net
FROM stripe_transactions
WHERE payment_status = 'succeeded'
GROUP BY DATE(created_timestamp)
ORDER BY date DESC;
```

### 4. Customer Support
Quick customer lookup by email or card last 4:

```sql
SELECT * FROM stripe_transactions
WHERE customer_email = 'customer@example.com'
   OR card_last4 = '4242'
ORDER BY created_timestamp DESC;
```

## Performance

- **Storage Impact**: ~200-500 bytes per transaction
- **Query Performance**: No impact on basic queries
- **Indexes**: Customer ID and email indexed for fast lookups
- **Optional**: Zero impact when disabled

## Migration

### New Installation
Simply set `CaptureExtendedData: true` - the extended schema will be created automatically.

### Existing Installation
1. **Backup your database**
2. **Drop and recreate** (development):
   ```bash
   rm payments.db
   dotnet run
   ```
3. **Or use migrations** (production):
   ```bash
   dotnet ef migrations add AddExtendedFields
   dotnet ef database update
   ```

## Testing

### SQLite Example

**Configuration**: `appsettings.Extended.json`
```json
{
  "FilthyRodriguez": {
    "Database": {
      "Enabled": true,
      "ConnectionString": "Data Source=payments_extended.db",
      "Provider": "SQLite",
      "CaptureExtendedData": true
    }
  }
}
```

**Run Test**:
```bash
cd examples/HtmlTestApp
export ASPNETCORE_ENVIRONMENT=Extended
dotnet run
```

**Verify Data**:
```bash
sqlite3 payments_extended.db "
SELECT transaction_id, customer_email, card_brand, card_last4, amount_cents / 100.0 
FROM stripe_transactions 
WHERE customer_email IS NOT NULL;
"
```

## Configuration Reference

```json
{
  "FilthyRodriguez": {
    "Database": {
      "Enabled": true,                          // Enable database persistence
      "ConnectionString": "...",                // Your database connection
      "Provider": "SQLite",                     // SqlServer, PostgreSQL, MySQL, SQLite
      "TableName": "stripe_transactions",       // Custom table name (optional)
      "CaptureExtendedData": true,              // Enable extended data capture
      "FieldMapping": {                         // Custom field names (optional)
        "CustomerId": "customer_id",
        "CustomerEmail": "customer_email",
        "PaymentMethodId": "payment_method_id",
        "CardBrand": "card_brand",
        "CardLast4": "card_last4"
        // ... more field mappings
      }
    }
  }
}
```

## FAQ

**Q: Does this affect performance?**
A: No noticeable impact. Extended fields are optional/nullable and indexed where needed.

**Q: Can I enable this on an existing database?**
A: Yes, but you'll need to update the schema. See Migration section above.

**Q: What if I don't want all extended fields?**
A: The fields are captured automatically from Stripe. If Stripe doesn't provide a value (e.g., no customer_id), it remains NULL.

**Q: Can I customize field names?**
A: Yes, use the `FieldMapping` configuration to map to your preferred column names.

**Q: Is this production-ready?**
A: The database feature is experimental. Extended data capture is built on the same foundation and has the same stability level. Test thoroughly before production use.

## See Also

- [Database Configuration Guide](../../docs/DATABASE.md)
- [Database Integration Guide](../../docs/DATABASE_INTEGRATION.md)
- [SQLite Testing Guide](SQLITE_TESTING.md)
- [Main README](../../README.md)
