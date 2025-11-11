# Screenshots for Documentation

This directory contains screenshots used in the main README and documentation.

## Required Screenshots

### 1. payment-form.png
- **Where**: HtmlTestApp home page (http://localhost:5000)
- **What to capture**: The payment form showing amount input field and "Pay Now" button
- **Recommended size**: 800x600px

### 2. processing.png
- **Where**: During payment processing (before redirect)
- **What to capture**: "Processing payment..." message or loading state
- **Recommended size**: 800x600px

### 3. receipt.png
- **Where**: Success page after payment (http://localhost:5000/success.html)
- **What to capture**: Receipt showing transaction ID, amount, status, timestamp, and "Request Refund" button
- **Recommended size**: 800x600px

### 4. refund.png
- **Where**: After clicking "Request Refund" on success page
- **What to capture**: Refund confirmation showing original payment details and refund status
- **Recommended size**: 800x600px

## How to Create Screenshots

1. Start the test app:
   ```bash
   cd examples/HtmlTestApp
   dotnet run
   ```

2. Open http://localhost:5000 in your browser

3. Take screenshot 1: Initial payment form

4. Enter amount (e.g., 50.00) and click "Pay Now"

5. Use test card: 4242 4242 4242 4242, any future date, any CVC

6. Take screenshot 2: Processing state (if visible)

7. Wait for redirect to success page

8. Take screenshot 3: Success/receipt page

9. Click "Request Refund"

10. Take screenshot 4: Refund confirmation

## Screenshot Guidelines

- Use clean browser window (hide bookmarks bar, etc.)
- Capture enough context to show the UI clearly
- Use consistent browser width (recommend 1200px)
- Save as PNG format for quality
- Optimize file size (recommend using tinypng.com or similar)

## Alternative: Screen Recording

You can also create an animated GIF showing the entire flow:

```bash
# macOS: Use QuickTime Player > File > New Screen Recording
# Windows: Use Xbox Game Bar (Win + G)
# Linux: Use SimpleScreenRecorder or Peek

# Convert to GIF with ffmpeg:
ffmpeg -i recording.mov -vf "fps=10,scale=800:-1:flags=lanczos" -c:v gif payment-flow.gif
```

Add to README as:
```markdown
![Payment Flow Demo](docs/images/payment-flow.gif)
```
