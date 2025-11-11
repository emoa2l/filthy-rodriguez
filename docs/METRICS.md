# Metrics System

FilthyRodriguez includes a comprehensive **metrics system** that automatically tracks payment operations, refunds, and database activity. The metrics are collected through a pluggable architecture that allows you to send data to any monitoring platform (DataDog, Prometheus, Application Insights, etc.) or simply log them.

## 🚀 Quick Start

### Default Setup (Logging Metrics)

By default, metrics are automatically collected and logged:

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration);
// Metrics are enabled by default and logged via ILogger
```

**Example Output:**
```
📊 METRIC [Counter] stripe.payment.created.count=1 [currency=usd]
📊 METRIC [Counter] stripe.payment.created.amount=9999 [currency=usd]
📊 METRIC [Counter] stripe.payment.confirmed.count=1 [currency=usd]
📊 METRIC [Counter] stripe.payment.confirmed.amount=9999 [currency=usd]
```

### Configuration

Configure metrics behavior via `appsettings.json`:

```json
{
  "StripePayment": {
    "ApiKey": "your_stripe_api_key",
    "Metrics": {
      "Enabled": true,
      "Prefix": "stripe.payment",
      "IncludeDetailedTags": false,
      "LogMetrics": false
    }
  }
}
```

**Configuration Options:**

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable metrics collection globally |
| `Prefix` | string | `"stripe.payment"` | Prefix for all metric names |
| `IncludeDetailedTags` | bool | `false` | Include detailed tags (payment method IDs, customer IDs, etc.) |
| `LogMetrics` | bool | `false` | Write metrics to ILogger (separate from custom sinks) |

### Disabling Metrics

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithoutMetrics();
```

## 📊 Collected Metrics

### Payment Metrics

| Metric Name | Type | Tags | Description |
|-------------|------|------|-------------|
| `{prefix}.created.count` | Counter | `currency` | Number of payment intents created |
| `{prefix}.created.amount` | Counter | `currency` | Total amount of created payments (in cents) |
| `{prefix}.confirmed.count` | Counter | `currency` | Number of successfully confirmed payments |
| `{prefix}.confirmed.amount` | Counter | `currency` | Total amount of confirmed payments (in cents) |
| `{prefix}.failed.count` | Counter | `currency`, `status` | Number of failed payments |
| `{prefix}.failed.amount` | Counter | `currency`, `status` | Total amount of failed payments (in cents) |
| `{prefix}.canceled.count` | Counter | `currency` | Number of canceled payments |
| `{prefix}.canceled.amount` | Counter | `currency` | Total amount of canceled payments (in cents) |

### Refund Metrics

| Metric Name | Type | Tags | Description |
|-------------|------|------|-------------|
| `{prefix}.refund.initiated.count` | Counter | `currency` | Number of refund requests initiated |
| `{prefix}.refund.initiated.amount` | Counter | `currency` | Total amount of initiated refunds (in cents) |
| `{prefix}.refund.initiated.by_reason` | Counter | `reason` | Refunds grouped by reason (duplicate, fraudulent, etc.) |
| `{prefix}.refund.succeeded.count` | Counter | `currency` | Number of successful refunds |
| `{prefix}.refund.succeeded.amount` | Counter | `currency` | Total amount of successful refunds (in cents) |
| `{prefix}.refund.failed.count` | Counter | `currency`, `status` | Number of failed refunds |
| `{prefix}.refund.failed.amount` | Counter | `currency`, `status` | Total amount of failed refunds (in cents) |

### Database Metrics

| Metric Name | Type | Tags | Description |
|-------------|------|------|-------------|
| `{prefix}.database.record_created.count` | Counter | - | Number of transaction records created in database |
| `{prefix}.database.record_updated.count` | Counter | - | Number of transaction records updated in database |
| `{prefix}.database.operations` | Counter | `operation`, `status` | Database operations grouped by type and success/failure |

**Example Tags:**
- `currency`: `"usd"`, `"eur"`, `"gbp"`, etc.
- `status`: `"requires_payment_method"`, `"processing"`, `"succeeded"`, `"canceled"`, etc.
- `reason`: `"duplicate"`, `"fraudulent"`, `"requested_by_customer"`, etc.
- `operation`: `"create"`, `"update"`
- `status`: `"success"`, `"failure"`

## 🔌 Custom Metrics Sinks

### Built-in Sinks

FilthyRodriguez includes **`LoggingMetricsSink`** by default, which writes metrics to `ILogger`.

### Implementing a Custom Sink

Create a class implementing `IMetricsSink`:

```csharp
using FilthyRodriguez.Abstractions;

public class DataDogMetricsSink : IMetricsSink
{
    private readonly ILogger<DataDogMetricsSink> _logger;
    private readonly DogStatsdService _statsd;

    public DataDogMetricsSink(
        ILogger<DataDogMetricsSink> logger,
        DogStatsdService statsd)
    {
        _logger = logger;
        _statsd = statsd;
    }

    public Task EmitCounterAsync(
        string metricName,
        long value,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tagArray = tags?.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
            _statsd.Counter(metricName, value, tags: tagArray);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit counter metric: {MetricName}", metricName);
        }

        return Task.CompletedTask;
    }

    public Task EmitGaugeAsync(
        string metricName,
        double value,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tagArray = tags?.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
            _statsd.Gauge(metricName, value, tags: tagArray);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit gauge metric: {MetricName}", metricName);
        }

        return Task.CompletedTask;
    }

    public Task EmitHistogramAsync(
        string metricName,
        double value,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tagArray = tags?.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
            _statsd.Histogram(metricName, value, tags: tagArray);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit histogram metric: {MetricName}", metricName);
        }

        return Task.CompletedTask;
    }
}
```

### Register the Custom Sink

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithMetricsSink<DataDogMetricsSink>();
```

**Multiple sinks** can be registered simultaneously:

```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithMetricsSink<DataDogMetricsSink>()
    .WithMetricsSink<PrometheusMetricsSink>();
// Plus the default LoggingMetricsSink
```

## 🏗️ Architecture

### Event-Driven Collection

Metrics are collected **automatically** via the `MetricsEventListener`, which subscribes to the payment event system. When a payment event occurs, metrics are emitted in parallel to all registered sinks.

```
┌─────────────────┐
│ StripePayment   │
│    Service      │
└────────┬────────┘
         │ Publishes Events
         ▼
┌─────────────────────┐
│ PaymentEvent        │
│   Publisher         │
└────────┬────────────┘
         │ Notifies Listeners
         ▼
┌──────────────────────┐
│ MetricsEvent         │
│   Listener           │ (Implements IPaymentEventListener)
└────────┬─────────────┘
         │ Emits Metrics
         ▼
┌──────────────────────┐
│ PaymentMetrics       │
│   Service            │ (Implements IPaymentMetrics)
└────────┬─────────────┘
         │ Routes to all sinks
         ▼
┌─────────────────────────────────────────┐
│ IMetricsSink Implementations            │
│  ├─ LoggingMetricsSink (default)        │
│  ├─ DataDogMetricsSink (custom)         │
│  └─ PrometheusMetricsSink (custom)      │
└─────────────────────────────────────────┘
```

### Key Components

1. **`IPaymentMetrics`**: Core metrics API interface
   - `IncrementCounter(name, value, tags)`
   - `RecordGauge(name, value, tags)`
   - `RecordHistogram(name, value, tags)`
   - `RecordAmount(name, amount, currency, tags)`

2. **`PaymentMetricsService`**: Default implementation routing metrics to all sinks

3. **`IMetricsSink`**: Pluggable sink interface for custom backends
   - `EmitCounterAsync(name, value, tags)`
   - `EmitGaugeAsync(name, value, tags)`
   - `EmitHistogramAsync(name, value, tags)`

4. **`MetricsEventListener`**: Automatic metrics collection from payment events

5. **`LoggingMetricsSink`**: Default sink writing to `ILogger`

6. **`MetricsOptions`**: Configuration for metrics behavior

## 📈 Platform Examples

### DataDog

**Install NuGet Package:**
```bash
dotnet add package DogStatsD-CSharp-Client
```

**Implementation:**
```csharp
public class DataDogMetricsSink : IMetricsSink
{
    private readonly DogStatsdService _statsd;

    public DataDogMetricsSink()
    {
        var config = new StatsdConfig
        {
            StatsdServerName = "127.0.0.1",
            StatsdPort = 8125,
            Prefix = "filthy_rodriguez"
        };
        _statsd = new DogStatsdService();
        _statsd.Configure(config);
    }

    public Task EmitCounterAsync(string metricName, long value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        var tagArray = tags?.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
        _statsd.Counter(metricName, value, tags: tagArray);
        return Task.CompletedTask;
    }

    public Task EmitGaugeAsync(string metricName, double value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        var tagArray = tags?.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
        _statsd.Gauge(metricName, value, tags: tagArray);
        return Task.CompletedTask;
    }

    public Task EmitHistogramAsync(string metricName, double value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        var tagArray = tags?.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
        _statsd.Histogram(metricName, value, tags: tagArray);
        return Task.CompletedTask;
    }
}
```

### Prometheus (with prometheus-net)

**Install NuGet Package:**
```bash
dotnet add package prometheus-net
```

**Implementation:**
```csharp
using Prometheus;

public class PrometheusMetricsSink : IMetricsSink
{
    private readonly ILogger<PrometheusMetricsSink> _logger;
    private readonly Dictionary<string, Counter> _counters = new();
    private readonly Dictionary<string, Gauge> _gauges = new();
    private readonly Dictionary<string, Histogram> _histograms = new();

    public PrometheusMetricsSink(ILogger<PrometheusMetricsSink> logger)
    {
        _logger = logger;
    }

    public Task EmitCounterAsync(string metricName, long value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var counter = GetOrCreateCounter(metricName, tags);
            counter.Inc(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit Prometheus counter: {MetricName}", metricName);
        }
        return Task.CompletedTask;
    }

    public Task EmitGaugeAsync(string metricName, double value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var gauge = GetOrCreateGauge(metricName, tags);
            gauge.Set(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit Prometheus gauge: {MetricName}", metricName);
        }
        return Task.CompletedTask;
    }

    public Task EmitHistogramAsync(string metricName, double value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var histogram = GetOrCreateHistogram(metricName, tags);
            histogram.Observe(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit Prometheus histogram: {MetricName}", metricName);
        }
        return Task.CompletedTask;
    }

    private Counter GetOrCreateCounter(string name, IDictionary<string, string>? tags)
    {
        var key = $"{name}_{string.Join("_", tags?.Keys ?? Array.Empty<string>())}";
        if (!_counters.TryGetValue(key, out var counter))
        {
            var labelNames = tags?.Keys.ToArray() ?? Array.Empty<string>();
            counter = Metrics.CreateCounter(
                name.Replace('.', '_').Replace('-', '_'), 
                $"Counter for {name}",
                labelNames);
            _counters[key] = counter;
        }

        if (tags != null && tags.Any())
        {
            return counter.WithLabels(tags.Values.ToArray());
        }

        return counter;
    }

    private Gauge GetOrCreateGauge(string name, IDictionary<string, string>? tags)
    {
        var key = $"{name}_{string.Join("_", tags?.Keys ?? Array.Empty<string>())}";
        if (!_gauges.TryGetValue(key, out var gauge))
        {
            var labelNames = tags?.Keys.ToArray() ?? Array.Empty<string>();
            gauge = Metrics.CreateGauge(
                name.Replace('.', '_').Replace('-', '_'), 
                $"Gauge for {name}",
                labelNames);
            _gauges[key] = gauge;
        }

        if (tags != null && tags.Any())
        {
            return gauge.WithLabels(tags.Values.ToArray());
        }

        return gauge;
    }

    private Histogram GetOrCreateHistogram(string name, IDictionary<string, string>? tags)
    {
        var key = $"{name}_{string.Join("_", tags?.Keys ?? Array.Empty<string>())}";
        if (!_histograms.TryGetValue(key, out var histogram))
        {
            var labelNames = tags?.Keys.ToArray() ?? Array.Empty<string>();
            histogram = Metrics.CreateHistogram(
                name.Replace('.', '_').Replace('-', '_'), 
                $"Histogram for {name}",
                labelNames);
            _histograms[key] = histogram;
        }

        if (tags != null && tags.Any())
        {
            return histogram.WithLabels(tags.Values.ToArray());
        }

        return histogram;
    }
}
```

**Register in Program.cs:**
```csharp
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithMetricsSink<PrometheusMetricsSink>();

// Expose metrics endpoint
app.MapMetrics();
```

### Azure Application Insights

**Install NuGet Package:**
```bash
dotnet add package Microsoft.ApplicationInsights
```

**Implementation:**
```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class ApplicationInsightsMetricsSink : IMetricsSink
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ApplicationInsightsMetricsSink> _logger;

    public ApplicationInsightsMetricsSink(
        TelemetryClient telemetryClient,
        ILogger<ApplicationInsightsMetricsSink> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public Task EmitCounterAsync(string metricName, long value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metric = new MetricTelemetry(metricName, value);
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    metric.Properties[tag.Key] = tag.Value;
                }
            }
            _telemetryClient.TrackMetric(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit Application Insights metric: {MetricName}", metricName);
        }
        return Task.CompletedTask;
    }

    public Task EmitGaugeAsync(string metricName, double value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metric = new MetricTelemetry(metricName, value);
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    metric.Properties[tag.Key] = tag.Value;
                }
            }
            _telemetryClient.TrackMetric(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit Application Insights gauge: {MetricName}", metricName);
        }
        return Task.CompletedTask;
    }

    public Task EmitHistogramAsync(string metricName, double value, 
        IDictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metric = new MetricTelemetry(metricName, value);
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    metric.Properties[tag.Key] = tag.Value;
                }
            }
            _telemetryClient.TrackMetric(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit Application Insights histogram: {MetricName}", metricName);
        }
        return Task.CompletedTask;
    }
}
```

**Register in Program.cs:**
```csharp
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddFilthyRodriguez(builder.Configuration)
    .WithMetricsSink<ApplicationInsightsMetricsSink>();
```

## 🎯 Use Cases

### 1. **Business Analytics**
Track revenue, refund rates, and payment success rates:
```
stripe.payment.confirmed.amount (sum by day) = Daily revenue
stripe.payment.failed.count / stripe.payment.created.count = Failure rate
stripe.refund.succeeded.amount / stripe.payment.confirmed.amount = Refund rate
```

### 2. **Operational Monitoring**
Alert on payment failures, database errors:
```
Alert: stripe.payment.failed.count > 10 in 5 minutes
Alert: stripe.database.operations{status="failure"} > 0
```

### 3. **Performance Tracking**
Monitor database operation counts:
```
stripe.database.operations{operation="create"} = Insert rate
stripe.database.operations{operation="update"} = Update rate
```

### 4. **Fraud Detection**
Track refund reasons:
```
stripe.refund.initiated.by_reason{reason="fraudulent"} = Fraud refunds
```

## 🔍 Detailed Tags

Enable `IncludeDetailedTags` for additional dimensional data:

```json
{
  "StripePayment": {
    "Metrics": {
      "IncludeDetailedTags": true
    }
  }
}
```

**Additional tags when enabled:**
- `payment_method`: Card brand, wallet type
- `customer_id`: Stripe customer ID
- `payment_intent_id`: Unique payment identifier

⚠️ **Warning**: Detailed tags increase cardinality significantly. Use with caution in high-volume environments.

## 🚨 Error Handling

Metrics collection is **fire-and-forget** by design. If a metrics sink fails, it logs an error but **does not affect payment processing**.

```csharp
// From PaymentMetricsService
foreach (var sink in _sinks)
{
    try
    {
        _ = sink.EmitCounterAsync(metricName, value, tags, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Metrics sink failed: {Sink}", sink.GetType().Name);
        // Payment processing continues unaffected
    }
}
```

## 🧪 Testing

Metrics can be mocked for unit tests:

```csharp
var mockMetrics = new Mock<IPaymentMetrics>();
mockMetrics.Setup(m => m.IncrementCounter(
    It.IsAny<string>(), 
    It.IsAny<long>(), 
    It.IsAny<IDictionary<string, string>>()));

// Inject into service
var service = new StripePaymentService(
    options,
    mockLogger,
    mockStripeClient,
    mockRepository,
    mockEventPublisher,
    mockMetrics.Object);

// Verify metrics were called
mockMetrics.Verify(m => m.IncrementCounter(
    "stripe.payment.created.count", 
    1, 
    It.Is<IDictionary<string, string>>(d => d["currency"] == "usd")), 
    Times.Once);
```

## 📚 Related Documentation

- **[Event System](EVENTS.md)**: Payment events that drive metrics collection
- **[Database Integration](DATABASE.md)**: Database metrics tracking
- **[Configuration](README.md)**: General configuration options

---

**Questions or Issues?** Open an issue on [GitHub](https://github.com/ericmmartin/filthy-rodriguez).
