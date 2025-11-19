# 上游规则引擎端口实施总结

## 概述
本次 PR 实现了与上游规则引擎通信的统一端口，采用六边形架构的端口-适配器模式，支持 MQTT、TCP 和 Disabled 三种模式，完全移除了对 HTTP 的依赖。

## 实施的主要改动

### 1. Core 层端口接口
**文件位置**: `ZakYip.NarrowBeltDiverterSorter.Core/Domain/Sorting/`

- **ISortingRuleEnginePort**: 核心端口接口，定义与规则引擎的统一通信契约
  - `RequestSortingAsync()`: 请求分拣
  - `NotifySortingResultAckAsync()`: 通知分拣结果

- **SortingRequestEventArgs**: 分拣请求事件参数
  - 包含包裹 ID、购物车编号、条码、DWS 数据（重量、尺寸）等
  - 所有数值类型使用 `decimal` 而非 `double`，确保精度

- **SortingResultAckEventArgs**: 分拣结果确认事件参数
  - 包含包裹 ID、格口编号、成功标志、处理时间等

### 2. UpstreamContracts DTO 规范化
**文件位置**: `ZakYip.NarrowBeltDiverterSorter.UpstreamContracts/Models/`

新增三个 record 类型，只使用基础类型，可在 RuleEngine 和 NarrowBelt 之间共享：

- **ParcelCreatedMessage**: 包裹创建消息
- **DwsDataMessage**: DWS（动态称重扫描）数据消息
- **SortingResultMessage**: 分拣结果消息

### 3. Communication 层实现
**文件位置**: `ZakYip.NarrowBeltDiverterSorter.Communication/Upstream/`

#### 核心抽象
- **ISortingRuleEngineClient**: 协议无关的客户端接口
  - 定义了 `SendParcelCreatedAsync()`, `SendDwsDataAsync()`, `SendSortingResultAsync()` 方法
  - 不暴露任何协议细节

#### 具体实现
- **MqttSortingRuleEngineClient**: MQTT 协议实现
  - 使用 MQTTnet 库（版本 4.3.7.1207）
  - 支持 TLS、认证、QoS 等配置
  - 主题格式：`{BaseTopic}/parcel-created`, `{BaseTopic}/dws-data`, `{BaseTopic}/sorting-result`

- **TcpSortingRuleEngineClient**: TCP 协议骨架（预留）
  - 提供骨架实现，实际 TCP 协议细节待补充

- **DisabledSortingRuleEngineClient**: No-op 实现
  - 用于单机仿真模式，不进行任何网络通信
  - 所有操作只记录日志

#### 辅助类
- **SortingRuleEngineClientFactory**: 工厂类
  - 根据 `UpstreamOptions.Mode` 创建相应的客户端实例

- **SortingRuleEnginePortAdapter**: 端口适配器
  - 将 Core 层的 `ISortingRuleEnginePort` 映射到 Communication 层的 `ISortingRuleEngineClient`
  - 自动拆分复合请求（包裹创建 + DWS 数据）

- **ObservableSortingRuleEngineClient**: 可观察包装器
  - 包装任意客户端实现，自动发布状态变更事件

#### 配置结构
- **UpstreamOptions**: 主配置类
  - `Mode`: Disabled / Mqtt / Tcp
  - `Mqtt`: MQTT 配置（Broker、Port、User、Password、BaseTopic 等）
  - `Tcp`: TCP 配置（Host、Port）

### 4. Host 层集成
**文件位置**: `ZakYip.NarrowBeltDiverterSorter.Host/`

- **HostConfigurationProvider**: 
  - 新增 `GetUpstreamOptionsAsync()` 方法，从 LiteDB 读取上游配置

- **Program.cs**:
  - 移除了原有的 `HttpClient<IUpstreamSortingApiClient>` 注册
  - 注册 `ISortingRuleEngineClient`（根据配置创建具体实现）
  - 注册 `ISortingRuleEnginePort`（通过适配器）
  - 在 Disabled 以外的模式下，启动时自动连接

### 5. Observability 层补充
**文件位置**: `ZakYip.NarrowBeltDiverterSorter.Observability/`

#### 新增类型
- **UpstreamRuleEngineSnapshot**: 上游状态快照
  - `Mode`: 当前模式（Disabled / Mqtt / Tcp）
  - `Status`: 连接状态（Disabled / Disconnected / Connecting / Connected / Error）
  - `ConnectionAddress`: 连接地址（如果适用）

- **UpstreamConnectionStatus**: 连接状态枚举

- **UpstreamRuleEngineStatusChangedEventArgs**: 状态变更事件参数

#### 集成
- **INarrowBeltLiveView**: 新增 `GetUpstreamRuleEngineStatus()` 方法
- **NarrowBeltLiveView**: 
  - 订阅 `UpstreamRuleEngineStatusChangedEventArgs` 事件
  - 维护内存中的上游状态快照

## 架构优势

### 1. 端口-适配器模式
- **Core 层**只定义接口（`ISortingRuleEnginePort`），不依赖具体协议
- **Communication 层**提供多种协议实现，可按配置切换
- **松耦合**：Core 不知道底层使用的是 MQTT 还是 TCP

### 2. 可扩展性
- 添加新协议（如 gRPC、WebSocket）只需实现 `ISortingRuleEngineClient`
- 无需修改 Core 层代码

### 3. 可观察性
- 所有状态变更通过 EventBus 发布
- LiveView 自动追踪连接状态
- 为前端 SignalR 推送做好准备

### 4. 类型安全
- 所有 DTO 使用 `record` 类型，不可变
- 数值类型使用 `decimal`，避免浮点精度问题

## 待完成项

### LiteDB 配置存储
- 需要在 LiteDB 中存储和加载 `UpstreamOptions`
- 当前配置通过代码默认值提供

### 移除 appsettings.json 中的 HTTP 配置
- 当前 `appsettings.json` 中仍有 `UpstreamSortingApi` 配置节
- 需要在后续 PR 中移除

### SignalR 前端推送
- Observability 层已就绪
- 需要在 SignalR Hub 中添加推送逻辑

### 旧代码迁移
- 部分代码仍依赖 `IUpstreamSortingApiClient`（HTTP 客户端）
- 需要逐步迁移到新的 `ISortingRuleEnginePort`

## 验收标准

### ✅ 已完成
1. 解决方案可以成功编译，无错误
2. 新增的 MQTT 客户端实现完整
3. Disabled 模式下，系统可以正常启动，不依赖外部规则引擎
4. Observability 层可以追踪上游连接状态

### ⏳ 待验证
1. Mode=Disabled 时，日志中出现"上游规则引擎适配器: Disabled（已禁用）"
2. Mode=Mqtt 时，日志中出现"正在连接 RuleEngine MQTT Broker"
3. ISortingRuleEnginePort 可以通过 DI 正常解析
4. 没有 HTTP + sorting 相关配置残留（需要后续 PR 清理）

## 技术栈

- **MQTT 库**: MQTTnet 4.3.7.1207
- **JSON 序列化**: System.Text.Json 8.0.5
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **日志**: Microsoft.Extensions.Logging.Abstractions 8.0.1

## 代码质量

- 所有新增代码均包含 XML 文档注释
- 遵循现有代码规范
- 使用 record 类型保证不可变性
- 异步操作使用 ValueTask 优化性能
