# ParcelRoutingWorker Integration Guide

## Overview
`ParcelRoutingWorker` is a background service that handles parcel routing by communicating with the upstream sorting system. It processes parcel creation events and requests chute assignments.

## Event Subscription
The `ParcelRoutingWorker` needs to be connected to the parcel creation event source. This can be done in several ways:

### Option 1: Direct Event Subscription (if using in-process events)
If your system has a component that publishes `ParcelCreatedFromInfeedEventArgs` events, you can subscribe to it:

```csharp
// In Program.cs or during service initialization
var parcelSource = serviceProvider.GetRequiredService<IParcelEventSource>();
var routingWorker = serviceProvider.GetRequiredService<ParcelRoutingWorker>();

parcelSource.ParcelCreated += async (sender, args) =>
{
    await routingWorker.HandleParcelCreatedAsync(args);
};
```

### Option 2: Using Message Bus/Event Bus
If using a message bus (e.g., MediatR, MassTransit), configure it to route `ParcelCreatedFromInfeedEventArgs` to the worker:

```csharp
// In message bus configuration
bus.Subscribe<ParcelCreatedFromInfeedEventArgs>(async (args, ct) =>
{
    var worker = serviceProvider.GetRequiredService<ParcelRoutingWorker>();
    await worker.HandleParcelCreatedAsync(args, ct);
});
```

### Option 3: Queue-based Processing
For high-throughput scenarios, use a queue:

```csharp
// Modify ParcelRoutingWorker.ExecuteAsync to process from a queue
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var eventArgs in _eventQueue.Reader.ReadAllAsync(stoppingToken))
    {
        await HandleParcelCreatedAsync(eventArgs, stoppingToken);
    }
}
```

## Configuration
Configure the upstream sorting API in `appsettings.json`:

```json
{
  "UpstreamSortingApi": {
    "BaseUrl": "http://your-upstream-server:5000",
    "TimeoutSeconds": 30
  }
}
```

## Published Events
The worker publishes `ParcelRoutedEventArgs` events when routing completes. Subscribe to these events to trigger downstream processing:

```csharp
var routingWorker = serviceProvider.GetRequiredService<ParcelRoutingWorker>();
routingWorker.ParcelRouted += (sender, args) =>
{
    if (args.IsSuccess)
    {
        _logger.LogInformation("Parcel {ParcelId} routed to chute {ChuteId}", 
            args.ParcelId.Value, args.ChuteId?.Value);
    }
};
```

## Integration with Existing Components
The worker integrates with:
- **IParcelLifecycleService**: Manages parcel state transitions
- **IUpstreamSortingApiClient**: Communicates with upstream system
- **ParcelCreatedFromInfeedEventArgs**: Input event from infeed system
- **ParcelRoutedEventArgs**: Output event for downstream consumers

## Example: Complete Integration
```csharp
// In Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Register services (already done)
builder.Services.AddSingleton<IParcelLifecycleService, ParcelLifecycleService>();
builder.Services.AddHttpClient<IUpstreamSortingApiClient, UpstreamSortingApiClient>(...);
builder.Services.AddHostedService<ParcelRoutingWorker>();

// Register event source (example)
builder.Services.AddSingleton<IParcelEventSource, InfeedParcelSource>();

var host = builder.Build();

// Wire up event subscription after building the host
var eventSource = host.Services.GetRequiredService<IParcelEventSource>();
var routingWorker = host.Services.GetServices<IHostedService>()
    .OfType<ParcelRoutingWorker>()
    .First();

eventSource.ParcelCreated += async (sender, args) =>
{
    await routingWorker.HandleParcelCreatedAsync(args);
};

await host.RunAsync();
```
