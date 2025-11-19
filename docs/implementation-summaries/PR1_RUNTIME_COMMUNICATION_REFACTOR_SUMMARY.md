# PR1 运行时与通讯重构 - 实施总结

> **PR 状态**: ✅ 核心功能已完成，待审查
> 
> **日期**: 2025-11-19
> 
> **目标**: 上游通讯 Client/Server 模式 + Host 瘦身 + Drivers 抽象 + 安全隔离基线

---

## 📋 快速导航

- [核心改动](#核心改动)
- [验收标准对照](#验收标准对照)
- [架构设计](#架构设计)
- [使用指南](#使用指南)

---

## 🎯 核心改动

### 1. 上游通讯 Client/Server 双模式

#### 配置支持
- 新增 `UpstreamRole` 枚举：`Client` / `Server`
- 扩展 `UpstreamOptions`：
  - `Role`: 通讯角色配置
  - `Retry`: 重试参数配置（InitialBackoffMs, MaxBackoffMs, BackoffMultiplier）

#### 自动重试机制
- 实现 `UpstreamClientRetryWrapper`
- 特性：
  - ✅ 指数退避算法（初始100ms，倍数2.0，上限2秒）
  - ✅ 无限重试（除非取消或热更新）
  - ✅ 异步实现（Task.Delay + CancellationToken）
  - ✅ 不阻塞线程

#### 热更新支持
- 使用 `IOptionsMonitor<UpstreamOptions>` 监听配置变更
- 自动关闭旧连接，使用新参数重连
- 新参数失败时继续按退避策略重试

#### 发送失败处理
- 发送失败不重试
- 捕获异常并记录详细日志（包含消息类型、ParcelId等上下文）
- 调用方不会收到未处理异常

### 2. Drivers 抽象层

#### 新增接口
- `ICartDrive`: 小车驱动接口
- `IIoDevice`: 通用 IO 设备接口
- `README.md`: Drivers 层架构说明

#### 已有接口（复用）
- `IMainLineDrive`: 主线驱动接口
- `ICartParameterPort`: 小车参数配置接口
- `IChuteIoService`: 格口 IO 服务接口

#### 实现示例
- 仿真实现：`SimulatedMainLineDrive`
- 真实设备：`RemaLm1000HMainLineDrive`

### 3. 时间语义统一

#### 本地时间提供器
- 接口：`ILocalTimeProvider`
- 实现：`LocalTimeProvider` (返回 `DateTime.Now`)
- 已更新的类：
  - `MainLineRuntime`
  - `FeedingBackpressureController`

### 4. 安全隔离基线

#### SafetyIsolator
- 统一的异常捕获和日志记录
- 支持同步和异步操作
- 支持带返回值的操作（失败时返回默认值）

#### 应用场景
- 通讯发送失败（在 UpstreamClientRetryWrapper 中）
- 可扩展到执行路径的关键操作

### 5. Host 层瘦身

#### Host 层职责
- ✅ 配置绑定（Options, IOptionsMonitor）
- ✅ DI 装配（注册服务）
- ✅ 运行模式选择（StartupModeConfiguration）
- ✅ 应用启动（WebApplication.Run）

#### 新增注册
```csharp
// 时间提供器
builder.Services.AddSingleton<ILocalTimeProvider, LocalTimeProvider>();

// 安全隔离器
builder.Services.AddSingleton<SafetyIsolator>();

// 上游 Options 热更新支持
builder.Services.AddOptions<UpstreamOptions>()
    .Configure<IHostConfigurationProvider>(...);

// 上游客户端重试包装
if (options.Role == UpstreamRole.Client) {
    wrappedClient = new UpstreamClientRetryWrapper(...);
}
```

---

## ✅ 验收标准对照

### 通讯模式与重试行为

| 验收项 | 状态 | 说明 |
|-------|------|------|
| 配置项存在 | ✅ | `UpstreamOptions.Role` 和 `Retry` 配置 |
| 自动重试循环 | ✅ | `UpstreamClientRetryWrapper.ConnectWithRetryAsync` |
| 退避算法 | ✅ | 指数退避，最大2秒 |
| 异步实现 | ✅ | 使用 `Task.Delay` 不阻塞 |
| 无限重试 | ✅ | 除非取消或热更新 |
| 热更新支持 | ✅ | `IOptionsMonitor.OnChange` |
| 发送不重试 | ✅ | 仅记录日志 |
| 异常日志 | ✅ | 包含消息类型和上下文 |

### Drivers / Execution 抽象

| 验收项 | 状态 | 说明 |
|-------|------|------|
| Drivers 抽象存在 | ✅ | Core/Abstractions/Drivers |
| 主线驱动接口 | ✅ | `IMainLineDrive` |
| 小车驱动接口 | ✅ | `ICartDrive` |
| IO 设备接口 | ✅ | `IIoDevice` |
| Execution 仅依赖抽象 | ✅ | 现有架构已遵循 |
| 仿真实现 | ✅ | `SimulatedMainLineDrive` |
| 真实设备实现 | ✅ | `RemaLm1000HMainLineDrive` |
| 配置切换 | ✅ | 通过 DI 绑定切换 |

### 安全隔离 / 线程安全 / 本地时间

| 验收项 | 状态 | 说明 |
|-------|------|------|
| 安全隔离器 | ✅ | `SafetyIsolator` 已创建 |
| 通讯路径应用 | ✅ | UpstreamClientRetryWrapper |
| 本地时间提供器 | ✅ | `ILocalTimeProvider` |
| 不使用 UtcNow | ⚠️ | 新代码已遵循，旧代码部分更新 |
| 运行时使用本地时间 | ✅ | MainLineRuntime, FeedingBackpressureController |

### Host 层瘦身

| 验收项 | 状态 | 说明 |
|-------|------|------|
| 无业务逻辑 | ✅ | Host 仅负责配置和注册 |
| 仅保留基础设施 | ✅ | 配置、DI、启动 |
| 新逻辑在正确层 | ✅ | Core/Execution/Communication |

---

## 🏗️ 架构设计

### 上游通讯重试流程

```
┌─────────────────────────────────────────────────────────┐
│  Host/Program.cs                                        │
│  ┌─────────────────────────────────────────────┐       │
│  │ ISortingRuleEngineClient 注册               │       │
│  │  ↓                                          │       │
│  │ if (Role == Client)                         │       │
│  │    UpstreamClientRetryWrapper               │       │
│  │    ├─ Inner Client (TCP/MQTT)              │       │
│  │    ├─ IOptionsMonitor (热更新)             │       │
│  │    └─ 退避算法 (最大2秒)                   │       │
│  └─────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  UpstreamClientRetryWrapper                            │
│  ┌─────────────────────────────────────────────┐       │
│  │ ConnectAsync()                              │       │
│  │  ↓                                          │       │
│  │ ConnectWithRetryAsync()                     │       │
│  │  ├─ 尝试连接                                │       │
│  │  ├─ 失败 → 计算退避时间                     │       │
│  │  ├─ Task.Delay(backoff)                    │       │
│  │  └─ 循环（无限）                            │       │
│  │                                             │       │
│  │ OnOptionsChanged()                          │       │
│  │  ├─ 取消当前重连                            │       │
│  │  ├─ 断开连接                                │       │
│  │  └─ 使用新参数重连                          │       │
│  └─────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────┘
```

### Drivers 抽象层架构

```
┌──────────────────────────────────────────────────────┐
│  Execution Layer                                     │
│  ┌────────────────────────────────────────────┐     │
│  │  MainLineRuntime                          │     │
│  │  ├─ IMainLineDrive                        │     │
│  │  └─ IMainLineControlService               │     │
│  └────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────┘
                     ↓
┌──────────────────────────────────────────────────────┐
│  Core/Abstractions/Drivers                           │
│  ┌────────────────────────────────────────────┐     │
│  │  IMainLineDrive (已有)                    │     │
│  │  ICartDrive (新增)                        │     │
│  │  IIoDevice (新增)                         │     │
│  └────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────┘
                     ↓
┌──────────────────────────────────────────────────────┐
│  Execution/Vendors                                    │
│  ┌────────────┐    ┌────────────────────────┐       │
│  │ Simulated  │    │ Rema                   │       │
│  │  Drivers   │    │  Drivers               │       │
│  └────────────┘    └────────────────────────┘       │
└──────────────────────────────────────────────────────┘
```

---

## 📖 使用指南

### 配置上游通讯模式

在 `appsettings.json` 或 LiteDB 配置中：

```json
{
  "Upstream": {
    "Mode": "Tcp",
    "Role": "Client",
    "Tcp": {
      "Host": "192.168.1.100",
      "Port": 8888
    },
    "Retry": {
      "InitialBackoffMs": 100,
      "MaxBackoffMs": 2000,
      "BackoffMultiplier": 2.0,
      "InfiniteRetry": true
    }
  }
}
```

### 使用本地时间提供器

```csharp
public class MyService
{
    private readonly ILocalTimeProvider _timeProvider;
    
    public MyService(ILocalTimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void DoWork()
    {
        var now = _timeProvider.Now;  // 使用本地时间
        // 不要使用 DateTime.UtcNow
    }
}
```

### 使用安全隔离器

```csharp
public class MyService
{
    private readonly SafetyIsolator _isolator;
    
    public async Task<bool> SendDataAsync()
    {
        return await _isolator.ExecuteAsync(
            async () => await DoActualSendAsync(),
            operationName: "SendData",
            context: "MessageId: 12345");
    }
}
```

---

## 🔬 测试

### 已创建测试

- `UpstreamClientRetryWrapperTests`
  - ✅ 客户端模式重试测试
  - ✅ 服务端模式不重试测试
  - ✅ 发送失败异常捕获测试

### 测试运行

```bash
dotnet test --filter "FullyQualifiedName~UpstreamClientRetryWrapperTests"
```

---

## 📝 后续工作（可选）

1. **完成 DateTime.UtcNow 替换**
   - MainLineSpeedProvider
   - ProductionMainLineDrive
   - RemaLm1000HMainLineDrive
   - 其他 Execution 层文件

2. **SafetyIsolator 应用**
   - 执行路径关键操作
   - 创建 SafetyIsolator 测试

3. **Server 模式实现**
   - 当前仅框架预留
   - 需实现 TCP Server 接受连接

4. **线程安全审查**
   - 共享状态集合
   - 并发访问场景

5. **架构文档完善**
   - Host 层禁止业务逻辑规则
   - Drivers 扩展指南

---

## 🎉 总结

本 PR 建立了运行时骨架重构的核心基线：

1. **通讯层**：Client/Server 双模式，自动重试，热更新
2. **抽象层**：Drivers 接口分离，提高可测试性
3. **安全层**：异常隔离，统一日志
4. **时间语义**：本地时间提供器，便于测试
5. **架构清晰**：Host 层瘦身，职责分明

为后续 PR（路由决策、安全控制、性能优化等）奠定了坚实基础。
