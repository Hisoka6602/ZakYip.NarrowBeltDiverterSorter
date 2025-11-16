# 窄带分拣机仿真系统 (Narrow Belt Sorter Simulation)

## 概述

本仿真系统提供了一个完整的端到端 (E2E) 仿真环境，用于验证窄带分拣机的三种分拣模式，而无需连接真实硬件。

## 三种分拣模式

### 1. Normal 模式 - 正式分拣模式
通过上游 Sorting.RuleEngine 分配格口。这是生产环境的默认模式，模拟真实的规则引擎行为。

**特点：**
- 模拟真实上游系统的格口分配逻辑
- 包裹根据业务规则分配到不同格口
- 自动跳过强排口

### 2. FixedChute 模式 - 指定落格模式
始终将包裹路由到一个固定的格口 ID。适用于测试特定格口或调试场景。

**特点：**
- 所有包裹分配到指定格口
- 可通过命令行参数指定固定格口 ID
- 简化测试和调试流程

### 3. RoundRobin 模式 - 循环格口模式
按格口列表循环分配包裹，实现负载均衡。

**特点：**
- 包裹按顺序循环分配到可用格口
- 自动跳过强排口
- 确保格口负载均衡

## 使用方法

### 基本命令

```bash
# 运行 E2E 仿真（Normal 模式，默认）
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- --scenario e2e-report --parcel-count 20

# 运行 FixedChute 模式
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- \
  --scenario e2e-report \
  --parcel-count 20 \
  --sorting-mode fixed-chute \
  --fixed-chute-id 5

# 运行 RoundRobin 模式
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- \
  --scenario e2e-report \
  --parcel-count 20 \
  --sorting-mode round-robin

# 保存报告到文件
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- \
  --scenario e2e-report \
  --parcel-count 20 \
  --sorting-mode normal \
  --output simulation-report.json

# 重置配置数据库
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- \
  --scenario e2e-report \
  --parcel-count 20 \
  --reset-config
```

### 命令行参数

| 参数 | 说明 | 默认值 | 示例 |
|------|------|--------|------|
| `--scenario` | 仿真场景：`legacy` 或 `e2e-report` | `legacy` | `--scenario e2e-report` |
| `--parcel-count` | 本次仿真包裹数量 | `20` | `--parcel-count 100` |
| `--sorting-mode` | 分拣模式：`normal`, `fixed-chute`, `round-robin` | `normal` | `--sorting-mode fixed-chute` |
| `--fixed-chute-id` | 固定格口 ID（仅 FixedChute 模式） | - | `--fixed-chute-id 5` |
| `--output` | 报告输出路径 | - | `--output report.json` |
| `--reset-config` | 重置配置数据库 | `false` | `--reset-config` |

## 仿真报告

E2E 仿真完成后，会生成包含以下信息的详细报告：

### 控制台输出示例

```
════════════════════════════════════════
║      E2E 仿真结果报告                ║
════════════════════════════════════════

【包裹统计】
  总包裹数:        20 个
  正常落格:        18 个
  强制排出:         2 个
  误分/失败:        0 个
  已分拣数:        20 个
  完成率:       100.0 %

【分拣配置】
  分拣模式:    FixedChute
  固定格口:         5
  可用格口:         9 个
  强排口:      格口 10

【小车环配置】
  小车数量:        60 辆
  小车间距:     500.0 mm
  状态:           已就绪
  预热耗时:     34.66 秒

【主线速度统计】
  目标速度:    1000.0 mm/s
  平均速度:    1014.4 mm/s
  速度标准差:   16.97 mm/s
  最小速度:     980.8 mm/s
  最大速度:    1093.5 mm/s

【性能指标】
  仿真耗时:     68.10 秒
  吞吐量:         0.3 件/秒

【数据验证】
  数据一致性:    ✓ 通过
  速度非零:      ✓ 通过
```

### JSON 报告结构

```json
{
  "statistics": {
    "totalParcels": 20,
    "successfulSorts": 18,
    "forceEjects": 2,
    "missorts": 0,
    "unprocessed": 0,
    "successRate": 0.9,
    "forceEjectRate": 0.1,
    "missortRate": 0.0,
    "unprocessedRate": 0.0,
    "startTime": "2025-11-16T20:00:00Z",
    "endTime": "2025-11-16T20:01:08Z",
    "durationSeconds": 68.1
  },
  "sortingConfig": {
    "sortingMode": "FixedChute",
    "fixedChuteId": 5,
    "availableChutes": 9,
    "forceEjectChuteId": 10
  },
  "cartRing": {
    "length": 60,
    "zeroCartId": 1,
    "zeroIndex": 0,
    "cartSpacingMm": 500.0,
    "isReady": true,
    "warmupDurationSeconds": 34.66
  },
  "mainDrive": {
    "targetSpeedMmps": 1000.0,
    "averageSpeedMmps": 1014.4,
    "speedStdDevMmps": 16.97,
    "minSpeedMmps": 980.8,
    "maxSpeedMmps": 1093.5
  },
  "parcelDetails": [
    {
      "parcelId": "PKG000001",
      "assignedCartId": 15,
      "targetChuteId": 5,
      "actualChuteId": 5,
      "isSuccess": true,
      "isForceEject": false,
      "failureReason": null
    }
  ]
}
```

## 包裹间隔过近的处理

系统内置了处理包裹过近的逻辑：

1. **检测机制**：ParcelLoadPlanner 在分配小车时会检测是否有合适的小车可用
2. **处理策略**：无法分配小车的包裹会因 TTL (Time To Live) 超时
3. **强排处理**：超时包裹自动路由到强排口 (Force Eject Chute)
4. **报告统计**：在仿真报告的"强制排出"统计中体现

**配置参数：**
- `ParcelGenerationIntervalSeconds`: 控制包裹生成间隔（默认 0.8 秒）
- `ParcelTimeToLiveSeconds`: 包裹存活时间（默认 25 秒）

## 仿真配置

仿真系统使用 LiteDB 存储配置，配置文件位于：`simulation.db`

**默认配置：**
- 小车数量: 60 辆
- 小车间距: 500 mm
- 格口数量: 10 个（1-9 为正常格口，10 为强排口）
- 主线速度: 1000 mm/s
- 入口输送线速度: 1000 mm/s

**注意：** 使用 `--reset-config` 参数会清空现有配置并重新生成默认配置。

## 测试

运行单元测试：

```bash
# 运行所有测试
dotnet test

# 仅运行分拣模式测试
dotnet test --filter "FullyQualifiedName~SortingModeTests"
```

## 架构说明

### 核心组件

1. **ParcelGeneratorWorker**: 定时生成包裹并触发入口传感器
2. **ParcelRoutingWorker**: 处理包裹路由，调用上游系统分配格口
3. **ParcelSortingSimulator**: 模拟包裹在小车上的运动和分拣动作
4. **CartMovementSimulator**: 模拟小车环运动
5. **EndToEndSimulationRunner**: E2E 仿真协调器

### Fake 实现

为了实现纯仿真环境，系统提供了所有硬件接口的 Fake 实现：

- `FakeMainLineDrivePort`: 主驱模拟
- `FakeMainLineFeedbackPort`: 主驱反馈模拟
- `FakeInfeedSensorPort`: 入口传感器模拟
- `FakeOriginSensorPort`: 原点传感器模拟
- `FakeFieldBusClient`: 现场总线模拟
- `FakeChuteTransmitterPort`: 格口发信器模拟
- `FakeUpstreamSortingApiClient`: 上游分拣系统 API 模拟

## 故障排查

### 常见问题

**Q: 仿真启动后长时间无响应？**
A: 检查小车环是否成功构建。正常情况下，60 辆小车需要约 30-40 秒完成一圈。

**Q: 包裹全部进入强排口？**
A: 可能是包裹生成间隔过短或 TTL 时间设置过短。尝试增加 `ParcelGenerationIntervalSeconds`。

**Q: 分拣成功率低？**
A: 检查主线速度是否稳定，确认格口配置正确，并确保小车环已就绪。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

[MIT License](../LICENSE)
