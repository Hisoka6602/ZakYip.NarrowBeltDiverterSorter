# Narrow Belt Diverter Sorter - Sorting System Implementation

## Overview

This document describes the implementation of the sorting system (分拣系统) for the Narrow Belt Diverter Sorter, based on the Q&A requirements.

## Architecture

### Core Layer - Domain Models

#### Sorting Domain (`Domain/Sorting`)

**CartAssignment** - Records the assignment of a parcel to a cart
- `ParcelId`: Package identifier
- `CartId`: Cart identifier
- `ChuteId`: Target chute identifier
- `AssignedAt`: Assignment timestamp

**EjectPlan** - Describes when and how to open a chute transmitter
- `ParcelId`: Package identifier
- `CartId`: Cart identifier carrying the parcel
- `ChuteId`: Chute identifier where ejection occurs
- `OpenAt`: Timestamp to open the transmitter
- `OpenDuration`: How long to keep transmitter open
- `IsForceEject`: Whether this is a force eject operation (强排口)

**ISortingPlanner** - Plans eject operations
```csharp
IReadOnlyList<EjectPlan> PlanEjects(DateTimeOffset now, TimeSpan horizon);
```

**IChuteConfigProvider** - Provides chute configurations
- `GetAllConfigs()`: Returns all chute configurations
- `GetConfig(ChuteId)`: Returns specific chute configuration

#### Carts Domain (`Domain/Carts`)

**ICartLifecycleService** - Manages cart state
- `InitializeCart()`: Create new cart with initial state
- `LoadParcel()`: Mark cart as loaded with a parcel
- `UnloadCart()`: Clear cart and mark as empty (reset)
- `Get()`: Query cart state
- `GetAll()`: Get all cart states

#### Tracking Domain (`Domain/Tracking`)

**ICartPositionTracker** - Tracks cart positions
- `CurrentOriginCartIndex`: Current cart at origin position
- `OnCartPassedOrigin()`: Called when a cart passes the origin sensor
- `CalculateCartIndexAtOffset()`: Calculate which cart is at a given position

### Execution Layer - Planning Logic

**SortingPlanner** - Implements the planning algorithm

The planner follows this logic:

1. **Verify Prerequisites**
   - Check if cart ring is built (CartRingSnapshot exists)
   - Verify main line speed is stable
   - Ensure speed is greater than 0

2. **Calculate Timing**
   - Time per cart = (cart spacing / main line speed)
   - This determines how long to keep transmitter open

3. **For Each Enabled Chute**
   - Calculate which cart is currently at this chute:
     - `cartIndex = originCartIndex + chuteOffset`
   - Get the cart snapshot for that cart

4. **Force Eject Logic** (强排口)
   - If chute is marked as force eject:
     - If cart is loaded, generate eject plan
     - Duration = MaxOpenDuration from config
     - Mark as IsForceEject = true

5. **Normal Eject Logic**
   - If cart is loaded with a parcel:
     - Check if parcel's target chute matches this chute
     - Check parcel is in "Sorting" state
     - Generate eject plan with calculated duration
     - Duration capped by MaxOpenDuration

### Host Layer - Execution Worker

**SortingExecutionWorker** - Background service that executes plans

Execution loop:
1. Call `ISortingPlanner.PlanEjects(now, horizon)`
2. For each plan:
   - Call `IChuteTransmitterPort.OpenWindowAsync(chuteId, duration)`
   - Update cart state via `ICartLifecycleService.UnloadCart()`
   - Update parcel state:
     - Force eject: `ParcelRouteState.ForceEjected`
     - Normal eject: `ParcelRouteState.Sorted`
   - Report result to upstream via `IUpstreamSortingApiClient.ReportSortingResultAsync()`
3. Wait for next execution period (default 100ms)

## How It Works (According to Q&A)

### Q: How does the system know which cart is at each position?

**A:** Through origin tracking and offset calculation:

1. Two sensors at origin detect each cart passing
2. Cart 0 has a longer metal plate that blocks both sensors
3. Other carts block only one sensor
4. System counts carts as they pass origin
5. For any chute: `cartAtChute = originCart + chuteOffset`

**Implementation:**
- `ICartPositionTracker` tracks current origin cart
- `SortingPlanner` uses `CalculateCartIndexAtOffset()` to find cart at each chute
- Wraps around ring using modulo arithmetic

### Q: How does the system know which cart carries which parcel?

**A:** Through timing calculation:

1. Parcel triggers infeed sensor
2. Travels fixed distance on conveyor at constant speed
3. Falls onto main line cart
4. System calculates: `travelTime = distance / speed + tolerance`
5. Knows which cart will be at drop point at that time

**Implementation:**
- Already handled by existing `IParcelLoadPlanner` (not modified in this PR)
- `IParcelLifecycleService` tracks parcel-to-cart binding
- `SortingPlanner` queries this information to know which parcels are on which carts

### Q: How does the system eject parcels at the correct chute?

**A:** Through transmitter timing control:

1. Each chute has a transmitter underneath
2. When cart passes, transmitter can signal cart to rotate
3. Open duration controls how many carts rotate
4. Example: 200ms per cart, 400ms open = 2 carts rotate
5. System calculates which cart is at chute using origin position + offset
6. Opens transmitter when target parcel's cart arrives

**Implementation:**
- `SortingPlanner` calculates when each cart arrives at each chute
- Generates `EjectPlan` with `OpenAt` and `OpenDuration`
- `SortingExecutionWorker` executes by calling `IChuteTransmitterPort.OpenWindowAsync()`

### Q: How does force eject (强排口) work for error correction?

**A:** Through dedicated force eject chute:

1. Force eject chute near end of main line
2. Transmitter is always open (or opens for long duration)
3. Every cart passing through rotates and drops parcel
4. Cart marked as empty in system
5. Can receive new parcels

**Implementation:**
- Chutes configured with `IsForceEject = true`
- `SortingPlanner` generates force eject plans for any loaded cart
- `SortingExecutionWorker` updates cart to empty and parcel to `ForceEjected`
- Reports to upstream with failure reason "ForceEjected"

## Configuration

### SortingPlannerOptions
```csharp
public class SortingPlannerOptions
{
    public decimal CartSpacingMm { get; set; } = 500m;
}
```

### SortingExecutionOptions
```csharp
public class SortingExecutionOptions
{
    public TimeSpan ExecutionPeriod { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan PlanningHorizon { get; set; } = TimeSpan.FromSeconds(5);
}
```

### ChuteConfig
```csharp
public record class ChuteConfig
{
    public required ChuteId ChuteId { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsForceEject { get; init; }
    public int CartOffsetFromOrigin { get; init; }
    public TimeSpan MaxOpenDuration { get; init; }
}
```

## Testing

### Test Coverage

**CartPositionTrackerTests** (6 tests)
- Cart position tracking and increment
- Offset calculation with wrap-around
- Null handling before first cart

**CartLifecycleServiceTests** (5 tests)
- Cart initialization
- Loading and unloading parcels
- State management
- Error handling for non-existent carts

**SortingPlannerTests** (5 tests)
- Empty plans when prerequisites not met
- Normal eject plan generation
- Force eject plan generation
- Disabled chute handling
- Integration scenarios

### Running Tests

```bash
dotnet test
```

**Results:**
- Core.Tests: 68 tests passed
- Execution.Tests: 22 tests passed
- Ingress.Tests: 11 tests passed
- Total: 103 tests passed

## Security

CodeQL analysis performed with **0 vulnerabilities** detected.

## Integration Notes

To use the sorting system, services need to be registered in dependency injection:

```csharp
// Core services
services.AddSingleton<ICartLifecycleService, CartLifecycleService>();
services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
services.AddSingleton<IChuteConfigProvider, ChuteConfigProvider>();

// Execution services
services.AddSingleton<ISortingPlanner, SortingPlanner>();
services.Configure<SortingPlannerOptions>(configuration.GetSection("SortingPlanner"));

// Host services
services.AddHostedService<SortingExecutionWorker>();
services.Configure<SortingExecutionOptions>(configuration.GetSection("SortingExecution"));
```

Configuration in appsettings.json:

```json
{
  "SortingPlanner": {
    "CartSpacingMm": 500
  },
  "SortingExecution": {
    "ExecutionPeriod": "00:00:00.100",
    "PlanningHorizon": "00:00:05"
  }
}
```

## Future Enhancements

Possible improvements for future iterations:

1. **Predictive Planning**: Plan ejects further in advance based on cart velocity
2. **Multi-parcel Eject**: Handle multiple parcels destined for same chute
3. **Dynamic Duration**: Adjust open duration based on actual cart speed variations
4. **Fallback Handling**: Retry logic if eject fails
5. **Metrics**: Add telemetry for eject success rates, timing accuracy
6. **Configuration Validation**: Validate chute offsets against ring length
