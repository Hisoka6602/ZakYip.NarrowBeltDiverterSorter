# Ports & Adapters 架构收敛实施总结

## 概述

本次重构旨在收敛 Execution/Ingress/Communication/Host 层边界，明确各层职责，遵循 Ports & Adapters（六边形架构）清理中间层。

## 主要变更

### 1. 上游事件类型移至 Core 层

**变更内容：**
- 创建 `Core/Domain/Upstream` 命名空间
- 移动 `UpstreamConnectionStatus` 枚举从 `Observability.LiveView` 到 `Core.Domain.Upstream`
- 移动 `UpstreamMetricsEventArgs` 从 `Observability.Events` 到 `Core.Domain.Upstream`
- 移动 `UpstreamRuleEngineStatusChangedEventArgs` 从 `Observability.Events` 到 `Core.Domain.Upstream`

**理由：**
- 这些类型是上游通讯的核心领域概念，不仅仅是可观测性事件
- Communication 层需要使用这些类型但不应依赖 Observability
- 使得 Core 成为所有层的稳定依赖基础

**影响的文件：**
- `Core/Domain/Upstream/UpstreamConnectionStatus.cs` (新建)
- `Core/Domain/Upstream/UpstreamMetricsEventArgs.cs` (新建)
- `Core/Domain/Upstream/UpstreamRuleEngineStatusChangedEventArgs.cs` (新建)
- `Observability/Events/UpstreamMetricsEventArgs.cs` (删除)
- `Observability/Events/UpstreamRuleEngineStatusChangedEventArgs.cs` (删除)
- `Observability/LiveView/UpstreamRuleEngineSnapshot.cs` (更新引用)
- `Communication/Upstream/ObservableSortingRuleEngineClient.cs` (更新引用)
- `Host/Program.cs` (更新引用)
- `Host/Controllers/UpstreamDiagnosticsController.cs` (更新引用)

### 2. SortingRuleEnginePortAdapter 移至 Execution 层

**变更内容：**
- 创建 `Execution/Upstream` 命名空间
- 移动 `SortingRuleEnginePortAdapter` 从 `Communication.Upstream` 到 `Execution.Upstream`
- 更新所有引用该类型的地方

**理由：**
- `SortingRuleEnginePortAdapter` 是一个端口适配器，实现了 Core 层的 `ISortingRuleEnginePort` 接口
- 根据 Ports & Adapters 模式，适配器应该在应用层（Execution），而不是基础设施层（Communication）
- Communication 应该只包含纯通讯协议实现（如 MqttSortingRuleEngineClient、TcpSortingRuleEngineClient）
- Execution 依赖 Communication 的客户端来实现适配器逻辑，这是正确的依赖方向

**影响的文件：**
- `Execution/Upstream/SortingRuleEnginePortAdapter.cs` (移动并更新命名空间)
- `Communication/Upstream/SortingRuleEnginePortAdapter.cs` (删除)
- `Host/Program.cs` (更新 DI 注册)
- `E2ETests/UpstreamIntegrationTests.cs` (更新引用)

### 3. Communication 层项目引用清理

**变更内容：**
- 保留 Communication 对 Core 的引用（需要 IEventBus 和上游事件类型）
- 保留 Communication 对 UpstreamContracts 的引用（消息协议定义）
- 移除 Communication 对 Observability 的引用

**理由：**
- Communication 应该专注于通讯协议，不应该知道可观测性的概念
- Communication 使用 IEventBus 发布事件，但 IEventBus 是 Core 的抽象
- 上游事件已经移至 Core，所以 Communication 不再需要 Observability

**影响的文件：**
- `Communication/ZakYip.NarrowBeltDiverterSorter.Communication.csproj`

### 4. Host 层 DI 扩展方法

**变更内容：**
- 创建 `Host/Extensions` 目录
- 创建 `ObservabilityServiceCollectionExtensions` 类
- 实现 `AddObservability()` 扩展方法
- 更新 `Program.cs` 使用新的扩展方法

**理由：**
- Program.cs 包含大量 DI 注册代码，难以阅读和维护
- 将各层的 DI 注册封装到扩展方法中，使 Program.cs 成为清晰的组合根
- 便于按层级理解和管理依赖注入配置

**影响的文件：**
- `Host/Extensions/ObservabilityServiceCollectionExtensions.cs` (新建)
- `Host/Program.cs` (简化 Observability 层注册)

## 当前架构状态

### 项目依赖关系

```
Core (核心层)
  ├─ 无依赖
  └─ 职责：领域模型、服务接口、事件定义

Communication (通讯层)
  ├─ 依赖：Core, UpstreamContracts
  └─ 职责：FieldBus、MQTT、TCP 通讯协议实现

Execution (执行层)
  ├─ 依赖：Core, Communication, Observability
  └─ 职责：驱动设备、执行算法、端口适配器

Ingress (输入层)
  ├─ 依赖：Core, Communication, Observability
  └─ 职责：监听传感器、按钮，转换为领域事件

Observability (可观测层)
  ├─ 依赖：Core
  └─ 职责：事件总线、录制回放、实时视图

Host (宿主层)
  ├─ 依赖：所有层
  └─ 职责：组合根、托管 Worker、对外 API
```

### 各层职责验证

#### ✅ Communication 层
**应该包含：**
- FieldBusClient 和配置/重连/节流逻辑
- MqttClient / TcpClient 等底层通讯包装
- 只操作线圈/寄存器/字节帧/Topic 报文

**不应该包含：**
- ✅ 不再依赖 Observability
- ✅ 不包含端口适配器（已移至 Execution）
- ⚠️ 但仍包含 `SortingRuleEngineClient` 系列类（含 "Sorting" 词汇）
  - 注：这些是通讯协议层面的客户端，而非业务逻辑

#### ✅ Execution 层
**应该包含：**
- 驱动实现（RemaMainLineDrive、SimulatedMainLineDrive）
- 执行服务（SortingPlanner、EjectPlanner、ChuteSafetyService）
- 运行时（MainLineRuntime、ParcelRoutingRuntime、SafetyRuntime）
- 端口适配器（SortingRuleEnginePortAdapter）

**不应该包含：**
- ✅ 不包含 ASP.NET 类型
- ✅ 不包含 SignalR Hub
- ✅ 不包含 LiteDb 具体类型
- ✅ 不包含 Worker 类型（都在 Host）

**当前状态：**
- ⚠️ 仍依赖 Observability（用于发布可观测性事件如 `ParcelCreatedFromInfeedEventArgs`）
- 说明：这些 DTO 形式的事件用于可观测性，与 Core 的领域事件不同

#### ✅ Ingress 层
**应该包含：**
- 传感器监视器（InfeedSensorMonitor、OriginSensorMonitor）
- 按钮监视器（PanelButtonMonitor）
- IO 监视器（ChuteIoMonitor）

**当前状态：**
- ⚠️ 仍依赖 Observability（用于发布可观测性事件）
- 这些监视器同时发布 Core 领域事件和 Observability DTO 事件

#### ✅ Host 层
**应该包含：**
- Worker 类（MainLineControlWorker、ParcelRoutingWorker 等）
- DI 注册扩展方法
- Controllers 和 SignalR Hubs
- 配置提供者

**不应该包含：**
- ✅ 不包含业务逻辑

#### ✅ Observability 层
**应该包含：**
- InMemoryEventBus
- LiveView 聚合器
- 事件录制器
- 包裹时间线服务

**不应该包含：**
- ✅ 不修改业务状态
- ✅ 只消费事件，不回写

## 验收状态

### ✅ 已完成
- [x] Communication 不依赖 Observability
- [x] 上游事件类型在 Core 中
- [x] SortingRuleEnginePortAdapter 在 Execution 中
- [x] Host 开始使用 DI 扩展方法
- [x] 项目成功编译

### ⚠️ 部分完成
- [~] Execution 只依赖 Core + Communication（仍依赖 Observability 用于事件发布）
- [~] Ingress 只依赖 Core + Communication（仍依赖 Observability 用于事件发布）

### ❌ 未完成（可选）
- [ ] 完整的 DI 扩展方法套件（AddCore、AddExecution、AddIngress、AddCommunication）
- [ ] 完全移除 Execution/Ingress 对 Observability 的依赖
- [ ] 运行完整测试套件验证
- [ ] 仿真模式系统验证

## 剩余工作说明

### Execution/Ingress 对 Observability 的依赖

**当前情况：**
Execution 和 Ingress 仍然依赖 Observability 项目，主要用于发布可观测性事件（DTO 形式）。

**为什么保留：**
1. **最小改动原则**：完全移除需要大规模重构事件发布机制
2. **架构合理性**：这些层发布可观测性事件是合理的（虽然不完美）
3. **功能等价性**：当前代码同时发布领域事件（Core）和可观测性事件（Observability）

**如需完全移除，需要：**
1. 将所有 Observability.Events 中的事件类型移至 Core
2. 创建事件转换器，将 Core 事件转换为 Observability DTO
3. 让 Observability 层订阅 Core 事件并自己创建 DTO
4. 修改 Execution/Ingress 只发布 Core 事件

## 结论

本次重构成功实现了主要目标：

1. **Communication 层纯化**：不再依赖 Observability，专注通讯协议
2. **Core 层强化**：成为所有层的稳定基础，包含上游领域概念
3. **Execution 层归位**：端口适配器正确放置在应用层
4. **Host 层组织**：开始使用扩展方法改善 DI 注册组织

架构边界更加清晰，符合 Ports & Adapters 模式的核心理念。剩余的 Observability 依赖是可接受的权衡，在最小改动和架构纯度之间取得平衡。
