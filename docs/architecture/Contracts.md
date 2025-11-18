# API & SignalR Contracts Documentation

## Overview

This document describes all external-facing contracts (DTOs) used by the ZakYip.NarrowBeltDiverterSorter.Host API and SignalR interfaces. These contracts form the official interface for frontend applications and external integrations.

## Architecture Principles

1. **Separation of Concerns**: All external DTOs are defined in the `ZakYip.NarrowBeltDiverterSorter.Host.Contracts` project, separate from internal domain models.
2. **No Domain Leakage**: Controllers and Hubs never expose internal Core domain types directly to external clients.
3. **Stable Contracts**: Changes to internal domain models do not automatically propagate to API contracts, ensuring backward compatibility.
4. **Clear Mapping**: Host project contains mapping logic between Core/Observability types and Contract DTOs.

## API Endpoints

### Line Control API (`api/line`)

Manages sorter line operations including start, stop, pause, and status queries.

#### Endpoints

| Method | Route | Description | Request | Response |
|--------|-------|-------------|---------|----------|
| GET | `/api/line/state` | Get current line state | - | `LineStateResponse` |
| POST | `/api/line/start` | Request line start | - | `LineOperationResponse` |
| POST | `/api/line/stop` | Request line stop | - | `LineOperationResponse` |
| POST | `/api/line/pause` | Request line pause | - | `LineOperationResponse` |
| POST | `/api/line/resume` | Resume from pause | - | `LineOperationResponse` |
| POST | `/api/line/fault/ack` | Acknowledge fault | - | `LineOperationResponse` |

#### DTOs

**LineStateResponse**
```csharp
public record class LineStateResponse
{
    public required string LineRunState { get; init; }  // "Stopped", "Starting", "Running", "Stopping", "Paused"
    public required string SafetyState { get; init; }   // "Normal", "Warning", "Fault", "Emergency"
    public DateTimeOffset Timestamp { get; init; }
}
```

**LineOperationResponse**
```csharp
public record class LineOperationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public required string CurrentLineRunState { get; init; }
    public required string CurrentSafetyState { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

### Configuration API (`api/config`)

Manages persistent configuration for the sorter system using LiteDB storage.

#### Endpoints

| Method | Route | Description | Request | Response |
|--------|-------|-------------|---------|----------|
| GET | `/api/config/mainline` | Get main line control options | - | `MainLineControlOptionsDto` |
| PUT | `/api/config/mainline` | Update main line control options | `MainLineControlOptionsDto` | Success message |
| GET | `/api/config/infeed-layout` | Get infeed layout options | - | `InfeedLayoutOptionsDto` |
| PUT | `/api/config/infeed-layout` | Update infeed layout options | `InfeedLayoutOptionsDto` | Success message |
| GET | `/api/config/upstream-connection` | Get upstream connection options | - | `UpstreamConnectionOptionsDto` |
| PUT | `/api/config/upstream-connection` | Update upstream connection | `UpstreamConnectionOptionsDto` | Success message |
| GET | `/api/config/long-run-load-test` | Get load test options | - | `LongRunLoadTestOptionsDto` |
| PUT | `/api/config/long-run-load-test` | Update load test options | `LongRunLoadTestOptionsDto` | Success message |
| GET | `/api/config/simulation` | Get simulation configuration | - | `SimulationConfigurationDto` |
| PUT | `/api/config/simulation` | Update simulation configuration | `SimulationConfigurationDto` | Success message |
| GET | `/api/config/safety` | Get safety configuration | - | `SafetyConfigurationDto` |
| PUT | `/api/config/safety` | Update safety configuration | `SafetyConfigurationDto` | Success message |
| GET | `/api/config/recording` | Get recording configuration | - | `RecordingConfigurationDto` |
| PUT | `/api/config/recording` | Update recording configuration | `RecordingConfigurationDto` | Success message |
| GET | `/api/config/signalr-push` | Get SignalR push configuration | - | `SignalRPushConfigurationDto` |
| PUT | `/api/config/signalr-push` | Update SignalR push configuration | `SignalRPushConfigurationDto` | Success message |

#### DTOs

**MainLineControlOptionsDto**
- `TargetSpeedMmps`: Target speed in millimeters per second
- `LoopPeriodMs`: Control loop period in milliseconds
- `ProportionalGain`, `IntegralGain`, `DerivativeGain`: PID controller parameters
- `StableDeadbandMmps`: Stability deadband in mm/s
- `StableHoldSeconds`: Stability hold duration in seconds
- `MinOutputMmps`, `MaxOutputMmps`: Output limits in mm/s
- `IntegralLimit`: Integral term limit

**InfeedLayoutOptionsDto**
- `InfeedToMainLineDistanceMm`: Distance from infeed sensor to main line drop point (mm)
- `TimeToleranceMs`: Time tolerance for cart prediction (ms)
- `CartOffsetCalibration`: Calibration offset in cart count

**UpstreamConnectionOptionsDto**
- `BaseUrl`: Upstream API base URL
- `RequestTimeoutSeconds`: HTTP request timeout (seconds)
- `AuthToken`: Optional authentication token

**LongRunLoadTestOptionsDto**
- `TargetParcelCount`: Total parcels to generate
- `ParcelCreationIntervalMs`: Interval between parcels (ms)
- `ChuteCount`: Number of chutes
- `ChuteWidthMm`: Chute width (mm)
- `MainLineSpeedMmps`: Main line speed (mm/s)
- `CartWidthMm`, `CartSpacingMm`, `CartCount`: Cart layout parameters
- `ExceptionChuteId`: Exception chute identifier
- `MinParcelLengthMm`, `MaxParcelLengthMm`: Parcel size range
- `ForceToExceptionChuteOnConflict`: Force to exception on conflict
- `InfeedToDropDistanceMm`: Infeed to drop distance (mm)
- `InfeedConveyorSpeedMmps`: Infeed conveyor speed (mm/s)

**SimulationConfigurationDto**
- `TimeBetweenParcelsMs`: Time between parcel creation (ms)
- `TotalParcels`: Total parcels to simulate
- `MinParcelLengthMm`, `MaxParcelLengthMm`: Parcel length range
- `RandomSeed`: Optional random seed for reproducibility
- `ParcelTtlSeconds`: Parcel time-to-live (seconds)

**SafetyConfigurationDto**
- `EmergencyStopTimeoutSeconds`: Emergency stop timeout
- `AllowAutoRecovery`: Enable automatic fault recovery
- `AutoRecoveryIntervalSeconds`: Recovery attempt interval
- `MaxAutoRecoveryAttempts`: Maximum recovery attempts
- `SafetyInputCheckPeriodMs`: Safety input check period (ms)
- `EnableChuteSafetyInterlock`: Enable chute safety interlock
- `ChuteSafetyInterlockTimeoutMs`: Interlock timeout (ms)

**RecordingConfigurationDto**
- `EnabledByDefault`: Auto-start recording on system start
- `MaxSessionDurationSeconds`: Maximum session duration
- `MaxEventsPerSession`: Maximum events per session
- `RecordingsDirectory`: Recording storage directory
- `AutoCleanupOldRecordings`: Auto-cleanup old recordings
- `RecordingRetentionDays`: Retention period (days)

**SignalRPushConfigurationDto**
- `LineSpeedPushIntervalMs`: Line speed push interval (ms)
- `ChuteCartPushIntervalMs`: Chute-cart mapping push interval (ms)
- `OriginCartPushIntervalMs`: Origin cart push interval (ms)
- `ParcelCreatedPushIntervalMs`: Parcel creation notification interval (ms)
- `ParcelDivertedPushIntervalMs`: Parcel diverted notification interval (ms)
- `DeviceStatusPushIntervalMs`: Device status push interval (ms)
- `CartLayoutPushIntervalMs`: Cart layout push interval (ms)
- `OnlineParcelsPushPeriodMs`: Online parcels push period (ms)
- `EnableOnlineParcelsPush`: Enable online parcels push

### Simulation API (`api/simulations`)

Controls simulation scenarios for testing and development.

#### Endpoints

| Method | Route | Description | Request | Response |
|--------|-------|-------------|---------|----------|
| POST | `/api/simulations/long-run/start-from-panel` | Start long-run simulation via panel button | - | `LongRunSimulationStartResponse` |

#### DTOs

**LongRunSimulationStartResponse**
- `RunId`: Unique simulation run identifier
- `Status`: Simulation status ("triggered", "running", "completed", "failed")
- `Message`: Human-readable status message
- `Configuration`: Simulation configuration summary

### Recording API (`api/recordings`)

Manages event recording and replay for diagnostics and analysis.

#### Endpoints

| Method | Route | Description | Request | Response |
|--------|-------|-------------|---------|----------|
| POST | `/api/recordings/start` | Start new recording session | `StartRecordingRequest` | `RecordingSessionResponse` |
| POST | `/api/recordings/{sessionId}/stop` | Stop recording session | - | `RecordingSessionResponse` |
| GET | `/api/recordings` | List all recordings | - | `List<RecordingSessionResponse>` |
| GET | `/api/recordings/{sessionId}` | Get recording details | - | `RecordingSessionResponse` |
| POST | `/api/recordings/{sessionId}/replay` | Replay recording | `ReplayRequest` | `ReplayResponse` |

#### DTOs

**StartRecordingRequest**
- `Name`: Session name (required)
- `Description`: Optional session description

**RecordingSessionResponse**
- `SessionId`: Unique session identifier (Guid)
- `Name`: Session name
- `StartedAt`: Session start timestamp
- `StoppedAt`: Session stop timestamp (null if still running)
- `Description`: Session description
- `IsCompleted`: Whether session completed normally
- `EventCount`: Total events recorded

**ReplayRequest**
- `Mode`: Replay mode (`OriginalSpeed`, `Accelerated`, `FixedInterval`)
- `SpeedFactor`: Acceleration factor (for `Accelerated` mode)
- `FixedIntervalMs`: Fixed interval (for `FixedInterval` mode)

**ReplayResponse**
- `SessionId`: Session identifier
- `Status`: Replay status
- `Message`: Optional message

### Parcels API (`api/parcels`)

Queries parcel lifecycle and statistics.

#### Endpoints

| Method | Route | Description | Request | Response |
|--------|-------|-------------|---------|----------|
| GET | `/api/parcels/{parcelId}/lifecycle` | Get parcel lifecycle | - | `ParcelLifecycleDto` |
| GET | `/api/parcels/online` | Get online parcels | - | `List<ParcelLifecycleDto>` |
| GET | `/api/parcels/recent-completed` | Get recent completed parcels | `count` (query param) | `List<ParcelLifecycleDto>` |
| GET | `/api/parcels/stats` | Get parcel statistics | - | `ParcelLifecycleStatsDto` |

#### DTOs

**ParcelLifecycleDto**
- `ParcelId`: Unique parcel identifier
- `Status`: Parcel status ("Created", "Loaded", "Sorted", "Completed", "Discarded")
- `FailureReason`: Failure reason if any ("None", "NoRoute", "CartConflict", "Timeout", etc.)
- `RouteState`: Routing state ("Pending", "Planned", "Executed")
- `TargetChuteId`: Target chute ID
- `ActualChuteId`: Actual chute ID where parcel was diverted
- `BoundCartId`: Cart ID to which parcel is bound
- `PredictedCartId`: Predicted cart ID
- `CreatedAt`: Parcel creation timestamp
- `LoadedAt`: Load timestamp
- `DivertPlannedAt`: Divert planning timestamp
- `DivertedAt`: Divert execution timestamp
- `SortedAt`: Sorting completion timestamp
- `CompletedAt`: Lifecycle completion timestamp
- `SortingOutcome`: Sorting outcome ("Success", "Failed", "Redirected")
- `DiscardReason`: Discard reason if discarded

**ParcelLifecycleStatsDto**
- `StatusDistribution`: Dictionary mapping status to count
- `FailureReasonDistribution`: Dictionary mapping failure reason to count
- `OnlineCount`: Current online parcel count
- `TotalTracked`: Total parcels tracked

## SignalR Hub

### NarrowBeltLiveHub (`/liveview`)

Real-time monitoring hub for sorter state and events.

#### Client Methods (Callable by Frontend)

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinChuteGroup` | `long chuteId` | Subscribe to chute-specific events |
| `LeaveChuteGroup` | `long chuteId` | Unsubscribe from chute events |
| `GetCurrentSnapshot` | - | Request full current state |

#### Server Messages (Pushed to Frontend)

| Message | Payload Type | Description |
|---------|--------------|-------------|
| `LineSpeedUpdated` | `LineSpeedDto` | Main line speed changed |
| `DeviceStatusUpdated` | `DeviceStatusDto` | Device status changed |
| `OriginCartChanged` | `OriginCartDto` | Origin cart position changed |
| `ChuteCartsUpdated` | `List<ChuteCartDto>` | Chute-cart mappings updated |
| `ChuteCartChanged` | `ChuteCartDto` | Single chute-cart mapping changed |
| `CartLayoutUpdated` | `CartLayoutDto` | Cart positions updated |
| `LineRunStateUpdated` | `LineRunStateDto` | Line run state changed |
| `SafetyStateUpdated` | `SafetyStateDto` | Safety state changed |
| `OnlineParcelsUpdated` | `List<ParcelDto>` | Online parcels list updated |
| `LastCreatedParcelUpdated` | `ParcelDto` | Last created parcel |
| `LastDivertedParcelUpdated` | `ParcelDto` | Last diverted parcel |

#### SignalR DTOs

**LineSpeedDto**
- `ActualMmps`: Actual line speed (mm/s)
- `TargetMmps`: Target line speed (mm/s)
- `Status`: Speed status ("Stopped", "Accelerating", "Stable", "Decelerating")
- `LastUpdatedAt`: Last update timestamp

**ParcelDto** (SignalR variant)
- `ParcelId`: Parcel identifier
- `Barcode`: Parcel barcode
- `WeightKg`: Parcel weight (kg)
- `VolumeCubicMm`: Parcel volume (cubic mm)
- `TargetChuteId`: Target chute
- `ActualChuteId`: Actual chute
- `CreatedAt`: Creation timestamp
- `DivertedAt`: Divert timestamp

**DeviceStatusDto**
- `Status`: Device status ("Online", "Offline", "Degraded", "Fault")
- `Message`: Optional status message
- `LastUpdatedAt`: Last update timestamp

**CartPositionDto**
- `CartId`: Cart identifier
- `CartIndex`: Cart index in ring
- `LinearPositionMm`: Linear position (mm)
- `CurrentChuteId`: Current chute ID at cart position

**CartLayoutDto**
- `CartPositions`: List of `CartPositionDto`
- `LastUpdatedAt`: Last update timestamp

**ChuteCartDto**
- `ChuteId`: Chute identifier
- `CartId`: Cart identifier at chute (null if no cart)

**OriginCartDto**
- `CartId`: Cart at origin position (null if none)
- `LastUpdatedAt`: Last update timestamp

**LineRunStateDto**
- `State`: Run state ("Stopped", "Starting", "Running", "Stopping", "Paused")
- `Message`: Optional state message
- `LastUpdatedAt`: Last update timestamp

**SafetyStateDto**
- `State`: Safety state ("Normal", "Warning", "Fault", "Emergency")
- `Source`: Event source (e.g., "EmergencyStop", "MotorFault")
- `Message`: Optional safety message
- `LastUpdatedAt`: Last update timestamp

## Usage Examples

### Starting the Line

```http
POST /api/line/start
Accept: application/json

Response:
{
  "success": true,
  "message": "启动命令已接受",
  "currentLineRunState": "Starting",
  "currentSafetyState": "Normal",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Updating Configuration

```http
PUT /api/config/mainline
Content-Type: application/json

{
  "targetSpeedMmps": 2000,
  "loopPeriodMs": 100,
  "proportionalGain": 1.0,
  "integralGain": 0.1,
  "derivativeGain": 0.05,
  "stableDeadbandMmps": 10,
  "stableHoldSeconds": 2,
  "minOutputMmps": 0,
  "maxOutputMmps": 2500,
  "integralLimit": 1000
}

Response:
{
  "message": "主线控制选项已更新"
}
```

### Subscribing to SignalR Updates

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/liveview")
  .build();

connection.on("LineSpeedUpdated", (data) => {
  console.log(`Speed: ${data.actualMmps} mm/s`);
});

connection.on("ParcelCreated", (parcel) => {
  console.log(`Parcel ${parcel.parcelId} created`);
});

await connection.start();
await connection.invoke("GetCurrentSnapshot");
```

## Best Practices

1. **Version Compatibility**: Frontend should handle unknown enum values gracefully
2. **Timestamps**: All timestamps are in UTC using `DateTimeOffset`
3. **Error Handling**: Always check HTTP status codes and parse error responses
4. **SignalR Reconnection**: Implement reconnection logic with exponential backoff
5. **State Synchronization**: Call `GetCurrentSnapshot` after reconnecting to SignalR
6. **Validation**: Configuration updates are validated server-side; check error messages

## Future Considerations

- API versioning (v1, v2) may be added in future releases
- Authentication/authorization endpoints to be added
- Webhook support for external system integration
- GraphQL endpoint for flexible querying
