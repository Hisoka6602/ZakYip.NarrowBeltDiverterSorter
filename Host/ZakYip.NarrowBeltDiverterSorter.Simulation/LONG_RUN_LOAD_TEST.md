# 长时间高负载分拣稳定性仿真场景

## 概述

本仿真场景用于验证窄带分拣系统在长时间高负载下的稳定性与正确性，支持生成1000个包裹进行持续分拣测试，并输出详细的生命周期报告。

## 功能特性

- ✅ **配置化参数**：所有参数可配置，无硬编码魔法数字
- ✅ **长跑测试**：支持1000个包裹持续分拣测试
- ✅ **随机格口分配**：模拟真实的上游分配场景
- ✅ **包裹长度随机化**：支持200mm-1000mm随机长度
- ✅ **异常口处理**：无法分拣的包裹自动路由到异常口60
- ✅ **生命周期追踪**：记录每个包裹的关键事件
- ✅ **内存优化**：采样记录，避免内存泄漏
- ✅ **Markdown报告**：生成中文可读报告

## 使用方法

### 运行长跑场景

```bash
cd ZakYip.NarrowBeltDiverterSorter.Simulation
dotnet run --scenario long-run-load-test
```

### 指定输出路径

```bash
dotnet run --scenario long-run-load-test --output my-report.md
```

或者同时生成JSON报告：

```bash
dotnet run --scenario long-run-load-test --output my-report.json
```

### 重置配置

```bash
dotnet run --scenario long-run-load-test --reset-config
```

## 配置参数

默认配置在 `LongRunLoadTestOptions.CreateDefault()` 中定义：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| TargetParcelCount | 1000 | 目标包裹总数 |
| ParcelCreationIntervalMs | 300 | 包裹创建间隔（毫秒） |
| ChuteCount | 60 | 格口数量 |
| ChuteWidthMm | 1000 | 格口宽度（毫米） |
| MainLineSpeedMmps | 1000 | 主线速度（毫米/秒） |
| CartWidthMm | 200 | 小车宽度（毫米） |
| CartSpacingMm | 500 | 小车节距（毫米） |
| CartCount | 60 | 小车数量 |
| ExceptionChuteId | 60 | 异常口编号 |
| MinParcelLengthMm | 200 | 包裹最小长度（毫米） |
| MaxParcelLengthMm | 1000 | 包裹最大长度（毫米） |
| InfeedToDropDistanceMm | 2000 | 入口到落车点距离（毫米） |
| InfeedConveyorSpeedMmps | 1000 | 入口输送线速度（毫米/秒） |

## 输出报告

### 控制台输出

运行完成后会在控制台输出仿真统计信息：

```
════════════════════════════════════════
║   长时间高负载分拣稳定性仿真报告     ║
════════════════════════════════════════

【包裹统计】
  目标包裹数:        1000 个
  实际完成:          1000 个
  正常落格:           980 个
  异常落格(异常口):    20 个
  错分:                 0 个
  未完成:               0 个
  仿真总耗时:        XXX 秒
  最大并发在途包裹数:   X 个（估算）

【验收结果】
  ✓ 配置正确且无魔法数字:  通过
  ✓ 全部包裹已生成:        通过
  ✓ 全部包裹已完成:        通过
  ✓ 无错分:                通过
```

### Markdown报告

报告包含以下内容：

1. **仿真配置**：所有配置参数
2. **仿真统计**：总包裹数、正常落格、异常落格、错分、未完成
3. **包裹生命周期**：采样显示部分包裹的详细时间线（前10个、后10个、随机10个）

示例：

```markdown
# 长时间高负载分拣稳定性仿真报告

## 仿真配置
- 目标包裹数: 1000
- 包裹创建间隔: 300ms
- 格口数量: 60
- 异常口: 60
- 主线速度: 1000 mm/s
...

## 仿真统计
- 总包裹数: 1000
- 正常落格: 980
- 异常落格(异常口): 20
- 错分: 0
- 未完成: 0

## 包裹生命周期

### 包裹 #000001
- [00:00.000] Created - 入口传感器触发
- [00:00.150] LoadedOnCart - 上车到小车 5
- [00:03.200] **最终状态**: 正常落格到格口 15

...
```

## 验收标准

1. ✅ **配置正确且无魔法数字**：所有参数来自配置
2. ✅ **功能正确**：
   - 实际完成 = 1000
   - 错分数量 = 0
   - 无法分拣的包裹全部落在异常口60
   - 无包裹处于悬空状态
3. ✅ **生命周期记录完整**：包含创建、路由、上车、落格等关键节点
4. ✅ **长时间运行无异常**：无未处理异常，内存无明显泄漏

## 实现细节

### 核心组件

1. **LongRunLoadTestOptions**：配置选项类，定义所有仿真参数
2. **ParcelTimelineRecorder**：包裹生命周期记录器，轻量级事件追踪
3. **LongRunHighLoadSortingScenario**：长跑场景实现，协调各组件
4. **LongRunRandomUpstreamClient**：随机上游客户端，模拟随机格口分配

### 内存优化

- 使用 `ConcurrentDictionary` 存储事件
- 采样显示，仅显示30个包裹的详细时间线
- 事件记录轻量化，仅保存关键信息

### 复用现有基础设施

- 复用 E2E 仿真基础设施
- 复用 TrackTopologyOptions 和 MainLineControlOptions
- 复用 ParcelLifecycleService 和 CartLifecycleService
- 复用现有领域事件体系

## 故障排查

### 问题：包裹未完成

可能原因：
- 超时时间不足
- 系统未就绪

解决方案：
- 检查日志中的小车环就绪时间
- 增加超时时间（在代码中修改 maxWaitSeconds）

### 问题：错分数量不为0

可能原因：
- 分拣逻辑错误
- 格口映射错误

解决方案：
- 检查 Markdown 报告中的包裹生命周期
- 分析错分包裹的目标格口和实际格口

## 扩展

如需修改配置参数，可以：

1. 修改 `LongRunLoadTestOptions.CreateDefault()` 方法
2. 或在 `Program.cs` 的 `RunLongRunLoadTestScenarioAsync` 方法中覆盖特定参数

例如，修改为10个包裹的快速测试：

```csharp
var options = LongRunLoadTestOptions.CreateDefault() with
{
    TargetParcelCount = 10,
    ParcelCreationIntervalMs = 500
};
```

## 相关文件

- `Options/LongRunLoadTestOptions.cs` - 配置选项
- `ParcelTimelineRecorder.cs` - 生命周期记录器
- `Scenarios/LongRunHighLoadSortingScenario.cs` - 场景实现
- `Fakes/LongRunRandomUpstreamClient.cs` - 随机上游客户端
- `Program.cs` - 命令行集成
