# Core（核心层）

## 概述

Core 是整个系统的核心层，包含领域模型、接口契约、配置模型和枚举定义。这一层定义了系统的"业务语言"，所有其他层都依赖 Core 中定义的抽象。

## 职责

### 1. 领域模型（Domain Models）
- 包裹（Parcels）: `ParcelId`, `ParcelLifecycle`
- 小车（Carts）: `CartId`, `CartState`
- 格口（Chutes）: `ChuteId`, `ChuteConfig`
- 主线（MainLine）: 速度、状态等
- 拓扑（Topology）: 轨道布局、位置等

### 2. 接口契约（Contracts）
- `ICartPositionQuery`: 小车位置查询
- `IChuteDirectory`: 格口目录
- `ILineSafetyOrchestrator`: 安全编排器
- `IParcelLifecycleService`: 包裹生命周期服务
- `IMainLineDrive`: 主线驱动抽象
- `IFieldBusClient`: 现场总线客户端抽象

### 3. 配置模型（Configuration）
- `MainLineControlOptions`: 主线控制配置
- `InfeedLayoutOptions`: 入口布局配置
- `ChuteConfig`: 格口配置
- `StartupModeConfiguration`: 启动模式配置

### 4. 领域事件（Domain Events）
- 定义为 `record struct`，后缀为 `EventArgs`
- 例如: `ParcelCreatedEventArgs`, `CartPassedEventArgs`

## 架构原则

### ✅ 允许
- 定义接口和抽象类
- 定义值对象和枚举
- 定义领域事件
- 依赖 BCL 和基础库（如 `Microsoft.Extensions.Logging.Abstractions`）

### ❌ 禁止
- 具体硬件实现
- 数据库操作（如 LiteDB）
- Web 框架（如 SignalR）
- 依赖其他业务层项目（Execution, Host 等）

## 目录结构

```
Core/
├── Abstractions/          # 硬件端口抽象（IPort 接口）
├── Application/           # 应用服务实现
├── Configuration/         # 配置模型
│   ├── MainLineControlOptions.cs
│   ├── InfeedLayoutOptions.cs
│   └── StartupModeConfiguration.cs
├── Domain/                # 领域模型
│   ├── Carts/            # 小车相关
│   ├── Chutes/           # 格口相关
│   ├── Feeding/          # 装载相关
│   ├── MainLine/         # 主线相关
│   ├── Parcels/          # 包裹相关
│   ├── Safety/           # 安全相关
│   ├── Sorting/          # 分拣相关
│   ├── SystemState/      # 系统状态
│   └── Tracking/         # 追踪相关
└── SelfCheck/            # 自检相关
```

## 使用示例

### 定义领域模型
```csharp
namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

public record ParcelId(long Value);

public enum ParcelState
{
    Created,
    Routed,
    Sorting,
    Sorted,
    ForceEjected
}
```

### 定义接口契约
```csharp
namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;

public interface ICartPositionQuery
{
    CartPosition GetPosition(CartId cartId, TimeSpan timestamp);
    CartId? FindCartAtPosition(decimal positionMm);
}
```

### 定义领域事件
```csharp
namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;

public readonly record struct ParcelCreatedEventArgs(
    ParcelId ParcelId,
    DateTime CreatedTime,
    decimal LengthMm
);
```

## 依赖规则

**依赖方向**: `其他所有层 → Core`

**Core 的依赖**:
- ✅ Microsoft.Extensions.Logging.Abstractions
- ✅ System.ComponentModel (用于 Description 特性)
- ✅ UpstreamContracts（上游系统合约）
- ❌ 任何其他业务项目

## 架构锚点

以下是 Core 层定义的关键"架构锚点"，其他层必须通过这些接口与系统交互：

1. **小车位置查询**: 通过 `ICartPositionQuery`
2. **包裹生命周期**: 通过 `IParcelLifecycleService`
3. **格口配置**: 通过 `IChuteConfigProvider`
4. **安全控制**: 通过 `ILineSafetyOrchestrator`
5. **主线驱动**: 通过 `IMainLineDrive`

## 后续工作

- [ ] 补充领域模型文档
- [ ] 添加接口使用示例
- [ ] 建立领域事件目录
- [ ] 添加配置模型默认值说明

## 参考

- [分层架构文档](../architecture/Layering.md)
- [依赖规则文档](../architecture/Dependencies.md)
