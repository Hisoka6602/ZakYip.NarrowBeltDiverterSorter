# 仿真统计服务 (Simulation Statistics Service)

## 概述

仿真统计服务用于跟踪和查询窄带分拣机仿真过程中的关键统计数据，包括包裹处理总数、成功分拣数、超时数、错分数等。

## 核心组件

### 1. SimulationResultDto

位置: `Contracts/ZakYip.NarrowBeltDiverterSorter.Host.Contracts/API/SimulationDto.cs`

仿真结果数据传输对象，包含以下字段：

- **RunId**: 仿真运行唯一标识符
- **TotalParcels**: 总包裹数
- **SortedToTargetChutes**: 成功分拣到目标格口的包裹数
- **SortedToErrorChute**: 分拣到异常口（强排口）的包裹数
- **TimedOutCount**: 超时的包裹数
- **MisSortedCount**: 错分的包裹数（分拣到错误格口）
- **IsCompleted**: 仿真是否已完成
- **StartTime**: 仿真开始时间
- **EndTime**: 仿真结束时间

### 2. ISimulationStatisticsService

位置: `Infrastructure/ZakYip.NarrowBeltDiverterSorter.Infrastructure/Simulation/ISimulationStatisticsService.cs`

仿真统计服务接口，提供以下方法：

- `StartRun(string runId)`: 开始新的仿真运行
- `EndRun(string runId)`: 结束仿真运行
- `RecordParcelCreated(string runId, long parcelId)`: 记录包裹创建
- `RecordParcelSorted(string runId, long parcelId, int targetChuteId, int actualChuteId)`: 记录包裹分拣
- `RecordParcelToErrorChute(string runId, long parcelId)`: 记录包裹进入异常口
- `RecordParcelTimedOut(string runId, long parcelId)`: 记录包裹超时
- `GetStatistics(string runId)`: 获取仿真统计结果
- `GetActiveRunId()`: 获取当前活动的仿真运行ID

### 3. InMemorySimulationStatisticsService

位置: `Infrastructure/ZakYip.NarrowBeltDiverterSorter.Infrastructure/Simulation/InMemorySimulationStatisticsService.cs`

ISimulationStatisticsService 的内存实现，使用 `ConcurrentDictionary` 存储统计数据，适用于单次运行的仿真测试。

线程安全，支持并发记录。

## API 端点

### GET /api/simulations/narrowbelt/result

获取窄带仿真结果统计。

**查询参数:**
- `runId` (可选): 运行标识符。如果未提供，返回当前活动的仿真统计。

**响应示例:**
```json
{
  "runId": "lr-20240115-103000-abc123",
  "totalParcels": 1000,
  "sortedToTargetChutes": 950,
  "sortedToErrorChute": 50,
  "timedOutCount": 40,
  "misSortedCount": 0,
  "isCompleted": true,
  "startTime": "2024-01-15T10:30:00Z",
  "endTime": "2024-01-15T10:35:00Z"
}
```

**状态码:**
- `200 OK`: 成功返回统计数据
- `404 Not Found`: 未找到指定的运行或无活动运行
- `503 Service Unavailable`: 统计服务未启用

## 使用示例

### 1. 基本使用

```csharp
// 创建统计服务
var statisticsService = new InMemorySimulationStatisticsService();

// 开始仿真
var runId = "simulation-001";
statisticsService.StartRun(runId);

// 记录包裹处理
statisticsService.RecordParcelCreated(runId, parcelId: 1);
statisticsService.RecordParcelSorted(runId, parcelId: 1, targetChuteId: 5, actualChuteId: 5);

// 记录超时包裹
statisticsService.RecordParcelCreated(runId, parcelId: 2);
statisticsService.RecordParcelTimedOut(runId, parcelId: 2);
statisticsService.RecordParcelToErrorChute(runId, parcelId: 2);

// 结束仿真
statisticsService.EndRun(runId);

// 获取统计结果
var statistics = statisticsService.GetStatistics(runId);
Console.WriteLine($"Total: {statistics.TotalParcels}, Success: {statistics.SortedToTargetChutes}");
```

### 2. 依赖注入

在 ASP.NET Core 应用中注册服务：

```csharp
// Startup.cs or Program.cs
builder.Services.AddSingleton<ISimulationStatisticsService, InMemorySimulationStatisticsService>();
```

在控制器中使用：

```csharp
public class SimulationsController : ControllerBase
{
    private readonly ISimulationStatisticsService _statisticsService;
    
    public SimulationsController(ISimulationStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }
    
    [HttpGet("result")]
    public IActionResult GetResult([FromQuery] string? runId = null)
    {
        var targetRunId = runId ?? _statisticsService.GetActiveRunId();
        var statistics = _statisticsService.GetStatistics(targetRunId);
        // ... 返回结果
    }
}
```

## 长时间仿真回归测试

位置: `Tests/ZakYip.NarrowBeltDiverterSorter.E2ETests/LongRunningSimulationRegressionTests.cs`

提供了完整的回归测试套件，验证 1000 个包裹处理的各种场景：

1. **全部成功场景**: 1000 个包裹全部成功分拣
2. **超时场景**: 400 个包裹超时并进入异常口
3. **错分场景**: 50 个包裹被错分到错误格口
4. **混合场景**: 850 成功 + 100 超时 + 50 错分

所有测试标记为 `[Trait("Category", "LongRunning")]`，可以在 CI 中单独执行。

### 运行测试

```bash
# 运行所有长时间仿真测试
dotnet test --filter "Category=LongRunning"

# 运行特定测试
dotnet test --filter "FullyQualifiedName~LongRunningSimulationRegressionTests"
```

## 设计参数

仿真配置基于 `docs/NarrowBeltDesign.md` 中的设计参数：

- **小车数量**: 20 辆
- **小车节距**: 500mm
- **格口数量**: 10 个（含 1 个异常口）
- **主线速度**: 1000mm/s
- **入口输送线速度**: 1000mm/s
- **入口到落车点距离**: 2000mm
- **包裹生成间隔**: 300ms (标准回归测试)
- **包裹 TTL**: 30 秒（可配置）

## 验收标准

作为"红线测试"，长时间仿真回归测试必须满足以下标准：

1. **包裹总数验证**: `TotalParcels == 1000`
2. **正常场景**: `MisSortedCount == 0`
3. **超时场景**: 超时包裹数 == 预期不回传的包裹数
4. **异常口验证**: 超时包裹全部进入异常口
5. **数据完整性**: `TotalParcels == SortedToTargetChutes + SortedToErrorChute`
6. **快速执行**: 测试在 1 秒内完成
7. **零警告**: 构建和测试无警告

## 未来改进

1. **持久化支持**: 实现基于数据库的统计服务，支持长期存储和查询
2. **实时更新**: 通过 SignalR 推送实时统计更新
3. **详细报告**: 生成包含时间序列数据的详细仿真报告
4. **性能指标**: 添加吞吐量、延迟等性能统计
5. **可视化**: 提供统计数据的图表和仪表板
