# 小车环自检功能 (Cart Ring Self-Check Feature)

## 概述 (Overview)

小车环自检功能用于验证窄带分拣机的小车环拓扑配置是否正确。通过仿真运行，系统可以自动检测小车数量和节距，并与配置值进行对比，确保系统配置的准确性。

The cart ring self-check feature verifies that the narrow belt diverter sorter's cart ring topology configuration is correct. Through simulation, the system automatically detects the cart count and pitch, comparing them against configured values to ensure system configuration accuracy.

## 功能特性 (Features)

### 核心功能 (Core Capabilities)

1. **小车数量检测** - 通过收集小车通过事件，统计唯一的小车ID数量
   - Cart count detection - Collects cart pass events and counts unique cart IDs

2. **节距估算** - 基于时间间隔和速度计算相邻小车之间的节距
   - Pitch estimation - Calculates pitch between adjacent carts using time intervals and speed

3. **容差配置** - 支持配置节距误差容忍百分比
   - Tolerance configuration - Supports configurable pitch error tolerance percentage

4. **中位数算法** - 使用中位数而非平均值，减少异常值影响
   - Median algorithm - Uses median instead of average to reduce outlier impact

## 架构设计 (Architecture)

### Core 层 (Core Layer)

```
ZakYip.NarrowBeltDiverterSorter.Core/
├── SelfCheck/
│   ├── ICartRingSelfCheckService.cs       - 自检服务接口
│   ├── CartRingSelfCheckService.cs        - 自检服务实现
│   ├── CartRingSelfCheckResult.cs         - 自检结果
│   ├── CartRingSelfCheckOptions.cs        - 配置选项
│   └── TrackTopologySnapshot.cs           - 拓扑快照
└── Domain/Tracking/
    └── CartPassEventArgs.cs               - 小车通过事件参数
```

### Simulation 层 (Simulation Layer)

```
ZakYip.NarrowBeltDiverterSorter.Simulation/
└── SelfCheck/
    ├── CartSelfCheckEventCollector.cs     - 事件收集器
    └── CartRingSelfCheckScenarioRunner.cs - 场景运行器
```

## 使用方法 (Usage)

### 1. 配置仿真 (Configure Simulation)

```csharp
var simulationConfig = new SimulationConfiguration
{
    NumberOfCarts = 20,
    CartSpacingMm = 500m,
    NumberOfChutes = 10,
    ForceEjectChuteId = 10,
    MainLineSpeedMmPerSec = 1000.0,
    Scenario = "cart-self-check"  // 启用自检场景
};
```

### 2. 配置自检选项 (Configure Self-Check Options)

```csharp
var selfCheckOptions = new CartRingSelfCheckOptions
{
    MinSamplingDurationSeconds = 30.0,      // 最小采样时长（秒）
    PitchTolerancePercent = 0.05,           // 节距误差容忍百分比（5%）
    AllowedMissDetectionRate = 0.0,         // 允许的漏检率（0表示不允许）
    MinCompleteRings = 2                    // 最少需要的完整环数
};
```

### 3. 运行自检 (Run Self-Check)

```csharp
var service = new CartRingSelfCheckService(selfCheckOptions);
var result = service.RunAnalysis(cartPassEvents, topologySnapshot);

// 检查结果
if (result.IsCartCountMatched && result.IsPitchWithinTolerance)
{
    Console.WriteLine("自检通过！");
}
```

## 输出示例 (Output Example)

### 正确配置 (Correct Configuration)

```
【小车环自检】分析结果：
  配置小车数: 20 辆
  检测小车数: 20 辆
  配置节距: 500.0 mm
  估算节距: 498.7 mm
  数车结果: ✓ 通过
  节距结果: ✓ 在误差范围内 (阈值: 5%)
```

### 错误配置 (Incorrect Configuration)

```
【小车环自检】分析结果：
  配置小车数: 18 辆
  检测小车数: 20 辆
  配置节距: 500.0 mm
  估算节距: 499.2 mm
  数车结果: ✗ 不匹配
  节距结果: ✓ 在误差范围内 (阈值: 5%)
```

## 算法说明 (Algorithm Details)

### 节距计算 (Pitch Calculation)

1. 收集相邻小车通过事件 (Collect consecutive cart pass events)
2. 计算时间差 Δt (Calculate time difference)
3. 获取平均速度 v (Get average speed)
4. 计算节距：pitch = v × Δt
5. 过滤异常值（节距 < 0 或 > 10000mm）
6. 返回所有样本的中位数

### 小车数量统计 (Cart Count Statistics)

使用 HashSet 统计唯一的小车ID数量，支持多圈数据（会自动去重）。

## 测试覆盖 (Test Coverage)

### 单元测试 (Unit Tests)

- `RunAnalysis_Should_Return_Matched_Result_For_Correct_Configuration` - 正确配置测试
- `RunAnalysis_Should_Return_Unmatched_For_Incorrect_Cart_Count` - 小车数量不匹配测试
- `RunAnalysis_Should_Detect_Incorrect_Pitch` - 节距错误检测测试
- `RunAnalysis_Should_Handle_Variable_Speed` - 变速场景测试
- `RunAnalysis_Should_Return_Empty_Result_For_No_Events` - 空事件测试
- `RunAnalysis_Should_Handle_Duplicate_Cart_Ids` - 重复ID测试
- `RunAnalysis_Should_Throw_ArgumentNullException_For_Null_Events` - 空参数测试
- `RunAnalysis_Should_Throw_ArgumentNullException_For_Null_Topology` - 空参数测试

### E2E测试 (E2E Tests)

- `CartSelfCheck_WithCorrectConfiguration_ShouldPass` - 完整场景正确配置测试
- `CartSelfCheck_WithIncorrectCartCount_ShouldFail` - 小车数量错误测试
- `CartSelfCheck_WithIncorrectPitch_ShouldFail` - 节距错误测试

## 扩展性 (Extensibility)

### 未来可扩展功能 (Future Extensions)

1. **实时监控** - 在生产环境中实时运行自检
2. **历史趋势** - 记录自检结果的历史数据，分析趋势
3. **自动校准** - 基于检测结果自动调整配置
4. **多传感器支持** - 支持使用多个传感器进行交叉验证
5. **异常预警** - 检测到配置偏差时发送告警

## 注意事项 (Notes)

1. 自检需要主线速度稳定后才能开始
2. 建议采样至少2个完整环的数据
3. 速度变化较大时，节距测量精度会降低
4. 容差配置应根据实际硬件精度调整

## 相关文档 (Related Documentation)

- [系统架构文档](../SORTING_SYSTEM.md)
- [仿真程序说明](../ZakYip.NarrowBeltDiverterSorter.Simulation/README.md)
