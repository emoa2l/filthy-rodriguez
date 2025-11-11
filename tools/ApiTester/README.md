# Stripe Payment Plugin API Tester

A command-line tool for testing all Stripe Payment Plugin API endpoints.

## Features

- **Payment Creation** - Create payment intents with custom amounts and metadata
- **Payment Confirmation** - Confirm payments with Stripe test cards (triggers webhooks!)
- **Status Checking** - Retrieve payment intent status
- **Refund Processing** - Full or partial refunds
- **Health Checks** - Verify API availability
- **Interactive Mode** - Run all tests in sequence

## Prerequisites

1. The Stripe Payment Plugin API must be running (examples/HtmlTestApp or tools/WebhookTestSubscriber)
2. Valid Stripe test API key configured in the running application

## Testing Webhooks - Quick Workflow

**IMPORTANT**: Webhooks only fire when payment status changes. Follow this workflow to test webhooks:

```bash
# 1. Create a payment (status: requires_payment_method)
dotnet run -- payment --amount 2000 --description "Test payment"
# Copy the payment intent ID from the output

# 2. Confirm the payment with a test card (triggers webhook!)
dotnet run -- confirm --id pi_XXXXXXXXXX --card 4242424242424242

# 3. Watch the WebhookTestSubscriber console for webhook events!
```

### Available Test Cards

- `4242424242424242` - Visa (succeeds)
- `4000000000000002` - Generic decline
- `4000002500003155` - Requires 3D Secure authentication
- `4000000000009995` - Insufficient funds decline

See [Stripe Test Cards](https://stripe.com/docs/testing#cards) for more options.

## Quick Start

### Run Health Check

```bash
cd tools/ApiTester
dotnet run -- health
```

### Create a Payment

```bash
dotnet run -- payment --amount 2000 --currency usd --description "Test payment"
```

### Confirm a Payment (NEW!)

Confirming a payment changes its status and triggers webhook events:

```bash
dotnet run -- confirm --id pi_3SS6QK21kiZimfCZ0eQrd8PE --card 4242424242424242
```

### Check Payment Status

```bash
dotnet run -- status --id pi_3SS6QK21kiZimfCZ0eQrd8PE
```

### Process a Refund

```bash
# Full refund
dotnet run -- refund --id pi_3SS6QK21kiZimfCZ0eQrd8PE

# Partial refund
dotnet run -- refund --id pi_3SS6QK21kiZimfCZ0eQrd8PE --amount 1000
```

### Interactive Test Session

Run all tests in sequence with prompts:

```bash
dotnet run -- interactive
```

## Usage

### Global Options

- `--url`, `-u` - API base URL (default: `http://localhost:5120/api/stripe`)

### Commands

#### `payment` - Create a Payment Intent

Creates a new payment intent.

**Options:**
- `--amount`, `-a` - Amount in cents (default: 2000 = $20.00)
- `--currency`, `-c` - Currency code (default: usd)
- `--description`, `-d` - Payment description

**Example:**
```bash
dotnet run -- payment -a 5000 -c eur -d "European payment"
```

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║  💳 Creating Payment Intent                               ║
╚═══════════════════════════════════════════════════════════╝

Request URL: http://localhost:5120/api/stripe/payment
Amount:      $50.00
Currency:    EUR
Description: European payment

✅ Success!

Payment Intent ID: pi_3SS6QK21kiZimfCZ0eQrd8PE
Status:            requires_payment_method
Client Secret:     pi_3SS6QK21kiZimfCZ...

💡 Next steps:
   - Confirm payment: dotnet run -- confirm --id pi_3SS6QK21kiZimfCZ0eQrd8PE
   - Check status: dotnet run -- status --id pi_3SS6QK21kiZimfCZ0eQrd8PE
```

#### `confirm` - Confirm Payment Intent (NEW!)

Confirms a payment intent with a test card. **This triggers webhook events!**

**Options:**
- `--id`, `-i` - Payment Intent ID (required)
- `--card`, `-c` - Test card number (default: 4242424242424242)

**Example:**
```bash
dotnet run -- confirm --id pi_3SS6QK21kiZimfCZ0eQrd8PE --card 4242424242424242
```

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║  ✅ Confirming Payment Intent                             ║
╚═══════════════════════════════════════════════════════════╝

Request URL:       http://localhost:5120/api/stripe/confirm
Payment Intent ID: pi_3SS6QK21kiZimfCZ0eQrd8PE
Test Card Number:  4242424242424242

✅ Success!

Payment Intent ID: pi_3SS6QK21kiZimfCZ0eQrd8PE
Status:            succeeded
Amount:            $20.00
Currency:          USD

💡 Tip: Check your webhook subscriber to see the event notifications!
```

**Available Test Cards:**
- `4242424242424242` - Visa (succeeds)
- `4000000000000002` - Generic decline
- `4000002500003155` - Requires 3D Secure authentication
- `4000000000009995` - Insufficient funds decline
- More at [Stripe Test Cards](https://stripe.com/docs/testing#cards)


#### `status` - Get Payment Status

Retrieves the current status of a payment intent.

**Options:**
- `--id`, `-i` - Payment Intent ID (required)

**Example:**
```bash
dotnet run -- status --id pi_3SS6QK21kiZimfCZ0eQrd8PE
```

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║  🔍 Getting Payment Status                                ║
╚═══════════════════════════════════════════════════════════╝

Request URL:       http://localhost:5000/api/stripe/status/pi_3SS6QK21kiZimfCZ0eQrd8PE
Payment Intent ID: pi_3SS6QK21kiZimfCZ0eQrd8PE

✅ Success!

Payment Intent ID: pi_3SS6QK21kiZimfCZ0eQrd8PE
Status:            succeeded
Amount:            $20.00
Currency:          USD
```

#### `refund` - Process a Refund

Processes a full or partial refund for a payment intent.

**Options:**
- `--id`, `-i` - Payment Intent ID (required)
- `--amount`, `-a` - Amount to refund in cents (optional, omit for full refund)
- `--reason`, `-r` - Refund reason (default: requested_by_customer)
  - Options: `requested_by_customer`, `duplicate`, `fraudulent`

**Examples:**
```bash
# Full refund
dotnet run -- refund --id pi_3SS6QK21kiZimfCZ0eQrd8PE

# Partial refund
dotnet run -- refund --id pi_3SS6QK21kiZimfCZ0eQrd8PE --amount 1000

# Refund with reason
dotnet run -- refund --id pi_3SS6QK21kiZimfCZ0eQrd8PE --reason duplicate
```

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║  💸 Processing Refund                                     ║
╚═══════════════════════════════════════════════════════════╝

Request URL:       http://localhost:5120/api/stripe/refund
Payment Intent ID: pi_3SS6QK21kiZimfCZ0eQrd8PE
Amount:            Full refund
Reason:            requested_by_customer

✅ Success!

Refund ID:    re_3SS6QK21kiZimfCZ0abcdefg
Status:       succeeded
Amount:       $20.00
```

#### `health` - Check API Health

Verifies that the API is running and healthy.

**Example:**
```bash
dotnet run -- health
```

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║  🏥 Checking API Health                                   ║
╚═══════════════════════════════════════════════════════════╝

Request URL: http://localhost:5120/api/stripe/health

✅ Healthy!

Status:    healthy
Timestamp: 2025-11-11T01:36:05.136Z
```

#### `interactive` - Run Interactive Test Session

Runs all API tests in sequence with prompts between each step.

**Example:**
```bash
dotnet run -- interactive
```

This will:
1. Check API health
2. Create a test payment
3. Check the payment status
4. Process a full refund

## Different API Endpoint

If your API is running on a different port or URL:

```bash
dotnet run -- --url http://localhost:5120/api/stripe payment -a 3000
```

## Example Workflow - Testing with Webhooks

```bash
# 1. Start the WebhookTestSubscriber (in one terminal)
cd tools/WebhookTestSubscriber
dotnet run

# 2. In another terminal, run the API tester
cd tools/ApiTester

# 3. Check health
dotnet run -- health

# 4. Create a payment (status: requires_payment_method)
dotnet run -- payment -a 2000 -c usd -d "Test order #12345"
# Copy the payment intent ID from output

# 5. Confirm the payment with test card (triggers webhook!)
dotnet run -- confirm --id pi_XXXXXXXXXXXXX --card 4242424242424242
# Watch the WebhookTestSubscriber terminal for webhook events!

# 6. Check status (should show 'succeeded')
dotnet run -- status --id pi_XXXXXXXXXXXXX

# 7. Process refund (triggers another webhook!)
dotnet run -- refund --id pi_XXXXXXXXXXXXX
# Watch the WebhookTestSubscriber terminal again!
```

## Example Workflow - Without Webhooks

```bash
# 1. Start the HtmlTestApp (in another terminal)
cd examples/HtmlTestApp
dotnet run

# 2. Check health
cd tools/ApiTester
dotnet run -- health

# 3. Create a payment
dotnet run -- payment -a 2000 -c usd -d "Test order #12345"

# 4. Confirm payment with test card
dotnet run -- confirm --id pi_XXXXXXXXXXXXX

# 5. Check status
dotnet run -- status --id pi_XXXXXXXXXXXXX

# 6. Process refund
dotnet run -- refund --id pi_XXXXXXXXXXXXX
```

## Troubleshooting

### Connection Refused

Make sure the API is running:
```bash
cd examples/HtmlTestApp
dotnet run
```

### Invalid Payment Intent ID

The payment intent ID must:
- Start with `pi_`
- Be from the same Stripe account as the API's configured key
- Actually exist (create one first with the `payment` command)

### API Errors

Check the API logs for more details. Common issues:
- Invalid Stripe API key
- Network connectivity to Stripe
- Invalid request parameters

## See Also

- [Main Plugin README](../../README.md)
- [Webhook Test Subscriber](../WebhookTestSubscriber/README.md)
- [Stripe API Documentation](https://stripe.com/docs/api)
