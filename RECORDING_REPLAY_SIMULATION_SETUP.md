# 在 Simulation 项目中启用录制回放 (Enable Recording & Replay in Simulation)

## 概述

由于 Host 和 Simulation 项目之间存在循环依赖，回放功能需要在 Simulation 项目中单独启用。

## 启用步骤

### 方式1：在 Simulation 项目的 Program.cs 中注册

在 `ZakYip.NarrowBeltDiverterSorter.Simulation/Program.cs` 中添加以下代码：

```csharp
using ZakYip.NarrowBeltDiverterSorter.Simulation.Replay;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

// ... 现有的服务注册 ...

// 注册回放运行器
builder.Services.AddSingleton<IRecordingReplayRunner, RecordingReplayRunner>();
```

### 方式2：创建独立的回放应用

创建一个新的控制台应用专门用于回放：

```bash
# 创建新项目
dotnet new console -n ZakYip.NarrowBeltDiverterSorter.ReplayTool

# 添加项目引用
dotnet add reference ../ZakYip.NarrowBeltDiverterSorter.Observability
dotnet add reference ../ZakYip.NarrowBeltDiverterSorter.Simulation
```

在 `ReplayTool/Program.cs` 中：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Replay;

var builder = Host.CreateApplicationBuilder(args);

// 注册事件总线
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// 注册录制管理器
builder.Services.AddSingleton<FileEventRecordingManager>();
builder.Services.AddSingleton<IEventRecordingManager>(
    sp => sp.GetRequiredService<FileEventRecordingManager>());

// 注册回放运行器
builder.Services.AddSingleton<IRecordingReplayRunner, RecordingReplayRunner>();

var host = builder.Build();

// 使用示例
var replayRunner = host.Services.GetRequiredService<IRecordingReplayRunner>();
var sessionId = Guid.Parse("your-session-id-here");

var config = new ReplayConfiguration
{
    Mode = ReplayMode.Accelerated,
    SpeedFactor = 10.0
};

await replayRunner.ReplayAsync(sessionId, config);

Console.WriteLine("回放完成！");
```

## 使用示例

### 示例1：命令行回放工具

```csharp
// ReplayTool/Program.cs
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Replay;

var rootCommand = new RootCommand("事件回放工具");

var sessionIdOption = new Option<string>(
    "--session-id",
    description: "录制会话ID");

var speedFactorOption = new Option<double>(
    "--speed-factor",
    getDefaultValue: () => 1.0,
    description: "加速倍数");

rootCommand.AddOption(sessionIdOption);
rootCommand.AddOption(speedFactorOption);

rootCommand.SetHandler(async (string sessionId, double speedFactor) =>
{
    var builder = Host.CreateApplicationBuilder();
    
    // 配置服务
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
    builder.Services.AddSingleton<FileEventRecordingManager>();
    builder.Services.AddSingleton<IEventRecordingManager>(
        sp => sp.GetRequiredService<FileEventRecordingManager>());
    builder.Services.AddSingleton<IRecordingReplayRunner, RecordingReplayRunner>();
    
    var host = builder.Build();
    
    // 执行回放
    var replayRunner = host.Services.GetRequiredService<IRecordingReplayRunner>();
    var config = new ReplayConfiguration
    {
        Mode = ReplayMode.Accelerated,
        SpeedFactor = speedFactor
    };
    
    Console.WriteLine($"开始回放会话 {sessionId}，加速 {speedFactor}x...");
    
    await replayRunner.ReplayAsync(Guid.Parse(sessionId), config);
    
    Console.WriteLine("回放完成！");
}, sessionIdOption, speedFactorOption);

return await rootCommand.InvokeAsync(args);
```

运行：
```bash
dotnet run --project ReplayTool -- --session-id "3fa85f64-5717-4562-b3fc-2c963f66afa6" --speed-factor 10
```

### 示例2：在测试中使用回放

```csharp
// Tests/ReplayIntegrationTests.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Replay;

public class ReplayIntegrationTests
{
    [Fact]
    public async Task ReplaySession_ShouldPublishAllEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton(NullLogger<FileEventRecordingManager>.Instance);
        services.AddSingleton<FileEventRecordingManager>();
        services.AddSingleton<IEventRecordingManager>(
            sp => sp.GetRequiredService<FileEventRecordingManager>());
        services.AddSingleton(NullLogger<RecordingReplayRunner>.Instance);
        services.AddSingleton<IRecordingReplayRunner, RecordingReplayRunner>();
        
        var provider = services.BuildServiceProvider();
        var replayRunner = provider.GetRequiredService<IRecordingReplayRunner>();
        
        // 监听事件
        var eventBus = provider.GetRequiredService<IEventBus>();
        var eventCount = 0;
        eventBus.Subscribe<LineSpeedChangedEventArgs>(async (e, ct) =>
        {
            eventCount++;
            await Task.CompletedTask;
        });
        
        // Act
        var sessionId = Guid.Parse("test-session-id");
        var config = new ReplayConfiguration
        {
            Mode = ReplayMode.FixedInterval,
            FixedIntervalMs = 1
        };
        
        await replayRunner.ReplayAsync(sessionId, config);
        
        // Assert
        Assert.True(eventCount > 0);
    }
}
```

## 验证回放功能

### 1. 创建测试录制

```bash
# 启动系统并开始录制
curl -X POST http://localhost:5000/api/recordings/start \
  -H "Content-Type: application/json" \
  -d '{"name":"回放测试"}'

# 运行仿真产生一些事件...

# 停止录制
curl -X POST http://localhost:5000/api/recordings/{sessionId}/stop
```

### 2. 检查录制文件

```bash
# 查看会话元数据
cat recordings/{session-id}/session.json

# 查看事件数量
wc -l recordings/{session-id}/events.ndjson

# 查看前几个事件
head -n 5 recordings/{session-id}/events.ndjson | jq
```

### 3. 执行回放

```csharp
// 在你的回放应用中
var replayRunner = serviceProvider.GetRequiredService<IRecordingReplayRunner>();

var config = new ReplayConfiguration
{
    Mode = ReplayMode.Accelerated,
    SpeedFactor = 10.0  // 快10倍
};

await replayRunner.ReplayAsync(sessionId, config, cancellationToken);
```

## 回放模式对比

| 模式 | 描述 | 适用场景 |
|------|------|----------|
| **OriginalSpeed** | 保持原始事件间隔 | 精确复现生产场景 |
| **Accelerated** | 按倍数加速 | 快速验证长时间场景 |
| **FixedInterval** | 固定时间间隔 | 压力测试、稳定性测试 |

## 注意事项

1. **事件总线容量**: 回放时确保事件总线有足够的处理能力
2. **状态初始化**: 回放前确保系统状态已正确初始化
3. **取消令牌**: 使用 CancellationToken 以便中途停止回放
4. **日志级别**: 回放时可以调整日志级别以减少输出

## 故障排查

### 问题1：回放时找不到 IRecordingReplayRunner

**原因**: 服务未注册

**解决**: 确认已在 DI 容器中注册：
```csharp
builder.Services.AddSingleton<IRecordingReplayRunner, RecordingReplayRunner>();
```

### 问题2：事件反序列化失败

**原因**: 事件类型定义变更或 JSON 格式错误

**解决**: 
- 检查 events.ndjson 文件格式
- 使用 `jq` 验证 JSON：`cat events.ndjson | jq .`
- 查看日志中的具体错误信息

### 问题3：回放速度不符合预期

**原因**: SpeedFactor 配置错误或系统处理能力限制

**解决**:
- 验证 ReplayConfiguration 设置
- 降低加速倍数
- 使用 FixedInterval 模式测试

## 扩展开发

### 添加自定义事件处理器

```csharp
public class CustomReplayRunner : RecordingReplayRunner
{
    protected override async Task PublishEventAsync(RecordedEventEnvelope envelope, CancellationToken ct)
    {
        // 在发布前进行自定义处理
        Console.WriteLine($"回放事件: {envelope.EventType} at {envelope.Timestamp}");
        
        // 调用基类方法
        await base.PublishEventAsync(envelope, ct);
    }
}

// 注册自定义实现
builder.Services.AddSingleton<IRecordingReplayRunner, CustomReplayRunner>();
```

### 添加回放进度报告

```csharp
public interface IReplayProgressReporter
{
    void ReportProgress(int current, int total);
}

public class ConsoleProgressReporter : IReplayProgressReporter
{
    public void ReportProgress(int current, int total)
    {
        var percent = (double)current / total * 100;
        Console.WriteLine($"回放进度: {current}/{total} ({percent:F1}%)");
    }
}
```

## 相关文档

- [录制回放功能总览](../RECORDING_REPLAY_README.md)
- [API 文档](../ZakYip.NarrowBeltDiverterSorter.Host/API_DOCUMENTATION.md)
- [系统架构](../README.md)
