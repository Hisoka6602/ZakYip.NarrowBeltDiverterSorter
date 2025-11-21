# 并发安全与异常处理规范

本文档定义了项目中并发访问控制和异常隔离的统一模式和最佳实践。

## 目录

1. [并发安全模型](#并发安全模型)
2. [SafetyIsolator 异常隔离](#safetyisolator-异常隔离)
3. [实施指南](#实施指南)
4. [验证清单](#验证清单)

---

## 并发安全模型

### 原则

本项目采用 **明确并发模型**，要求所有在多线程环境下访问的共享状态必须具备线程安全保护。

### 并发安全策略

项目采用 **分层并发安全策略**，优先级如下：

**优先级：线程安全集合 > 异步锁(SemaphoreSlim) > lock**

#### 策略 1：使用线程安全集合（最高优先级，首选方案）

**绝对优先使用线程安全集合，避免使用 lock。**

对于需要并发读写的集合，优先使用 .NET 提供的线程安全集合类型或不可变集合：

**适用场景：**
- 高频并发读写
- 多个线程同时访问
- 状态字典、缓存、注册表
- 几乎所有需要并发保护的集合场景

**推荐类型：**
```csharp
using System.Collections.Concurrent;
using System.Collections.Immutable;

// ✅ 使用 ConcurrentDictionary 替代 Dictionary
private readonly ConcurrentDictionary<string, bool> _states = new();

// ✅ 使用 ConcurrentQueue 替代 Queue
private readonly ConcurrentQueue<Event> _eventQueue = new();

// ✅ 使用 ConcurrentBag 替代 List（无序场景）
private readonly ConcurrentBag<Item> _items = new();

// ✅ 使用 ImmutableList 用于初始化后不变的数据（原子替换模式）
private ImmutableList<Config> _configs = ImmutableList<Config>.Empty;

// ✅ 使用 IReadOnlyList 接口返回只读视图
public IReadOnlyList<Item> GetItems() => _items;
```

**示例：**

```csharp
// ✅ 正确：使用 ConcurrentDictionary
public class SimulatedSafetyInputMonitor : ISafetyInputMonitor
{
    private readonly ConcurrentDictionary<string, bool> _safetyInputStates = new();

    public void UpdateState(string key, bool value)
    {
        // 线程安全的更新操作
        _safetyInputStates[key] = value;
    }

    public bool GetState(string key)
    {
        // 线程安全的读取操作
        return _safetyInputStates.GetValueOrDefault(key, true);
    }
}

// ✅ 正确：使用 ImmutableList 实现原子替换
public class ChuteTransmitterDriver
{
    private ImmutableList<ChuteTransmitterBinding> _bindings = ImmutableList<ChuteTransmitterBinding>.Empty;

    public void RegisterBindings(IEnumerable<ChuteTransmitterBinding> bindings)
    {
        // 原子替换整个列表，无需锁
        _bindings = bindings?.ToImmutableList() ?? ImmutableList<ChuteTransmitterBinding>.Empty;
    }

    public IReadOnlyList<ChuteTransmitterBinding> GetRegisteredBindings()
    {
        // 直接返回不可变集合，线程安全
        return _bindings;
    }
}

// ❌ 错误：使用普通 Dictionary 在多线程场景
public class UnsafeMonitor
{
    private readonly Dictionary<string, bool> _states = new(); // 不安全！

    public void UpdateState(string key, bool value)
    {
        _states[key] = value; // 可能导致并发异常
    }
}
```

#### 策略 2：使用异步锁 SemaphoreSlim（次优选择）

当无法使用线程安全集合时（例如需要多步骤原子操作），使用异步锁 `SemaphoreSlim` 而不是 `lock`。

**适用场景：**
- 需要跨 await 边界的锁保护
- 复杂的多步骤原子操作
- 异步方法中的状态保护

**最佳实践：**
```csharp
// ✅ 正确：使用 SemaphoreSlim 支持异步锁
public class LineSafetyOrchestrator
{
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private volatile LineRunState _currentState;

    public async Task<bool> RequestStartAsync(CancellationToken ct = default)
    {
        await _stateLock.WaitAsync(ct);
        try
        {
            if (_currentState != LineRunState.Idle)
            {
                return false;
            }
            _currentState = LineRunState.Starting;
            // ... 其他逻辑
            return true;
        }
        finally
        {
            _stateLock.Release();
        }
    }
}

// ❌ 错误：在异步方法中使用 lock
public async Task<bool> ProcessAsync()
{
    lock (_lock) // 不能跨 await 边界
    {
        await SomeAsyncOperationAsync(); // 编译错误或运行时问题
    }
}
```

#### 策略 3：使用 lock（仅作为最后手段）

**仅在以下情况使用 lock：**
- 纯同步代码，没有异步操作
- 极短的临界区（微秒级）
- 性能敏感且无法使用线程安全集合的场景

**最佳实践：**
```csharp
// ⚠️ 仅在必要时使用：专用锁对象保护纯同步操作
public class LegacyService
{
    private readonly object _lock = new();
    private int _counter;

    public void IncrementCounter() // 纯同步方法
    {
        lock (_lock)
        {
            _counter++; // 极短的临界区
        }
    }
}
```

**lock 使用规范：**
1. **使用专用锁对象**：`private readonly object _lock = new();`
2. **锁粒度最小**：锁住的代码块尽量小
3. **避免嵌套锁**：防止死锁
4. **避免锁 `this`**：使用专用锁对象
5. **纯同步代码**：不能包含 await

#### 策略 4：只读集合（初始化后不变）

对于在构造函数中初始化后不再修改的集合，使用只读集合：

**适用场景：**
- 配置数据
- 映射关系
- 初始化后不变的数据结构

**示例：**

```csharp
// ✅ 正确：构造后只读，线程安全
public class SimulationChuteIoService
{
    private readonly Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)> _chuteMapping;
    private readonly List<IChuteIoEndpoint> _endpoints;

    public SimulationChuteIoService(
        IEnumerable<IChuteIoEndpoint> endpoints,
        Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)> chuteMapping)
    {
        _endpoints = endpoints?.ToList() ?? throw new ArgumentNullException(nameof(endpoints));
        _chuteMapping = chuteMapping ?? throw new ArgumentNullException(nameof(chuteMapping));
        // 之后不再修改这些集合
    }

    public async ValueTask OpenAsync(long chuteId, CancellationToken ct = default)
    {
        // 只读访问，线程安全
        if (!_chuteMapping.TryGetValue(chuteId, out var mapping))
        {
            return;
        }
        await mapping.endpoint.SetChannelAsync(mapping.channelIndex, true, ct);
    }
}
```

### 并发场景识别

需要并发保护的典型场景：

1. **后台任务访问的状态**
   - 定时轮询任务
   - 事件处理循环
   - 异步回调

2. **多个服务共享的注册表/字典**
   - 连接管理
   - 状态缓存
   - 订阅管理

3. **Orchestrator/Manager/Registry 类型**
   - 系统状态管理
   - 资源池
   - 协调服务

4. **事件处理器**
   - 多个事件源同时触发
   - 事件处理的共享状态

---

## SafetyIsolator 异常隔离

### 原则

所有与外部资源交互的操作必须通过异常隔离机制保护，确保异常不会导致进程崩溃。

### SafetyIsolator 使用指南

#### 何时使用 SafetyIsolator

**必须使用的场景：**

1. **网络通讯**
   - TCP/UDP 连接
   - HTTP 请求
   - WebSocket 通讯
   - 上游系统调用

2. **硬件驱动操作**
   - 串口通讯 (Serial Port)
   - Modbus 读写
   - IO 板操作
   - 设备状态读取

3. **数据库访问**
   - LiteDB 操作
   - SQL 查询
   - 配置加载/保存

4. **文件 IO**
   - 文件读写
   - 目录操作
   - 配置文件解析

5. **序列化/反序列化**
   - JSON 序列化
   - XML 解析
   - BSON 映射

**SafetyIsolator API：**

```csharp
public class SafetyIsolator
{
    // 同步操作
    public bool Execute(Action action, string operationName, string? context = null);
    public T Execute<T>(Func<T> func, T defaultValue, string operationName, string? context = null);

    // 异步操作
    public Task<bool> ExecuteAsync(Func<Task> asyncAction, string operationName, string? context = null);
    public Task<T> ExecuteAsync<T>(Func<Task<T>> asyncFunc, T defaultValue, string operationName, string? context = null);
}
```

#### 使用示例

**示例 1：网络连接**

```csharp
public class ZhiQian32RelayClient
{
    private readonly SafetyIsolator _safetyIsolator;
    private readonly ILogger _logger;

    public async Task<bool> ConnectAsync(CancellationToken ct)
    {
        return await _safetyIsolator.ExecuteAsync(
            async () =>
            {
                var client = new TcpClient();
                await client.ConnectAsync(_ipAddress, _port, ct);
                _tcpClient = client;
                _stream = client.GetStream();
                return true;
            },
            defaultValue: false,
            operationName: "智嵌继电器TCP连接",
            context: $"IP={_ipAddress}, Port={_port}");
    }
}
```

**示例 2：数据库操作**

```csharp
public class LiteDbStore
{
    private readonly SafetyIsolator _safetyIsolator;

    public async Task<T?> LoadAsync<T>(string key) where T : class
    {
        return await _safetyIsolator.ExecuteAsync(
            async () =>
            {
                return await Task.Run(() =>
                {
                    var collection = _database.GetCollection<ConfigEntry>("config");
                    var entry = collection.FindById(key);
                    return entry != null ? BsonMapper.Global.ToObject<T>(entry.Data) : null;
                });
            },
            defaultValue: null,
            operationName: "LiteDB配置加载",
            context: $"Key={key}");
    }
}
```

**示例 3：文件 IO**

```csharp
public class FileRecordingManager
{
    private readonly SafetyIsolator _safetyIsolator;

    public async Task SaveSessionMetadataAsync(SessionInfo session)
    {
        await _safetyIsolator.ExecuteAsync(
            async () =>
            {
                var json = JsonSerializer.Serialize(session);
                await File.WriteAllTextAsync(_sessionFile, json);
            },
            operationName: "会话元数据保存",
            context: $"SessionId={session.SessionId}");
    }
}
```

**示例 4：硬件 IO**

```csharp
public class FieldBusClient
{
    private readonly SafetyIsolator _safetyIsolator;

    public async Task<bool> WriteSingleCoilAsync(int address, bool value)
    {
        return await _safetyIsolator.ExecuteAsync(
            async () =>
            {
                // Modbus 写线圈操作
                await _modbusClient.WriteSingleCoilAsync((ushort)address, value);
                return true;
            },
            defaultValue: false,
            operationName: "Modbus写单个线圈",
            context: $"Address={address}, Value={value}");
    }
}
```

### 异常处理最佳实践

#### 1. 日志记录

SafetyIsolator 自动记录异常日志，包含：
- 操作名称
- 上下文参数
- 完整异常堆栈

**无需在调用点再次记录相同日志。**

```csharp
// ✅ 正确：SafetyIsolator 已记录日志
var success = await _safetyIsolator.ExecuteAsync(
    async () => await _client.ConnectAsync(),
    "连接客户端",
    $"Address={_address}");

if (!success)
{
    // 仅记录业务层面的失败处理逻辑
    _logger.LogWarning("连接失败，将进入重连流程");
}

// ❌ 错误：重复记录
var success = await _safetyIsolator.ExecuteAsync(
    async () => await _client.ConnectAsync(),
    "连接客户端");

if (!success)
{
    _logger.LogError("连接失败"); // 重复记录
}
```

#### 2. 返回安全值

始终为 SafetyIsolator 提供合理的默认值：

```csharp
// ✅ 正确：失败返回 false
var connected = await _safetyIsolator.ExecuteAsync(
    async () => await ConnectInternalAsync(),
    defaultValue: false,
    "建立连接");

// ✅ 正确：失败返回空集合
var items = await _safetyIsolator.ExecuteAsync(
    async () => await FetchItemsAsync(),
    defaultValue: Array.Empty<Item>(),
    "获取数据");

// ✅ 正确：失败返回 null
var config = await _safetyIsolator.ExecuteAsync(
    async () => await LoadConfigAsync(),
    defaultValue: null,
    "加载配置");
```

#### 3. 不传播异常

SafetyIsolator 会捕获并记录异常，不会向上抛出。调用方应检查返回值而不是捕获异常：

```csharp
// ✅ 正确：检查返回值
var success = await _safetyIsolator.ExecuteAsync(
    async () => await ProcessAsync(),
    "处理数据");

if (!success)
{
    // 处理失败情况
    await HandleFailureAsync();
}

// ❌ 错误：尝试捕获异常（SafetyIsolator 已吞掉异常）
try
{
    await _safetyIsolator.ExecuteAsync(
        async () => await ProcessAsync(),
        "处理数据");
}
catch (Exception ex) // 永远不会执行
{
    _logger.LogError(ex, "处理失败");
}
```

---

## 实施指南

### 新代码要求

1. **共享状态必须线程安全**
   - 使用 ConcurrentDictionary / ConcurrentQueue 等线程安全集合
   - 或使用 lock 保护所有访问路径

2. **外部资源访问必须异常隔离**
   - 所有网络、硬件、文件、数据库操作使用 SafetyIsolator
   - 提供合理的默认值/失败返回值

3. **不崩溃原则**
   - 任何异常只记录，不导致进程终止
   - 除非人为明确终止

### 现有代码审查

审查清单：

1. **识别并发访问点**
   - [ ] 后台任务/定时器访问的字段
   - [ ] 事件处理器中的共享状态
   - [ ] Manager/Orchestrator 中的集合

2. **验证线程安全**
   - [ ] 使用 ConcurrentDictionary 等线程安全集合？
   - [ ] 或所有访问路径有 lock 保护？
   - [ ] 锁对象是专用的 `object` 类型？

3. **检查外部调用**
   - [ ] 网络操作是否有异常保护？
   - [ ] 硬件 IO 是否有异常保护？
   - [ ] 数据库访问是否有异常保护？
   - [ ] 文件操作是否有异常保护？

---

## 验证清单

### PR 提交前自检

- [ ] 所有多线程访问的集合使用了线程安全类型或锁保护
- [ ] 网络、硬件、数据库、文件 IO 使用 SafetyIsolator 或等效异常保护
- [ ] 异常处理返回安全值，不会导致空引用或未定义行为
- [ ] 没有嵌套锁或交叉锁风险
- [ ] 锁粒度合理，没有长时间持有锁
- [ ] 构建成功
- [ ] 所有测试通过

### 代码审查要点

审查者应检查：

1. **并发安全**
   - 共享状态是否线程安全
   - 锁使用是否正确
   - 是否有竞态条件

2. **异常隔离**
   - 外部调用是否有保护
   - 异常是否被正确处理
   - 是否会导致崩溃

3. **日志质量**
   - 异常日志是否包含足够上下文
   - 是否有重复日志
   - 日志级别是否合理

---

## 参考

- [Copilot 强制约束规则](../../.github/copilot-instructions.md) - 第 12、13 节
- [ARCHITECTURE_RULES.md](../../ARCHITECTURE_RULES.md) - 异常处理与线程安全规则
- [PERMANENT_CONSTRAINTS.md](../../PERMANENT_CONSTRAINTS.md) - 永久约束

---

**版本**：v1.0  
**最后更新**：2025-11-21  
**维护者**：ZakYip Team
