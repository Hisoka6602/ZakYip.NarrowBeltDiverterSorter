# 格口 IO 架构说明

## 概述

本项目中存在两个格口控制相关的接口，它们服务于不同的层次和目的：

## 1. IChuteTransmitterPort（底层硬件端口抽象）

**位置**: `ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IChuteTransmitterPort`

**用途**: 
- 底层硬件端口抽象
- 直接对接现场总线通信
- 被现有代码使用（SortingExecutionWorker、ChuteSafetyService）

**特点**:
- 使用领域对象 `ChuteId` 作为参数
- 包含 `OpenWindowAsync(ChuteId, TimeSpan, ...)` - 需要指定开启时长
- 包含 `ForceCloseAsync(ChuteId, ...)` - 强制关闭
- 与 `ChuteMappingConfiguration` 配合，将 ChuteId 映射到 Modbus 线圈地址

**实现**:
- `ChuteTransmitterDriver` - 真实硬件实现，使用 IFieldBusClient
- `FakeChuteTransmitterPort` - 仿真实现，用于测试

**何时使用**:
- 当需要精确控制开启时长时
- 当直接与现场总线通信时
- 维护现有代码兼容性时

## 2. IChuteIoService（通用格口 IO 服务）

**位置**: `ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IChuteIoService`

**用途**:
- 高层服务抽象
- 支持多 IP 端点架构
- 品牌无关的通用接口

**特点**:
- 使用原始 `long` 类型作为 ChuteId 参数
- 包含 `OpenAsync(long, ...)` - 使用配置的默认时长
- 包含 `CloseAsync(long, ...)` - 关闭格口
- 包含 `CloseAllAsync(...)` - 批量关闭所有格口
- 通过 `ChuteIoOptions` 配置多个节点和通道映射

**实现**:
- `SimulationChuteIoService` - 仿真实现，支持多端点
- `ChuteTransmitterPortAdapter` - 适配器，将 IChuteTransmitterPort 适配为 IChuteIoService

**何时使用**:
- 新开发的功能
- 需要支持多个 IP 端点时
- 需要避免品牌耦合时
- 不需要精确控制开启时长时（使用配置的默认值）

## 架构层次

```
┌─────────────────────────────────────┐
│     应用层 (Application Layer)      │
│   SortingExecutionWorker            │
│   ChuteSafetyService                │
└─────────────────┬───────────────────┘
                  │
                  │ 使用
                  ↓
┌─────────────────────────────────────┐
│      IChuteTransmitterPort          │  ← 现有接口（底层硬件抽象）
│   - OpenWindowAsync(ChuteId, ...)   │
│   - ForceCloseAsync(ChuteId, ...)   │
└─────────────────┬───────────────────┘
                  │
                  │ 实现
                  ↓
┌─────────────────────────────────────┐
│     ChuteTransmitterDriver          │
│   (使用 IFieldBusClient 与硬件通信)  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│    未来应用层 (Future Apps)         │
└─────────────────┬───────────────────┘
                  │
                  │ 使用
                  ↓
┌─────────────────────────────────────┐
│       IChuteIoService               │  ← 新接口（通用服务抽象）
│   - OpenAsync(long, ...)            │
│   - CloseAsync(long, ...)           │
│   - CloseAllAsync(...)              │
└─────────────────┬───────────────────┘
                  │
          ┌───────┴────────┐
          │                │
          ↓                ↓
┌──────────────────┐  ┌──────────────────────┐
│ SimulationChute  │  │ ChuteTransmitter     │
│ IoService        │  │ PortAdapter          │
│ (多端点仿真)      │  │ (适配器)              │
└──────────────────┘  └─────────┬────────────┘
                                │
                                │ 委托
                                ↓
                    ┌────────────────────────┐
                    │ IChuteTransmitterPort  │
                    └────────────────────────┘
```

## 配置示例

### IChuteTransmitterPort 配置（现有）

```json
{
  "ChuteMapping": {
    "ChuteAddressMap": {
      "1": 100,
      "2": 101,
      "3": 102
    }
  }
}
```

### IChuteIoService 配置（新增）

```json
{
  "ChuteIo": {
    "IsHardwareEnabled": false,
    "Mode": "Simulation",
    "Nodes": [
      {
        "NodeKey": "node-1",
        "Brand": "ZhiQian32Relay",
        "IpAddress": "192.168.1.100",
        "Port": 502,
        "MaxChannelCount": 32,
        "Channels": [
          { "ChuteId": 1, "ChannelIndex": 1 },
          { "ChuteId": 2, "ChannelIndex": 2 }
        ]
      },
      {
        "NodeKey": "node-2",
        "Brand": "ZhiQian32Relay",
        "IpAddress": "192.168.1.101",
        "Port": 502,
        "MaxChannelCount": 32,
        "Channels": [
          { "ChuteId": 33, "ChannelIndex": 1 },
          { "ChuteId": 34, "ChannelIndex": 2 }
        ]
      }
    ]
  }
}
```

## 迁移建议

### 短期（保持兼容性）
1. 保留 `IChuteTransmitterPort` 用于现有代码
2. 新功能使用 `IChuteIoService`
3. 通过配置选择使用哪个实现

### 长期（可选重构）
1. 评估将 `SortingExecutionWorker` 和 `ChuteSafetyService` 迁移到 `IChuteIoService`
2. 如果迁移，使用 `ChuteTransmitterPortAdapter` 作为过渡方案
3. 最终可能废弃 `IChuteTransmitterPort`（需要充分测试）

## 优点对比

| 特性 | IChuteTransmitterPort | IChuteIoService |
|------|----------------------|-----------------|
| 多 IP 端点支持 | ❌ | ✅ |
| 品牌无关 | ❌ (与 Modbus 耦合) | ✅ |
| 灵活通道映射 | ❌ (固定线圈地址) | ✅ |
| 支持不同通道数 | ❌ (硬编码) | ✅ (可配置) |
| 精确控制开启时长 | ✅ | ❌ (使用配置) |
| 现有代码兼容 | ✅ | ⚠️ (需适配器) |

## 结论

两个接口都有其存在价值：
- **IChuteTransmitterPort**: 保留用于现有代码和需要精确控制的场景
- **IChuteIoService**: 用于新功能和需要灵活配置的场景

通过适配器模式，可以在需要时桥接两者，保持架构的灵活性。
