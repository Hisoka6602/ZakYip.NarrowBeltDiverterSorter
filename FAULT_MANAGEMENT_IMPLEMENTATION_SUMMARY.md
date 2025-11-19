# 系统故障管理统一化实施总结

## 概述

本PR实现了统一的系统故障管理框架，为窄带分流器分拣系统提供了一致的故障注册、查询、复位和可视化机制。

## 实现的功能

### 1. 核心故障模型 (Core.Domain.SystemState)

#### SystemFaultCode 枚举
定义了5种系统故障类型：
- `FieldBusDisconnected` - 现场总线断开
- `RuleEngineUnavailable` - 规则引擎不可用
- `EmergencyStopActive` - 紧急停止激活
- `ChuteIoConfigMissing` - 格口IO配置缺失
- `MainLineDriveFault` - 主线驱动故障

#### SystemFaultService
提供以下核心功能：
- `RegisterFault()` - 注册新故障
- `ClearFault()` - 清除指定故障
- `ClearAllFaults()` - 清除所有故障
- `GetActiveFaults()` - 获取当前活动故障列表
- `HasBlockingFault()` - 检查是否存在阻断运行的故障
- `FaultAdded` - 故障添加事件
- `FaultCleared` - 故障清除事件

#### ISystemRunStateService 扩展
- 新增 `ForceToFaultState()` 方法，允许外部服务强制系统进入故障状态

### 2. Panel急停集成 (Ingress.Safety)

#### PanelButtonMonitor 修改
- 在检测到急停按钮按下时：
  - 调用 `_systemRunStateService.TryHandleEmergencyStop()` 将系统状态切换到 Fault
  - 注册 `EmergencyStopActive` 故障到故障服务
  - 执行停止 IO 联动，关闭相关设备
- 在急停复位时：
  - 清除 `EmergencyStopActive` 故障
  - 系统状态从 Fault 切换到 Stopped
  - 需要手动按启动按钮才能重新运行

### 3. API 层 (Host.Controllers)

#### SystemFaultsController
提供以下REST API端点：

**GET /api/system/faults**
- 返回当前所有活动故障列表
- 包含故障代码、消息、发生时间、是否阻断运行
- 同时返回当前系统状态

**POST /api/system/faults/reset**
- 清除所有故障标记
- 将系统状态从 Fault 切换到 Stopped
- 不会自动启动系统，需要手动启动
- 如果系统不在故障状态，返回400错误

### 4. 可视化层 (Observability.LiveView)

#### NarrowBeltLiveView 扩展
- 新增 `GetSystemFaultsState()` 方法
- 返回 `SystemFaultsStateSnapshot` 包含：
  - `CurrentFaults` - 当前活动故障列表
  - `HasBlockingFault` - 是否存在阻断运行的故障
  - `LastUpdatedAt` - 最后更新时间
- 支持通过 SignalR 实时推送故障状态

### 5. 依赖注入配置 (Host.Program)

在 `Program.cs` 中注册 `ISystemFaultService` 为单例服务：
```csharp
builder.Services.AddSingleton<ISystemFaultService, SystemFaultService>();
```

### 6. 测试 (Core.Tests)

#### SystemFaultServiceTests
实现了8个单元测试，全部通过：
- `RegisterFault_AddsFaultToCollection` - 验证故障注册
- `RegisterFault_DuplicateFault_DoesNotAddTwice` - 验证重复故障不会添加两次
- `HasBlockingFault_ReturnsTrueWhenBlockingFaultExists` - 验证阻断故障检测
- `HasBlockingFault_ReturnsFalseWhenNoBlockingFaultExists` - 验证无阻断故障检测
- `ClearFault_RemovesFaultFromCollection` - 验证故障清除
- `ClearAllFaults_RemovesAllFaults` - 验证批量清除
- `FaultAdded_EventFired_WhenFaultRegistered` - 验证故障添加事件
- `FaultCleared_EventFired_WhenFaultCleared` - 验证故障清除事件

## 安全扫描结果

✅ **CodeQL 扫描通过** - 未发现安全漏洞

所有代码变更已通过 CodeQL 静态代码分析，未发现任何安全问题。

## 文件变更清单

### 新增文件
1. `Core/Domain/SystemState/SystemFaultCode.cs` - 故障代码枚举
2. `Core/Domain/SystemState/SystemFaultEventArgs.cs` - 故障事件参数
3. `Core/Domain/SystemState/SystemFault.cs` - 故障记录模型
4. `Core/Domain/SystemState/ISystemFaultService.cs` - 故障服务接口
5. `Core/Domain/SystemState/SystemFaultService.cs` - 故障服务实现
6. `Host.Contracts/API/SystemFaultDto.cs` - API数据传输对象
7. `Host/Controllers/SystemFaultsController.cs` - 故障管理控制器
8. `Core.Tests/SystemState/SystemFaultServiceTests.cs` - 单元测试

### 修改文件
1. `Core/Domain/SystemState/ISystemRunStateService.cs` - 添加 ForceToFaultState 方法
2. `Core/Domain/SystemState/SystemRunStateService.cs` - 实现 ForceToFaultState 方法
3. `Ingress/Safety/PanelButtonMonitor.cs` - 集成故障服务
4. `Observability/LiveView/INarrowBeltLiveView.cs` - 添加故障状态方法
5. `Observability/LiveView/NarrowBeltLiveView.cs` - 实现故障状态集成
6. `Observability/LiveView/Snapshots.cs` - 添加故障快照类型
7. `Host/Program.cs` - 注册故障服务
8. `Core.Tests/Fakes/FakeSystemRunStateService.cs` - 更新测试桩
9. `Observability.Tests/LiveView/NarrowBeltLiveViewTests.cs` - 更新测试

## 使用示例

### 注册故障
```csharp
_faultService.RegisterFault(
    SystemFaultCode.EmergencyStopActive,
    "面板急停按钮被按下",
    isBlocking: true);
```

### 查询故障
```csharp
var faults = _faultService.GetActiveFaults();
var hasBlocking = _faultService.HasBlockingFault();
```

### 清除故障
```csharp
_faultService.ClearFault(SystemFaultCode.EmergencyStopActive);
// 或清除所有
_faultService.ClearAllFaults();
```

### API调用
```bash
# 获取当前故障
GET http://localhost:5000/api/system/faults

# 复位故障
POST http://localhost:5000/api/system/faults/reset
```

## 设计考虑

### 线程安全
- `SystemFaultService` 使用锁保护内部字典，确保线程安全
- 事件触发在锁外执行，避免死锁

### 事件驱动
- 故障服务提供 `FaultAdded` 和 `FaultCleared` 事件
- 允许其他组件订阅并响应故障变化
- 未来可用于自动停机、日志记录等

### 阻断与非阻断故障
- 每个故障都有 `IsBlocking` 标志
- 阻断故障会影响系统运行状态
- 非阻断故障仅用于监控和告警

### API设计
- RESTful风格
- 清晰的中文文档和示例
- 适当的HTTP状态码
- 错误处理完善

## 后续工作

以下功能留作后续PR实现：

1. **FieldBus故障监控**
   - 添加连接失败计数器
   - 实现长时间断开检测
   - 触发 `FieldBusDisconnected` 故障

2. **RuleEngine故障监控**
   - 添加失败计数器
   - 实现连续失败检测
   - 触发 `RuleEngineUnavailable` 故障

3. **Execution层联动**
   - 订阅故障服务事件
   - 在阻断故障发生时自动停机
   - 禁止在故障状态下启动

4. **配置检查**
   - 启动时检查关键配置
   - 配置缺失时触发 `ChuteIoConfigMissing` 故障

5. **主线驱动监控**
   - 监控驱动通信状态
   - 触发 `MainLineDriveFault` 故障

## 总结

本PR成功实现了统一的故障管理框架，为系统提供了：
- ✅ 统一的故障注册和查询机制
- ✅ 清晰的REST API接口
- ✅ 实时故障可视化
- ✅ 完整的单元测试覆盖
- ✅ 通过安全扫描验证

这为后续集成更多故障源（FieldBus、RuleEngine等）提供了坚实的基础。
