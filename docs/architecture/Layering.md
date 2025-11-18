# ZakYip.NarrowBeltDiverterSorter 分层架构基线

## 概述

本文档定义了 ZakYip.NarrowBeltDiverterSorter 项目的统一分层架构，明确各层职责和边界。

## 分层结构

```
┌─────────────────────────────────────────────────────────┐
│                    Host / Simulation                     │
│              （组合根，负责DI和程序入口）                  │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────┬──────────────┬──────────────┬─────────────┐
│  Execution   │   Ingress    │ Observability│Infrastructure│
│（执行编排）   │  （IO采集）   │  （观测）     │  （持久化）   │
└──────────────┴──────────────┴──────────────┴─────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                         Core                             │
│          （领域模型、接口、配置、枚举）                    │
└─────────────────────────────────────────────────────────┘
```

## 各层职责

### 1. Core（核心层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Core`

**职责**:
- 存放领域模型、值对象、枚举（带 Description）
- 定义领域事件载荷（如 `record struct ...EventArgs`）
- 定义关键接口契约（如 `ICartPositionQuery`、`IChuteDirectory`、`ILineSafetyOrchestrator`、包裹生命周期接口等）
- 定义配置模型 + 默认值契约
- 存放 `StartupModeConfiguration`（启动模式配置）

**不允许**:
- 具体硬件协议实现
- LiteDB 等持久化操作
- SignalR Hub
- 日志具体实现
- 依赖其他业务层项目

**依赖**:
- 只可依赖 BCL 和基础库（如 `Microsoft.Extensions.Logging.Abstractions`）
- 可依赖 `UpstreamContracts`（上游系统合约）

**架构锚点**:
- 所有关于小车位置查询的服务，都必须通过 `ICartPositionQuery` 契约穿过这一层
- 所有配置模型都定义在 `Core.Configuration` 命名空间
- 所有领域事件都定义为 `record struct`，后缀为 `EventArgs`

---

### 2. Execution（执行层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Execution`

**职责**:
- 具体执行逻辑（主线驱动、PID 控制、硬件驱动编排、吐件编排器等）
- Vendor 相关驱动（如 Rema 变频器命令帧、窄带驱动卡协议等）
- 基于 Core 接口的实现（如 `RemaMainlineDrive : IMainlineDrive`）
- 主线控制服务、分拣计划器、安全编排器等

**不允许**:
- 引用 Host、UI、Web 框架
- 直接操作数据库（应通过 Infrastructure 提供的接口）

**依赖**:
- Core
- Communication（通信层，用于现场总线等）
- Observability（用于发布事件和日志）

**架构锚点**:
- `MainLineControlService` 是主线控制的官方入口
- `ParcelLoadPlanner` 是包裹装载计划的官方入口
- `SortingPlanner` 是分拣计划的官方入口

---

### 3. Ingress（入口层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Ingress`

**职责**:
- IO 采集层（光电、编码器、离散点等）
- 传感器监视器（`OriginSensorMonitor`、`InfeedSensorMonitor`）
- 外部系统输入网关（如上游 WCS/VMS 的 Adapter，视情况可放在 Infrastructure）
- 主要职责是"把外界事件变成领域事件"

**不允许**:
- 业务逻辑决策（应该只做采集和转换）
- 引用 Host

**依赖**:
- Core
- Communication（通信层）

---

### 4. Observability（观测层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Observability`

**职责**:
- 日志封装
- 录制/回放（Recording/Replay）
- 分析（Analytics）
- LiveView 聚合器
- 结构化事件追踪 / Metric 抽象
- 事件总线（`IEventBus`）

**不允许**:
- 直接控制硬件
- 业务逻辑决策

**依赖**:
- 理想情况下不依赖其他业务项目（只依赖 BCL）
- 当前状态：无项目引用（✓ 符合架构）

**架构锚点**:
- `IEventBus` 是事件发布/订阅的官方入口
- `INarrowBeltLiveView` 是实时视图聚合的官方入口
- 录制/回放功能通过 `IEventRecordingManager` 和 `IEventRecorder` 接口

---

### 5. Infrastructure（基础设施层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Infrastructure`

**职责**:
- LiteDB 配置存储实现
- 持久化实现
- 某些外部库封装
- 配置仓储（`IMainLineOptionsRepository` 等）

**不允许**:
- 业务逻辑
- 硬件控制逻辑

**依赖**:
- Core
- Communication（用于某些配置）

---

### 6. Communication（通信层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Communication`

**职责**:
- 现场总线客户端（`IFieldBusClient`）
- 底层通信协议
- 网络通信封装

**依赖**:
- Core
- UpstreamContracts

**注意**: 此项目角色需要进一步明确，可能应归入 Infrastructure 或保持独立

---

### 7. UpstreamContracts（上游合约层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.UpstreamContracts`

**职责**:
- 定义与上游系统（WCS/VMS）的接口合约
- 上游 API 模型

**依赖**:
- 无项目依赖（只依赖 BCL）

---

### 8. Simulation（仿真层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Simulation`

**职责**:
- 仿真用的驱动实现（`SimMainlineDrive` 等）
- 模拟 IO 实现
- 模拟上游 / 设备
- 仿真场景 Runner、场景配置等
- 独立可执行程序（用于仿真测试）

**依赖**:
- Core
- Execution（复用执行逻辑）
- Ingress（模拟 IO）
- Infrastructure（配置加载）
- **⚠️ 架构债务**: 当前依赖 Host（需要使用 Workers），应将 Workers 移至 Execution 或创建共享层

---

### 9. Host（主机层）

**项目**: `ZakYip.NarrowBeltDiverterSorter.Host`

**职责**:
- 组合根：负责 DI 注册、配置加载（LiteDB + 默认）
- Kestrel/URL 配置
- SignalR Hub
- API Controller
- 后台工作器（Workers）- **注意**: 这些应该考虑移到 Execution
- 不实现具体领域逻辑，只组装模块和暴露端点
- 必须始终可编译运行

**依赖**:
- 可以依赖所有其他业务项目（作为组装者）

**架构锚点**:
- `Program.cs` 是整个系统的组合根
- 所有 DI 注册在 `Program.cs` 中完成
- `ProductionMainLineSetpointProvider` 提供生产环境的主线设定点

---

### 10. Tests（测试层）

**项目**: `*.Tests`, `E2ETests`

**职责**:
- 单元测试
- 集成测试
- E2E 测试

**依赖**:
- 可以依赖任何业务项目

---

## 配置与 LiteDB 改造

### 配置分层

- **配置模型**: 在 `Core.Configuration`
- **配置默认值**: 在 `Core.Configuration`（通过 `NarrowBeltDefaultConfigSeeder`）
- **配置持久化**: 在 `Infrastructure`（`LiteDbSorterConfigurationStore`）
- **配置组装**: 在 `Host`（通过 `IHostConfigurationProvider`）

### 配置行为要求

- 所有配置都有默认值
- LiteDB 挂掉也能跑（使用默认配置）
- Host 只负责拼装 `IHostConfigurationProvider` 并注入到需要的位置

---

## 后续工作

1. **Worker 重构**: 将 `MainLineControlWorker`、`ParcelRoutingWorker`、`SortingExecutionWorker` 等从 Host 移到 Execution
2. **Communication 项目定位**: 明确是否应归入 Infrastructure
3. **Simulation 依赖解耦**: 移除 Simulation -> Host 依赖
4. **UpstreamContracts 整合**: 考虑是否应合并到 Core
5. **架构约束测试**: 建立自动化测试验证依赖规则

---

## 版本历史

- **v1.0** (2025-11-18): 初始版本，建立架构基线
