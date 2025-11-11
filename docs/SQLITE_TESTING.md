# SQLite Database Testing Guide

This guide helps you test the database persistence features of FilthyRodriguez using SQLite.

## Quick Start

### Option 1: Using the Test Script (Recommended)

```bash
cd examples/HtmlTestApp
./test-sqlite.sh
```

### Option 2: Manual Setup

```bash
cd examples/HtmlTestApp
export ASPNETCORE_ENVIRONMENT=Sqlite
dotnet run
```

## Configuration

The SQLite configuration is in `appsettings.Sqlite.json`:

```json
{
  "FilthyRodriguez": {
    "Database": {
      "Enabled": true,
      "Provider": "SQLite",
      "ConnectionString": "Data Source=filthy_rodriguez_test.db",
      "TableName": "stripe_transactions"
    }
  }
}
```

## Testing Steps

1. **Start the Application**
   ```bash
   ./test-sqlite.sh
   ```

2. **Open Browser**
   Navigate to: http://localhost:5000

3. **Make Test Payment**
   - Use test card: `4242 4242 4242 4242`
   - Any future expiry date
   - Any CVC code

4. **Verify Database**
   ```bash
   # View all transactions
   sqlite3 filthy_rodriguez_test.db "SELECT * FROM stripe_transactions;"
   
   # View table schema
   sqlite3 filthy_rodriguez_test.db ".schema stripe_transactions"
   
   # Count transactions
   sqlite3 filthy_rodriguez_test.db "SELECT COUNT(*) FROM stripe_transactions;"
   
   # View recent transactions
   sqlite3 filthy_rodriguez_test.db "SELECT transaction_id, payment_status, amount_cents, created_timestamp FROM stripe_transactions ORDER BY created_timestamp DESC LIMIT 5;"
   ```

## Database Schema

The default table structure:

| Column | Type | Description |
|--------|------|-------------|
| `transaction_id` | TEXT PRIMARY KEY | Unique transaction identifier |
| `stripe_pi_id` | TEXT | Stripe Payment Intent ID |
| `payment_status` | TEXT | Status (succeeded, failed, pending) |
| `amount_cents` | INTEGER | Amount in cents |
| `currency_code` | TEXT | Currency (usd, eur, etc.) |
| `client_secret` | TEXT | Stripe client secret |
| `created_timestamp` | TEXT | When transaction was created |
| `updated_timestamp` | TEXT | When transaction was last updated |
| `metadata_json` | TEXT | Additional metadata as JSON |

## Useful SQL Queries

### View Successful Payments
```sql
sqlite3 filthy_rodriguez_test.db "
SELECT 
    transaction_id,
    amount_cents / 100.0 as amount_dollars,
    currency_code,
    payment_status,
    datetime(created_timestamp) as created
FROM stripe_transactions 
WHERE payment_status = 'succeeded'
ORDER BY created_timestamp DESC;
"
```

### View Failed Payments
```sql
sqlite3 filthy_rodriguez_test.db "
SELECT 
    transaction_id,
    payment_status,
    created_timestamp
FROM stripe_transactions 
WHERE payment_status != 'succeeded'
ORDER BY created_timestamp DESC;
"
```

### Payment Statistics
```sql
sqlite3 filthy_rodriguez_test.db "
SELECT 
    payment_status,
    COUNT(*) as count,
    SUM(amount_cents) / 100.0 as total_amount
FROM stripe_transactions 
GROUP BY payment_status;
"
```

## Testing Different Scenarios

### 1. Successful Payment
- Card: `4242 4242 4242 4242`
- Expected: Transaction saved with status `succeeded`

### 2. Declined Payment
- Card: `4000 0000 0000 0002`
- Expected: Transaction may or may not be saved depending on Stripe response

### 3. Application Restart Persistence
```bash
# Make a payment
# Stop the app (Ctrl+C)
# Restart with: ./test-sqlite.sh
# Check database - transaction should still exist
```

### 4. Concurrent Payments
- Open multiple browser tabs
- Make payments simultaneously
- Verify all transactions are saved correctly

## Testing Extended Data Capture

To test with extended transaction data, use the `appsettings.Extended.json` configuration:

```bash
export ASPNETCORE_ENVIRONMENT=Extended
dotnet run
```

Or create a test script:
```bash
#!/bin/bash
cd examples/HtmlTestApp
export ASPNETCORE_ENVIRONMENT=Extended
dotnet run
```

### Extended Data Queries

**View All Extended Fields:**
```sql
sqlite3 filthy_rodriguez_extended.db "
SELECT 
    transaction_id,
    customer_email,
    payment_method_type,
    card_brand,
    card_last4,
    amount_cents / 100.0 as amount_dollars,
    payment_status
FROM stripe_transactions 
ORDER BY created_timestamp DESC 
LIMIT 5;
"
```

**Customer Analysis:**
```sql
sqlite3 filthy_rodriguez_extended.db "
SELECT 
    customer_email,
    COUNT(*) as num_transactions,
    SUM(amount_cents) / 100.0 as total_spent,
    MAX(created_timestamp) as last_purchase
FROM stripe_transactions
WHERE customer_email IS NOT NULL
GROUP BY customer_email
ORDER BY total_spent DESC;
"
```

**Card Brand Breakdown:**
```sql
sqlite3 filthy_rodriguez_extended.db "
SELECT 
    card_brand,
    COUNT(*) as transactions,
    SUM(amount_cents) / 100.0 as total_volume
FROM stripe_transactions
WHERE card_brand IS NOT NULL
GROUP BY card_brand;
"
```

**Payment Method Analysis:**
```sql
sqlite3 filthy_rodriguez_extended.db "
SELECT 
    payment_method_type,
    payment_status,
    COUNT(*) as count
FROM stripe_transactions
WHERE payment_method_type IS NOT NULL
GROUP BY payment_method_type, payment_status;
"
```

### Extended Schema Verification

```bash
sqlite3 filthy_rodriguez_extended.db ".schema stripe_transactions"
```

You should see additional columns:
- `customer_id`
- `customer_email`
- `payment_method_id`
- `payment_method_type`
- `card_last4`
- `card_brand`
- `description`
- `receipt_email`
- `captured_amount`
- `refunded_amount`
- `application_fee_amount`

## Cleanup

To start fresh:
```bash
rm filthy_rodriguez_test.db
./test-sqlite.sh
```

## Troubleshooting

### "Database is locked"
- Close any open sqlite3 connections
- Stop the application
- Delete the database file and restart

### "Table not found"
- Ensure the application started successfully
- Check logs for "✓ Database initialized successfully"

### No transactions appearing
- Verify `Database.Enabled` is `true` in configuration
- Check application logs for EF Core messages
- Ensure you're running with `ASPNETCORE_ENVIRONMENT=Sqlite`

## Advanced: Custom Field Mapping

You can customize field names in `appsettings.Sqlite.json`:

```json
{
  "FilthyRodriguez": {
    "Database": {
      "Enabled": true,
      "Provider": "SQLite",
      "ConnectionString": "Data Source=filthy_rodriguez_test.db",
      "TableName": "my_payments",
      "FieldMapping": {
        "Id": "id",
        "StripePaymentIntentId": "stripe_id",
        "Status": "status",
        "Amount": "amount",
        "Currency": "currency",
        "CreatedAt": "created_at",
        "UpdatedAt": "updated_at"
      }
    }
  }
}
```

## See Also

- [Database Integration Guide](../../docs/DATABASE_INTEGRATION.md) - Complete integration scenarios
- [Database Documentation](../../docs/DATABASE.md) - Detailed database features
- [Main README](../../README.md) - Project overview
