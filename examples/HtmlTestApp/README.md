# HTML Test App - Stripe Payment Plugin

A simple HTML-based test application that demonstrates the complete credit card payment flow using the Stripe Payment Plugin, including payment creation, real-time updates via WebSocket, and redirect to success/cancel pages.

## Features

- 💳 **Payment Form**: Simple, responsive payment form with product summary
- 🚀 **Quick Test**: One-click test payment using Stripe test card tokens
- 🔄 **Real-time Updates**: WebSocket integration for instant payment status updates
- ✅ **Success Page**: Beautiful confirmation page with payment details
- ❌ **Cancel Page**: Helpful cancellation page with retry options
- � **Refund Page**: Full-featured refund interface for testing refunds
- �📱 **Responsive Design**: Works on desktop and mobile devices
- 🎨 **Modern UI**: Clean, professional interface with animations

## What This Demonstrates

This example app shows the complete payment flow including refunds:

1. **Payment Creation** - Create a payment intent via API
2. **Payment Confirmation** - Confirm payment with test card token
3. **Real-time Updates** - WebSocket notifications of status changes
4. **Automatic Redirect** - Redirect to success/cancel pages based on payment status
5. **Payment Details** - Display payment information on confirmation pages
6. **Refund Processing** - Test full and partial refunds with detailed UI

## Getting Started

### Prerequisites

- .NET 8 SDK installed
- A Stripe account with test API keys ([Get one here](https://dashboard.stripe.com/test/apikeys))

### Setup

1. **Update Configuration**

   Edit `appsettings.json` and add your Stripe test API key:

   ```json
   {
     "FilthyRodriguez": {
       "ApiKey": "sk_test_your_actual_stripe_api_key_here",
       "WebhookSecret": "whsec_your_webhook_secret_here"
     }
   }
   ```

   > **Note**: The webhook secret is optional for this test app. It's only needed if you want to test webhook events with Stripe CLI.

2. **Run the Application**

   ```bash
   cd examples/HtmlTestApp
   dotnet run
   ```

   Or **test with SQLite database persistence**:

   ```bash
   cd examples/HtmlTestApp
   ./test-sqlite.sh
   ```

   See [SQLITE_TESTING.md](SQLITE_TESTING.md) for complete database testing guide.

3. **Open in Browser**

   Navigate to: `http://localhost:5000`

## Usage

### Quick Test (Recommended)

1. Click the **"Test with Visa (Auto-Success)"** button
2. The app will:
   - Create a payment intent ($20.00)
   - Connect to WebSocket for real-time updates
   - Confirm the payment with a test Visa card token
   - Automatically redirect to the success page
3. View payment details on the success page

### Manual Testing with Test Cards

While Stripe Elements integration is simplified in this demo, you can test various scenarios by modifying the Quick Test button to use different test payment method tokens:

- `pm_card_visa` - Success (default)
- `pm_card_chargeDeclined` - Generic decline
- `pm_card_authenticationRequired` - Requires 3D Secure
- `pm_card_chargeDeclinedInsufficientFunds` - Insufficient funds

See [Stripe Test Cards Documentation](https://stripe.com/docs/testing#cards) for more test tokens.

## Project Structure

```
HtmlTestApp/
├── Program.cs                 # ASP.NET Core app configuration
├── appsettings.json          # Configuration (add your API keys here)
├── HtmlTestApp.csproj        # Project file
└── wwwroot/                  # Static files
    ├── index.html            # Main payment page
    ├── success.html          # Payment success page
    ├── cancel.html           # Payment canceled page
    └── refund.html           # Refund testing page
```

## API Endpoints

The app uses these Stripe Payment Plugin endpoints:

- `POST /api/stripe/payment` - Create payment intent
- `POST /api/stripe/confirm` - Confirm payment (test mode)
- `GET /api/stripe/status/{id}` - Get payment status
- `WS /stripe/ws` - WebSocket for real-time updates

## How It Works

### Payment Flow

1. **User clicks "Quick Test"**
   - JavaScript creates a payment intent via POST to `/api/stripe/payment`
   - Response includes payment intent ID and status

2. **WebSocket Connection**
   - App connects to `/stripe/ws` WebSocket endpoint
   - Subscribes to the specific payment intent ID
   - Listens for status updates

3. **Payment Confirmation**
   - JavaScript confirms payment via POST to `/api/stripe/confirm`
   - Uses Stripe test payment method token (`pm_card_visa`)
   - Payment status changes to "succeeded"

4. **Real-time Update & Redirect**
   - Webhook fires internally (payment_intent.succeeded)
   - WebSocket receives status update
   - JavaScript automatically redirects to `/success.html?payment_intent=pi_xxx`

5. **Success Page**
   - Fetches payment details via GET to `/api/stripe/status/{id}`
   - Displays formatted payment information
   - Shows raw JSON for debugging

### WebSocket Integration

The WebSocket connection enables real-time status updates without polling:

```javascript
// Connect to WebSocket
const ws = new WebSocket('ws://localhost:5000/stripe/ws');

// Subscribe to payment updates
ws.send(JSON.stringify({
    action: 'subscribe',
    paymentId: 'pi_...'
}));

// Listen for updates
ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    if (data.type === 'payment_update') {
        // Handle status change, redirect if needed
    }
};
```

## Testing with Stripe CLI

To test webhook events:

1. **Install Stripe CLI**
   ```bash
   # See: https://stripe.com/docs/stripe-cli
   brew install stripe/stripe-cli/stripe  # macOS
   # or download from: https://github.com/stripe/stripe-cli/releases
   ```

2. **Login to Stripe**
   ```bash
   stripe login
   ```

3. **Forward Webhooks**
   ```bash
   stripe listen --forward-to http://localhost:5000/api/stripe/webhook
   ```

4. **Copy Webhook Secret**
   - Copy the `whsec_...` secret from the CLI output
   - Update `appsettings.json` with the secret
   - Restart the app

5. **Trigger Test Events**
   ```bash
   stripe trigger payment_intent.succeeded
   ```

## Customization

### Change Payment Amount

Edit `wwwroot/index.html` and modify the `createPaymentIntent()` function:

```javascript
body: JSON.stringify({
    amount: 5000,  // $50.00 in cents
    currency: 'usd',
    description: 'Your custom description'
})
```

### Change Product Display

Edit the HTML in `wwwroot/index.html`:

```html
<div class="product-info">
    <h2>Order Summary</h2>
    <div class="product-item">
        <span>Your Product</span>
        <span>$XX.XX</span>
    </div>
    <!-- Add more items -->
</div>
```

### Add Styling

All styles are embedded in the HTML files. Modify the `<style>` sections to customize:
- Colors
- Fonts
- Layout
- Animations

## Troubleshooting

### "Failed to create payment intent"

- Check that your Stripe API key is correct in `appsettings.json`
- Ensure you're using a test key (starts with `sk_test_`)
- Verify the app is running and accessible

### WebSocket Connection Fails

- Check browser console for errors
- Ensure WebSockets are enabled (`app.UseWebSockets()` in Program.cs)
- Some proxies/firewalls may block WebSocket connections

### Redirect Doesn't Happen

- Check browser console for JavaScript errors
- Verify WebSocket connection is established
- Test with browser dev tools Network tab to see API responses

## Production Considerations

This is a **test/demo application**. For production use:

1. **Use Stripe.js Properly** - Integrate full Stripe Elements for PCI compliance
2. **Never Expose API Keys** - Use publishable keys in frontend, secret keys in backend only
3. **Implement Proper Security** - Add authentication, CSRF protection, rate limiting
4. **Handle All Payment States** - Process all Stripe payment statuses properly
5. **Use HTTPS** - WebSocket should use WSS protocol in production
6. **Add Error Handling** - Comprehensive error handling and user feedback
7. **Test All Scenarios** - Test different cards, currencies, and failure modes

## Learn More

- [Stripe Payment Plugin Documentation](../../README.md)
- [Stripe API Documentation](https://stripe.com/docs/api)
- [Stripe Testing Guide](https://stripe.com/docs/testing)
- [Stripe Elements](https://stripe.com/docs/stripe-js)

## License

This example is part of the Stripe Payment Plugin project and is licensed under the MIT License.
