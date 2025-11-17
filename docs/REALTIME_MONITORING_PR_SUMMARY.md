# 实时监控聚合器与 SignalR 推送实现总结

## 概述

本 PR 为窄带分拣系统增加了完整的实时监控能力，通过 SignalR 向前端推送系统状态更新，包括速度、小车、包裹和设备状态。实现遵循分层架构和零入侵原则。

## 主要变更

### 1. Observability 层 - 领域事件与枚举

**新增文件：**
- `ZakYip.NarrowBeltDiverterSorter.Observability/EventArgs.cs` (扩展)

**新增内容：**
- 枚举：`LineSpeedStatus`, `DeviceStatus`
- 事件参数：
  - `LineSpeedChangedEventArgs` - 主线速度变更
  - `CartAtChuteChangedEventArgs` - 格口下方小车变更
  - `OriginCartChangedEventArgs` - 原点小车变更
  - `ParcelCreatedEventArgs` - 包裹创建
  - `ParcelDivertedEventArgs` - 包裹落格
  - `DeviceStatusChangedEventArgs` - 设备状态变更
  - `CartLayoutChangedEventArgs` - 小车布局变更
  - `CartPositionSnapshot` - 小车位置快照

### 2. Observability 层 - 实时视图聚合器

**新增文件：**
- `ZakYip.NarrowBeltDiverterSorter.Observability/LiveView/INarrowBeltLiveView.cs`
- `ZakYip.NarrowBeltDiverterSorter.Observability/LiveView/NarrowBeltLiveView.cs`
- `ZakYip.NarrowBeltDiverterSorter.Observability/LiveView/Snapshots.cs`

**功能：**
- 订阅事件总线并维护内存快照
- 提供只读接口查询当前系统状态
- 支持的快照类型：
  - `LineSpeedSnapshot` - 主线速度状态
  - `OriginCartSnapshot` - 原点小车
  - `ChuteCartSnapshot` - 格口小车映射
  - `ParcelSummary` - 包裹摘要
  - `DeviceStatusSnapshot` - 设备状态
  - `CartLayoutSnapshot` - 小车布局

### 3. Host 层 - SignalR Hub

**新增文件：**
- `ZakYip.NarrowBeltDiverterSorter.Host/SignalR/NarrowBeltLiveHub.cs`
- `ZakYip.NarrowBeltDiverterSorter.Host/SignalR/LiveViewDtos.cs`
- `ZakYip.NarrowBeltDiverterSorter.Host/SignalR/LiveViewBridgeService.cs`
- `ZakYip.NarrowBeltDiverterSorter.Host/SignalR/LiveViewPushOptions.cs`

**功能：**
- **NarrowBeltLiveHub**：SignalR Hub 提供实时推送接口
  - 支持客户端连接管理
  - 提供格口分组订阅功能
  - 在连接时发送初始状态快照
  
- **LiveViewBridgeService**：后台服务桥接事件总线与 SignalR
  - 订阅所有实时监控事件
  - 带推送频率限制（throttling）避免前端过载
  - 支持周期性推送在线包裹列表
  
- **LiveViewPushOptions**：可配置的推送选项
  - 各类型事件独立的推送间隔配置
  - 默认值：速度 200ms，格口小车 100ms，包裹 50ms 等

### 4. Host 层 - 配置与注册

**修改文件：**
- `ZakYip.NarrowBeltDiverterSorter.Host/Program.cs`
- `ZakYip.NarrowBeltDiverterSorter.Host/appsettings.json`

**变更：**
- 注册 SignalR 服务
- 注册 `INarrowBeltLiveView` 和 `NarrowBeltLiveView`
- 注册 `LiveViewBridgeService` 后台服务
- 映射 SignalR Hub 到 `/hubs/narrowbelt-live`
- 添加 `LiveViewPush` 配置节

### 5. 测试

**新增文件：**
- `ZakYip.NarrowBeltDiverterSorter.Observability.Tests/LiveView/NarrowBeltLiveViewTests.cs`

**测试覆盖：**
- ✅ 主线速度更新
- ✅ 包裹创建和落格
- ✅ 原点小车变更
- ✅ 格口小车变更
- ✅ 设备状态变更
- ✅ 小车布局变更
- ✅ 初始状态验证

**测试结果：** 8/8 通过

### 6. 文档

**新增文件：**
- `docs/SIGNALR_REALTIME_MONITORING.md`

**内容：**
- SignalR 集成指南
- 前端连接示例（TypeScript/JavaScript）
- 完整的数据结构定义
- Vue 3 组件示例
- 配置说明
- 性能优化建议
- 故障排查指南

## 架构遵循

### 分层清晰
```
┌─────────────────────────────────────────┐
│          Frontend (Not in PR)           │
│         SignalR Client Connection       │
└────────────────┬────────────────────────┘
                 │ WebSocket
┌────────────────▼────────────────────────┐
│          Host Layer                     │
│  ┌──────────────────────────────────┐  │
│  │    NarrowBeltLiveHub             │  │ ◄─ SignalR Hub
│  └───────────┬──────────────────────┘  │
│              │                          │
│  ┌───────────▼──────────────────────┐  │
│  │   LiveViewBridgeService          │  │ ◄─ Background Service
│  └───────────┬──────────────────────┘  │    (with throttling)
└──────────────┼──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│       Observability Layer               │
│  ┌──────────────────────────────────┐  │
│  │    NarrowBeltLiveView            │  │ ◄─ State Aggregator
│  └───────────┬──────────────────────┘  │
│              │ subscribes              │
│  ┌───────────▼──────────────────────┐  │
│  │        EventBus                  │  │
│  └───────────┬──────────────────────┘  │
└──────────────┼──────────────────────────┘
               │ publishes
┌──────────────▼──────────────────────────┐
│       Execution Layer                   │
│   (MainLine, Carts, Parcels, etc.)     │
└─────────────────────────────────────────┘
```

### 零入侵原则
- ✅ Core/Execution 层不依赖 SignalR
- ✅ SignalR 只存在于 Host 层
- ✅ 使用事件总线解耦各层
- ✅ Observability 层提供清晰抽象

## SignalR 消息流

**推送的消息：**
1. `LineSpeedUpdated` - 主线速度更新
2. `OriginCartChanged` - 原点小车变更
3. `ChuteCartChanged` - 单个格口小车变更
4. `ChuteCartsUpdated` - 所有格口小车映射
5. `LastCreatedParcelUpdated` - 最后创建的包裹
6. `LastDivertedParcelUpdated` - 最后落格的包裹
7. `OnlineParcelsUpdated` - 在线包裹列表（周期推送）
8. `DeviceStatusUpdated` - 设备状态
9. `CartLayoutUpdated` - 小车布局

**客户端可调用的方法：**
- `JoinChuteGroup(chuteId)` - 加入格口分组
- `LeaveChuteGroup(chuteId)` - 离开格口分组
- `GetCurrentSnapshot()` - 获取当前状态快照

## 推送频率控制

为避免前端过载，实现了基于时间的推送节流：

```json
{
  "LiveViewPush": {
    "LineSpeedPushIntervalMs": 200,
    "ChuteCartPushIntervalMs": 100,
    "OriginCartPushIntervalMs": 100,
    "ParcelCreatedPushIntervalMs": 50,
    "ParcelDivertedPushIntervalMs": 50,
    "DeviceStatusPushIntervalMs": 500,
    "CartLayoutPushIntervalMs": 500,
    "OnlineParcelsPushPeriodMs": 1000,
    "EnableOnlineParcelsPush": true
  }
}
```

**工作原理：**
- 每种事件类型独立跟踪最后推送时间
- 当事件触发间隔小于配置值时，该事件被节流（throttle）
- 在线包裹列表采用定时推送，而非事件驱动

## 性能考虑

1. **内存占用**：
   - 仅维护当前状态快照，不存储历史
   - 在线包裹使用 `ConcurrentDictionary` 高效管理
   
2. **推送效率**：
   - 可配置的节流机制防止过度推送
   - 格口分组允许客户端只订阅关心的数据
   
3. **线程安全**：
   - `NarrowBeltLiveView` 使用 lock 保护状态更新
   - `LiveViewBridgeService` 使用专用锁保护节流状态

## 使用示例

### 前端连接（JavaScript/TypeScript）

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/narrowbelt-live")
    .withAutomaticReconnect()
    .build();

// 订阅主线速度
connection.on("LineSpeedUpdated", (data) => {
    console.log(`速度: ${data.ActualMmps} mm/s, 状态: ${data.Status}`);
});

// 订阅包裹创建
connection.on("LastCreatedParcelUpdated", (data) => {
    console.log(`新包裹: ${data.Barcode} → 格口 ${data.TargetChuteId}`);
});

// 启动连接
await connection.start();
```

详细文档见 `docs/SIGNALR_REALTIME_MONITORING.md`

## 验证步骤

1. ✅ 编译成功（无警告无错误）
2. ✅ 单元测试通过（8/8）
3. ✅ CodeQL 安全扫描通过（0 告警）
4. ✅ 代码审查完成
5. ⏳ 手动验证（需要运行 Host 并用前端连接测试）

## 后续工作（不在本 PR 范围）

1. 前端 UI 实现
2. 实时速度曲线图表
3. 包裹轨迹可视化
4. 历史数据持久化
5. 报警通知机制

## 安全性

- ✅ 无 CodeQL 告警
- ✅ 无敏感信息泄露
- ✅ 输入验证（格口 ID 等）
- ✅ 异常处理完善

## 兼容性

- .NET 8.0
- SignalR 8.0（内置于 ASP.NET Core）
- 前端需要 `@microsoft/signalr` 库

## 回滚计划

若出现问题，可以：
1. 移除 `LiveViewBridgeService` 的注册
2. 移除 SignalR Hub 映射
3. 或直接回滚 PR 的所有更改

核心执行逻辑不受影响，系统仍可正常运行。
