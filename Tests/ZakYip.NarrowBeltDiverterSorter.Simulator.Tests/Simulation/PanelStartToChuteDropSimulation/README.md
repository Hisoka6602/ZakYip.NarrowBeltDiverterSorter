# Panel Start to Chute Drop Simulation Tests

## 概述 (Overview)

本目录包含端到端仿真测试，验证从控制面板按钮配置、通过IO识别小车、包裹上车绑定，到最终格口落格的完整流程。

This directory contains end-to-end simulation tests that validate the complete workflow from control panel button configuration through IO-driven cart identification and package binding to final chute ejection.

## 测试目标 (Test Objectives)

1. **小车识别正确性** - 验证通过IO脉冲正确识别小车编号
2. **包裹绑定准确性** - 验证包裹正确绑定到小车
3. **落格精确性** - 验证包裹在正确的格口、正确的小车到达时落格
4. **大规模稳定性** - 测试1000个包裹的处理能力

## 核心组件 (Core Components)

### SimulationClock
离散时间仿真时钟，提供tick-based时间推进机制。

### SimulatedParcel
仿真包裹记录，包含包裹ID、目标格口和上料时刻。

### SimulatedIoBoard
仿真IO板，支持：
- 输入通道设置（用于模拟传感器）
- 输出通道写入（用于记录落格DO）
- IO历史记录（用于验证时序正确性）

### SimulatedCartPositionTracker
仿真小车位置跟踪器，实现ICartPositionTracker接口：
- 跟踪当前原点小车索引
- 支持小车经过原点事件
- 计算指定偏移量的小车编号

### SimulatedChuteTransmitterPort
仿真格口发信器端口，实现IChuteTransmitterPort接口：
- 记录所有落格事件
- 包含格口ID、时刻和持续时间

## 测试场景 (Test Scenarios)

### Should_CorrectlyIdentifyCarts_AndDropParcels_For1000Packages

核心端到端测试，验证1000个包裹的完整处理流程。

**配置：**
- 小车总数：100辆
- 格口配置：
  - 格口1: 首车在原点时对应90号车
  - 格口2: 首车在原点时对应85号车
  - 格口3: 首车在原点时对应80号车
- 小车经过原点间隔：500ms

**验证项：**
1. 所有1000个包裹都成功绑定车号
2. 绑定的车号在有效范围内（1-100）
3. 所有包裹都触发了落格事件
4. 每个包裹的落格格口与目标一致
5. 每个包裹的落格车号与绑定车号一致
6. 没有重复的包裹ID
7. 所有配置的格口都被使用

## 仿真流程 (Simulation Flow)

```
1. 初始化
   ├─ 创建仿真时钟
   ├─ 创建仿真IO板
   ├─ 创建小车位置跟踪器
   └─ 创建格口发信器

2. 生成测试数据
   └─ 生成1000个随机分布的包裹

3. 仿真执行循环
   ├─ 推进时钟到包裹上料时刻
   ├─ 模拟小车通过原点（IO脉冲）
   ├─ 包裹绑定到当前窗口小车
   ├─ 包裹随小车运行到目标格口
   └─ 触发落格DO输出

4. 结果验证
   ├─ 验证绑定正确性
   ├─ 验证落格正确性
   └─ 验证时序一致性
```

## 使用方法 (Usage)

运行仿真测试：

```bash
cd /path/to/repository
dotnet test Tests/ZakYip.NarrowBeltDiverterSorter.Simulator.Tests/ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.csproj --filter "FullyQualifiedName~PanelStartToChuteDropSimulation"
```

## 扩展点 (Extension Points)

如需添加新的仿真测试场景，可以：

1. 创建新的测试方法在`PanelStartToChuteDropSimulationTests`类中
2. 调整仿真参数（小车数量、格口配置等）
3. 添加新的验证断言
4. 扩展仿真组件以支持更复杂的场景

## 限制与简化 (Limitations and Simplifications)

当前实现的简化假设：

1. **时间模型**：使用离散tick而非实时时间
2. **物理运动**：简化的小车运动模型
3. **IO模型**：不包含实际的硬件抖动和延迟
4. **控制面板**：当前版本未包含完整的控制面板按钮配置流程

未来可以增强的方向：

1. 添加控制面板API配置测试
2. 实现更真实的小车运动物理模型
3. 添加异常场景测试（传感器故障、IO失效等）
4. 集成实际的状态机转换测试
