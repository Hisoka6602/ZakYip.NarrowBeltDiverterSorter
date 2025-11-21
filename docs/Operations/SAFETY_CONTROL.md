# 格口安全控制 (Chute Safety Control)

## 概述

格口安全控制系统确保在系统启动和停止时，所有格口发信器都被强制关闭，避免误触发导致小车继续转动的安全问题。

## 设计架构

### 核心接口

**IChuteSafetyService** - 位于 `Core/Abstractions`
```csharp
public interface IChuteSafetyService
{
    /// <summary>
    /// 关闭全部格口发信器（发射 IO），确保不会有小车继续被触发。
    /// </summary>
    Task CloseAllChutesAsync(CancellationToken cancellationToken = default);
}
```

### 实现

#### 1. ChuteSafetyService (生产环境)
- 位置: `Execution/Sorting/ChuteSafetyService.cs`
- 用途: 面向真实硬件的实现
- 功能: 
  - 从 `IChuteConfigProvider` 获取所有格口配置
  - 通过 `IChuteTransmitterPort` 依次关闭每个格口
  - 并行执行关闭操作以提高效率
  - 即使部分格口关闭失败，也继续尝试关闭其他格口

#### 2. SimulatedChuteSafetyService (仿真环境)
- 位置: `Simulation/Fakes/SimulatedChuteSafetyService.cs`
- 用途: 仿真测试
- 功能: 与生产环境实现相同，但使用 `FakeChuteTransmitterPort`

### 集成点

#### Host 层集成

**SafetyControlWorker** - 位于 `Host/SafetyControlWorker.cs`

这是一个 `IHostedService` 实现，负责：

1. **启动时 (StartAsync)**
   - 在主驱运行前调用 `CloseAllChutesAsync()`
   - 记录日志: "安全控制: 启动前已关闭全部格口发信器"

2. **停止时 (ApplicationStopping 回调)**
   - 在应用停止时调用 `CloseAllChutesAsync()`
   - 使用 5 秒超时避免阻塞关机
   - 记录日志: "安全控制: 停止前已关闭全部格口发信器"

#### Program.cs 注册

```csharp
// 注册格口安全控制服务
builder.Services.AddSingleton<IChuteSafetyService, ChuteSafetyService>();

// 注册安全控制工作器
builder.Services.AddHostedService<SafetyControlWorker>();
```

## 安全场景仿真

### SafetyScenarioRunner

- 位置: `Simulation/SafetyScenarioRunner.cs`
- 功能: 验证启动和停止时的安全行为

### 运行安全场景

```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- --scenario safety-chute-reset
```

### 验证步骤

1. **启动前检查**: 记录启动前打开的格口数量
2. **启动安全关闭**: 执行 `CloseAllChutesAsync()` 并验证所有格口关闭
3. **运行模拟**: 触发部分格口开合，验证正常运行
4. **停止安全关闭**: 执行 `CloseAllChutesAsync()` 并验证所有格口关闭
5. **最终验证**: 确认所有格口都处于关闭状态

### 报告示例

```
════════════════════════════════════════
║      安全场景验证报告 (Chute Safety) ║
════════════════════════════════════════

【格口状态】
  总格口数:          10
  启动前已清零:      ✓ 是
  运行中曾触发开合:  ✓ 是 (3 个格口)
  停止后全部关闭:    ✓ 是

【异常情况】
  启动时仍被检测为打开的格口: 0
  启动安全关闭后仍打开的格口: 0
  停止后仍被检测为打开的格口: 0

安全检查结果:       ✓ 通过

════════════════════════════════════════
```

## 技术特点

### 容错设计

1. **异常处理**: 即使关闭某个格口失败，也会继续尝试关闭其他格口
2. **日志记录**: 详细记录每个步骤的执行情况
3. **非阻塞**: 关闭操作失败不会阻止应用启动或停止

### 并行执行

使用 `Task.WhenAll()` 并行关闭所有格口，提高执行效率：

```csharp
var closeTasks = new List<Task>();
foreach (var chute in allChutes)
{
    closeTasks.Add(CloseChuteSafelyAsync(chute.ChuteId, cancellationToken));
}
await Task.WhenAll(closeTasks);
```

### 超时保护

停止时使用 5 秒超时，确保不会无限期阻塞应用关闭：

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
_chuteSafetyService.CloseAllChutesAsync(cts.Token).GetAwaiter().GetResult();
```

## 测试覆盖

### 单元测试
所有现有测试继续通过（163 个测试），确保新功能不影响现有行为。

### 集成测试
通过 `safety-chute-reset` 场景验证端到端安全行为。

### E2E 测试
不影响现有的三种分拣模式 E2E 测试：
- Normal 模式
- FixedChute 模式
- RoundRobin 模式
- 不稳定速度场景

## 日志示例

### 启动时日志
```
info: SafetyControlWorker[0]
      安全控制: 启动前关闭全部格口发信器
info: ChuteSafetyService[0]
      安全控制: 正在关闭全部 10 个格口发信器...
info: ChuteSafetyService[0]
      安全控制: 已关闭全部格口发信器
```

### 停止时日志
```
info: SafetyControlWorker[0]
      安全控制: 停止前关闭全部格口发信器
info: ChuteSafetyService[0]
      安全控制: 正在关闭全部 10 个格口发信器...
info: ChuteSafetyService[0]
      安全控制: 已关闭全部格口发信器
```

## 维护说明

### 添加新格口
格口安全控制会自动处理 `IChuteConfigProvider` 中配置的所有格口，无需额外配置。

### 修改超时时间
在 `SafetyControlWorker.cs` 中修改超时值：
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 改为 10 秒
```

### 扩展错误处理
在 `ChuteSafetyService.CloseChuteSafelyAsync()` 方法中添加自定义错误处理逻辑。

## 总结

格口安全控制系统通过在系统启动和停止时强制关闭所有格口发信器，提供了一层关键的安全保护，确保不会因为残留输出导致小车误触发。该系统设计简洁、容错性强、易于维护，并通过专门的仿真场景进行验证。
