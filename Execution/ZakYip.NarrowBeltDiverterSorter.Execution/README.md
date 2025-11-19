# Execution（执行层）

## 概述

Execution 层负责具体的执行逻辑，包括主线驱动控制、分拣计划、包裹装载编排等。这一层实现了 Core 层定义的接口，提供具体的业务逻辑执行能力。

## 职责

- 主线驱动控制（PID 控制、速度管理）
- Vendor 驱动（Rema 变频器、窄带驱动卡等）
- 分拣计划器（`SortingPlanner`）
- 包裹装载计划器（`ParcelLoadPlanner`）
- 吐件计划器（`EjectPlanner`）
- 安全编排器（`LineSafetyOrchestrator`）

## 架构原则

### ✅ 允许
- 实现 Core 中定义的接口
- 依赖 Core, Communication, Observability
- 包含业务逻辑和算法

### ❌ 禁止
- 依赖 Host, UI, Web 框架
- 直接操作数据库（应通过 Infrastructure）
- IO 采集（应由 Ingress 负责）

## 架构锚点

- **MainLineControlService**: 主线控制的官方入口
- **ParcelLoadPlanner**: 包裹装载计划的官方入口
- **SortingPlanner**: 分拣计划的官方入口
- **EjectPlanner**: 吐件计划的官方入口

## 依赖

```
Execution
  ├── Core ✅
  ├── Communication ✅
  └── Observability ✅
```

## 参考

- [分层架构文档](../architecture/Layering.md)
