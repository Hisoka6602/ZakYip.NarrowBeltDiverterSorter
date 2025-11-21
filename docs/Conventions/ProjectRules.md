# 项目规则集 (Project Rules)

本文档是 ZakYip.NarrowBeltDiverterSorter 项目的完整规则集，涵盖架构、编码、通讯、日志、时间、异常处理、并发安全等所有方面的硬性约束。

> **📌 强制性文档**：本文档定义的所有规则都是强制性的，所有贡献者和 GitHub Copilot 在生成代码时必须遵守。

---

## 文档导航

- [GitHub Copilot 强制约束](../../.github/copilot-instructions.md) - Copilot 必须遵守的规则
- [架构硬性规则](../../ARCHITECTURE_RULES.md) - 架构分层与依赖规则
- [永久约束规则](../../PERMANENT_CONSTRAINTS.md) - DI、时间、异常、并发等约束
- [贡献指南](../../CONTRIBUTING.md) - 编码规范与命名约定

---

## 1. 通讯与重试策略

### 1.1 客户端连接重试

**硬性规则**：
- 作为客户端连接上游时，连接失败**必须**采用指数退避重试
- 最大退避时间**不得超过 2 秒**
- 重试次数为**无限重试**，除非热更新了连接参数
- 热更新连接参数后，使用新参数继续无限重试

**禁止行为**：
- ❌ 设置有限的重试次数
- ❌ 连接失败不重试
- ❌ 退避时间超过 2 秒

**验证方法**：
```csharp
// 检查连接重试实现
// 1. 查找连接重试逻辑
// 2. 确认使用指数退避算法
// 3. 确认最大退避时间 <= 2000ms
// 4. 确认没有重试次数限制
```

### 1.2 发送失败处理

**硬性规则**：
- 数据发送失败时，**只记录日志**，**不进行重试**
- 不允许新增"发送失败自动重试"的行为

**禁止行为**：
- ❌ 对发送失败实现自动重试
- ❌ 对发送失败实现队列缓冲重发
- ❌ 对发送失败实现任何形式的重传机制

**正确实现**：
```csharp
public async Task SendDataAsync(Data data)
{
    try
    {
        await _client.SendAsync(data);
    }
    catch (Exception ex)
    {
        // 只记录日志，不重试
        _logger.LogError(ex, "发送数据失败: {Data}", data);
        // ❌ 不要在这里重试
    }
}
```

---

## 2. API 设计与验证

### 2.1 API 端点合并原则

**硬性规则**：
- 相关功能的 API 端点**必须**放在同一控制器下
- 避免过度碎片化的控制器

**示例**：
```csharp
// ✅ 正确：相关配置端点在同一控制器
[ApiController]
[Route("api/[controller]")]
public class ChuteConfigController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetConfig() { }
    
    [HttpPost]
    public async Task<IActionResult> UpdateConfig() { }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConfig(long id) { }
}
```

### 2.2 参数验证特性

**硬性规则**：
- 所有 API 入参**必须**通过特性（Attribute）标记进行验证
- 必填参数使用 `[Required]`
- 范围验证使用 `[Range]`
- 格式验证使用 `[RegularExpression]` 或自定义验证特性
- **禁止**仅依赖手写 if 判断与抛异常来做参数校验

**正确示例**：
```csharp
public class CreateParcelRequest
{
    [Required(ErrorMessage = "包裹 ID 不能为空")]
    [Range(1, long.MaxValue, ErrorMessage = "包裹 ID 必须大于 0")]
    public required long ParcelId { get; init; }
    
    [Required(ErrorMessage = "目标格口不能为空")]
    [Range(1, 1000, ErrorMessage = "格口 ID 必须在 1-1000 之间")]
    public required long ChuteId { get; init; }
    
    [StringLength(50, ErrorMessage = "目的地名称不能超过 50 个字符")]
    public string? Destination { get; init; }
}
```

**错误示例**：
```csharp
// ❌ 错误：手写参数校验
[HttpPost]
public IActionResult CreateParcel([FromBody] CreateParcelRequest request)
{
    if (request.ParcelId <= 0)
        throw new ArgumentException("包裹 ID 必须大于 0");
    
    if (request.ChuteId < 1 || request.ChuteId > 1000)
        throw new ArgumentException("格口 ID 必须在 1-1000 之间");
    
    // ...
}
```

### 2.3 配置策略

**硬性规则**：
- 所有**必须配置**都需要有 API 端点用于设置和读取
- 非必要不放在 `appsettings.json`
- `appsettings.json` 用作**默认值**而非唯一配置入口

**原因**：
- 支持运行时动态配置更新
- 避免重启服务修改配置
- 便于集中管理和监控

---

## 3. 日志管理

### 3.1 日志节流

**硬性规则**：
- 相同内容的日志**至少间隔 1 秒以上**才允许再次记录
- 避免高频重复日志淹没有效信息

**实现方式**：
- 使用 `ThrottledLogger` 或等价节流机制
- 为高频日志点配置节流间隔

**示例**：
```csharp
// 使用节流日志记录器
private readonly ThrottledLogger _logger;

public void ProcessData()
{
    // 该日志即使被频繁调用，也只会每秒记录一次
    _logger.LogInformation("正在处理数据...", throttleInterval: TimeSpan.FromSeconds(1));
}
```

### 3.2 日志保留配置

**硬性规则**：
- **必须**在 `appsettings.json` 中提供日志保留天数配置项
- 默认保留最近 **3 天**日志
- 可通过配置调整保留上限

**配置示例**：
```json
{
  "Logging": {
    "RetentionDays": 3,
    "MaxRetentionDays": 30
  }
}
```

---

## 4. 架构分层

### 4.1 分层职责定义

项目采用严格分层架构，各层职责如下：

#### Host 层
**职责**：
- 应用程序启动与生命周期管理
- 依赖注入（DI）配置与服务注册
- 路由映射与 API 端点定义
- 基础中间件配置（日志、异常处理等）

**禁止**：
- ❌ 不包含业务逻辑
- ❌ 不包含设备控制逻辑
- ❌ 不定义复杂领域类型或持久化逻辑
- ❌ 不直接操作硬件或数据库

#### Execution 层
**职责**：
- 调度逻辑与任务管理
- 小车追踪与位置计算
- 分拣执行逻辑
- 控制回路（如 PID 速度控制）
- 通过抽象接口调用 Drivers / Infrastructure

**特点**：
- 包含核心业务逻辑
- 不依赖具体硬件实现
- 使用接口与底层交互

#### Drivers 层
**职责**：
- 具体厂商硬件驱动实现
- 硬件通讯协议实现（Modbus、串口等）
- 设备控制接口实现
- 传感器数据采集

**特点**：
- 实现 Core 层定义的接口
- 封装硬件细节
- 支持多厂商设备

#### Core 层
**职责**：
- 领域模型与实体定义
- 业务逻辑接口声明
- 领域事件定义
- 数据契约（DTO）定义

**约束**：
- ❌ **不依赖**任何硬件库
- ❌ **不依赖**具体实现
- ❌ **不依赖**Infrastructure、Drivers、Host 层
- ✅ 只定义抽象和契约

#### Infrastructure 层
**职责**：
- 数据持久化实现（LiteDB 等）
- 配置存储实现
- 外部服务集成
- 基础设施服务实现

### 4.2 依赖方向规则

**允许的依赖关系**：
```
Host → Execution, Drivers, Infrastructure, Core
Execution → Core
Drivers → Core
Infrastructure → Core
Core → (无外部依赖)
```

**禁止的依赖关系**：
- ❌ Core → Infrastructure
- ❌ Core → Drivers
- ❌ Core → Host
- ❌ Core → 任何具体实现
- ❌ Host 直接依赖具体硬件库

---

## 5. Host 层约束

### 5.1 Host 层打薄原则

**硬性规则**：
- Host 层**尽量打薄**，只负责组合与启动
- **不在 Host 中写业务逻辑**
- **不在 Host 定义领域实体或复杂类型**

**错误示例**：
```csharp
// ❌ 错误：在 Host 层实现业务逻辑
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // ❌ 错误：在 Main 方法中实现路由规则
        var router = new ParcelRouter();
        router.AddRule(new RoutingRule 
        { 
            From = "A", 
            To = "B",
            Priority = 1
        });
        
        builder.Services.AddSingleton(router);
    }
}
```

**正确示例**：
```csharp
// ✅ 正确：只做组合和配置
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // 只注册服务，业务逻辑在 Execution 层
        builder.Services.AddSingleton<IParcelRouter, ParcelRouter>();
        builder.Services.AddSingleton<IRoutingRuleProvider, RoutingRuleProvider>();
    }
}
```

### 5.2 Host 层依赖约束

**硬性规则**：
- Host 层控制器**禁止**直接依赖 `Infrastructure.*` 命名空间下的**具体类型**
- **禁止**直接依赖具体驱动类（某厂商专用实现）
- **必须**依赖 Core/Application 层的抽象接口

**禁止**：
```csharp
// ❌ 错误：直接依赖 Infrastructure 具体类型
public class ConfigController : ControllerBase
{
    private readonly LiteDbSorterConfigurationStore _store;
    private readonly PlcModbusDriver _plcDriver;
    
    public ConfigController(
        LiteDbSorterConfigurationStore store,  // 具体实现
        PlcModbusDriver plcDriver)             // 具体驱动
    {
        _store = store;
        _plcDriver = plcDriver;
    }
}
```

**正确**：
```csharp
// ✅ 正确：依赖 Core 层抽象接口
public class ConfigController : ControllerBase
{
    private readonly IConfigurationStore _store;
    private readonly IPlcDriver _plcDriver;
    
    public ConfigController(
        IConfigurationStore store,      // Core 接口
        IPlcDriver plcDriver)            // Core 接口
    {
        _store = store;
        _plcDriver = plcDriver;
    }
}
```

### 5.3 DI 注册完整性

**硬性规则**：
- 任何已注册到 DI 容器的服务，其构造函数新增依赖时**必须**同步在 DI 中注册该依赖
- 避免出现 "Unable to resolve service for type 'XXX'" 错误

**验证方法**：
1. 运行 DI 验证测试：
```bash
dotnet test --filter "FullyQualifiedName~DependencyInjectionValidationTests"
```

2. 启用 DI 构建时验证：
```csharp
var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,  // 启用构建时验证
    ValidateScopes = true    // 启用作用域验证
});
```

**检查清单**：
- [ ] 所有服务构造函数依赖都已在 DI 中注册
- [ ] DI 验证测试全部通过
- [ ] 应用能够正常启动，无服务解析错误

---

## 6. Execution 与 Drivers 分层

### 6.1 Execution 层职责

**硬性规则**：
- Execution 层承担**核心执行逻辑**
- 调度、小车逻辑、分拣逻辑、控制回路都在此层
- **通过抽象接口**调用 Drivers 和 Infrastructure
- **不直接依赖**具体硬件驱动

**示例**：
```csharp
// ✅ 正确：Execution 层通过接口调用
public class SorterExecutionService
{
    private readonly IMainLineDrive _mainDrive;  // 接口
    private readonly IDiverterDriver _diverter;  // 接口
    
    public SorterExecutionService(
        IMainLineDrive mainDrive,
        IDiverterDriver diverter)
    {
        _mainDrive = mainDrive;
        _diverter = diverter;
    }
    
    public async Task ExecuteSortAsync(Parcel parcel)
    {
        // 执行逻辑使用接口，不关心具体实现
        await _mainDrive.SetSpeedAsync(1500);
        await _diverter.TriggerAsync(parcel.ChuteId);
    }
}
```

### 6.2 Drivers 层存在性与扩展性

**硬性规则**：
- **必须存在** Drivers 层来承载具体厂商硬件实现
- 让程序更容易对接同类能力的多厂商设备
- 支持通过配置切换不同厂商实现

**多厂商支持示例**：
```csharp
// Core 层：定义接口
public interface IMainLineDrive
{
    Task SetSpeedAsync(int mmps);
    Task<int> GetSpeedAsync();
}

// Drivers 层：厂商 A 实现
public class VendorAMainLineDrive : IMainLineDrive
{
    // 厂商 A 的实现
}

// Drivers 层：厂商 B 实现
public class VendorBMainLineDrive : IMainLineDrive
{
    // 厂商 B 的实现
}

// Host 层：根据配置选择实现
if (config.MainLineVendor == "VendorA")
    builder.Services.AddSingleton<IMainLineDrive, VendorAMainLineDrive>();
else
    builder.Services.AddSingleton<IMainLineDrive, VendorBMainLineDrive>();
```

---

## 7. 文档维护

### 7.1 文档目录分类

**硬性规则**：
- 文档**不要分散**，使用目录分类
- **必须**能从 README.md 导航到各类文档

**目录结构**：
```
docs/
├── Architecture/         # 系统架构、分层说明、拓扑图
│   ├── Layering.md
│   ├── Dependencies.md
│   └── SORTING_SYSTEM.md
├── Simulation/          # 仿真场景说明与运行方式
│   └── SimulationGuide.md
├── Operations/          # 部署与运维
│   ├── BringUpGuide.md
│   ├── RemaLm1000HBringUpGuide.md
│   └── SAFETY_CONTROL.md
└── Conventions/         # 编码规范、异常处理规范、日志规范
    ├── ProjectRules.md  (本文档)
    └── CodingStandards.md
```

### 7.2 README.md 必需内容

**硬性规则**：
README.md **必须**包含以下内容：

1. **项目简介与运行流程概述**
2. **系统拓扑图**（上游通讯、Host、Execution、Drivers、小车/格口）
3. **异常处理流程图**（从异常发生 → 捕获 → 日志 → 降级/健康检查）
4. **系统架构图/项目结构图**（分层及各命名空间职责）
5. **项目规范/约束章节**，链接到规范文档

**验证清单**：
- [ ] README.md 包含所有必需章节
- [ ] 拓扑图完整准确
- [ ] 流程图清晰易懂
- [ ] 架构图反映当前结构
- [ ] 链接到所有规范文档

---

## 8. 性能与资源

### 8.1 性能优先原则

**硬性规则**：
- 减少代码量和复杂度
- 提升执行性能
- 降低资源消耗
- 优先选择高性能实现

**禁止行为**：
- ❌ 不必要的对象分配
- ❌ 昂贵的反射操作（除非必要）
- ❌ 过度复杂的 LINQ 查询
- ❌ 频繁的字符串拼接（应使用 StringBuilder）

**推荐做法**：
- ✅ 使用对象池（ObjectPool）重用对象
- ✅ 使用 Span<T> 和 Memory<T> 减少分配
- ✅ 使用 ValueTask<T> 优化异步性能
- ✅ 使用 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 优化热路径

**示例**：
```csharp
// ✅ 正确：使用 Span<T> 避免分配
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int CalculateChecksum(ReadOnlySpan<byte> data)
{
    int checksum = 0;
    foreach (var b in data)
    {
        checksum += b;
    }
    return checksum & 0xFF;
}

// ✅ 正确：使用对象池
private readonly ObjectPool<StringBuilder> _stringBuilderPool;

public string FormatMessage(params string[] parts)
{
    var sb = _stringBuilderPool.Get();
    try
    {
        foreach (var part in parts)
        {
            sb.Append(part);
        }
        return sb.ToString();
    }
    finally
    {
        sb.Clear();
        _stringBuilderPool.Return(sb);
    }
}
```

---

## 9. 仿真场景

### 9.1 复杂仿真场景

**硬性规则**：
- 增加更复杂的仿真场景
- **至少支持 1000 包裹**的全流程仿真
- 全流程：启动按钮 → IO 识别小车 → 包裹创建绑定 → 正确落格

**验证点**：
- [ ] 存在大规模仿真测试（>= 1000 包裹）
- [ ] 覆盖完整分拣流程
- [ ] 验证正确性和性能
- [ ] 仿真测试稳定可重复

### 9.2 仿真验证新逻辑

**硬性规则**：
- 对新逻辑和关键路径**通过仿真进行验证**
- 确保功能不退化

**验证点**：
- [ ] 新功能有对应仿真测试
- [ ] 关键路径有回归测试
- [ ] 仿真测试全部通过
- [ ] 性能符合预期

---

## 10. 时间使用规范

### 10.1 统一使用本地时间

**硬性规则**：
- 所有时间**统一使用本地时间**，而不是 UTC 时间
- 日志、事件、数据库字段、配置更新时间等**均使用本地时间**
- 与外部系统对接需要 UTC 时**仅在边界做转换**

**禁止**：
```csharp
// ❌ 错误：使用 UTC 时间
var now = DateTime.UtcNow;
var timestamp = DateTimeOffset.UtcNow;
var universal = someTime.ToUniversalTime();

// ❌ 错误：在业务逻辑中使用 UTC
public void LogEvent(string message)
{
    var timestamp = DateTime.UtcNow;  // 错误
    _logger.LogInformation("{Timestamp}: {Message}", timestamp, message);
}
```

**正确**：
```csharp
// ✅ 正确：使用本地时间提供器
public class EventLogger
{
    private readonly ILocalTimeProvider _timeProvider;
    
    public EventLogger(ILocalTimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void LogEvent(string message)
    {
        var timestamp = _timeProvider.Now;  // 本地时间
        _logger.LogInformation("{Timestamp}: {Message}", timestamp, message);
    }
}

// ✅ 正确：边界处转换 UTC
public class UpstreamApiClient
{
    public async Task SendEventAsync(Event evt)
    {
        // 业务内部使用本地时间
        var localTime = evt.Timestamp;
        
        // 仅在 API 边界转换为 UTC
        var request = new UpstreamRequest
        {
            EventId = evt.Id,
            TimestampUtc = localTime.ToUniversalTime()  // 边界转换
        };
        
        await _httpClient.PostAsync("api/events", request);
    }
}
```

**验证方法**：
```bash
# 扫描 UTC 时间使用
grep -r "DateTimeOffset\.UtcNow\|DateTime\.UtcNow\|ToUniversalTime" --include="*.cs" . \
  | grep -v "// 边界转换" \
  | grep -v "// UTC required"
```

---

## 11. 异常安全隔离

### 11.1 安全隔离器使用

**硬性规则**：
- 所有**有概率异常的方法**必须使用安全隔离器
- 捕获异常，记录日志
- 返回安全的结果/状态，**不让异常冒泡导致进程崩溃**

**禁止**：
```csharp
// ❌ 错误：未处理的异常可能导致崩溃
public async Task ProcessAsync()
{
    await _hardware.WriteAsync(data);  // 可能抛异常，未处理
}
```

**正确**：
```csharp
// ✅ 正确：使用安全隔离器
public class HardwareService
{
    private readonly ISafetyIsolator _isolator;
    private readonly ILogger _logger;
    
    public async Task<bool> ProcessAsync()
    {
        return await _isolator.ExecuteAsync(
            async () =>
            {
                await _hardware.WriteAsync(data);
                return true;
            },
            onError: ex => _logger.LogError(ex, "硬件写入失败"),
            defaultValue: false
        );
    }
}
```

### 11.2 整体异常安全

**硬性规则**：
- 程序**任何地方的异常**都只记录，**不崩溃**
- 除非人为明确要终止进程

**实现要求**：
- [ ] 顶层有全局异常处理
- [ ] 关键方法有异常保护
- [ ] 异常不会导致未处理崩溃
- [ ] 异常日志完整详细

**全局异常处理示例**：
```csharp
// Program.cs 中配置全局异常处理
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        // 记录异常日志
        logger.LogError(exception, "未处理的异常");
        
        // 返回友好错误响应，不崩溃
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = "服务器内部错误，请稍后重试",
            Timestamp = DateTime.Now
        });
    });
});
```

---

## 12. 并发安全

### 12.1 线程安全集合

**硬性规则**：
- 所有存在并发访问的数组、集合、字典**必须**使用线程安全声明
- 使用 `ConcurrentDictionary`、`ConcurrentQueue`、`ConcurrentBag` 等线程安全类型
- 如必须使用锁，需保证安全使用，不导致死锁

**禁止**：
```csharp
// ❌ 错误：非线程安全集合
public class CartTracker
{
    private readonly Dictionary<long, CartInfo> _carts = new();
    
    public void UpdateCart(long cartId, CartInfo info)
    {
        lock (_lock)
        {
            _carts[cartId] = info;  // 虽然加锁，但容易出错
        }
    }
    
    public IEnumerable<CartInfo> GetAllCarts()
    {
        // ❌ 错误：枚举时没有锁保护，可能抛异常
        return _carts.Values;
    }
}
```

**正确**：
```csharp
// ✅ 正确：线程安全集合
public class CartTracker
{
    private readonly ConcurrentDictionary<long, CartInfo> _carts = new();
    
    public void UpdateCart(long cartId, CartInfo info)
    {
        _carts.AddOrUpdate(cartId, info, (_, _) => info);
    }
    
    public IEnumerable<CartInfo> GetAllCarts()
    {
        // 线程安全，枚举时自动快照
        return _carts.Values.ToList();
    }
}
```

**线程安全集合类型**：
- `ConcurrentDictionary<TKey, TValue>` - 并发字典
- `ConcurrentQueue<T>` - 并发队列
- `ConcurrentStack<T>` - 并发栈
- `ConcurrentBag<T>` - 并发包
- `ImmutableList<T>` / `ImmutableArray<T>` - 不可变集合
- `BlockingCollection<T>` - 阻塞集合（生产者-消费者模式）

---

## 13. C# 语言特性

### 13.1 对象构造

**硬性规则**：
- 使用 `required` + `init` 确保关键属性在创建时被设置
- 避免半初始化对象

**正确示例**：
```csharp
public class ParcelInfo
{
    public required long ParcelId { get; init; }
    public required long ChuteId { get; init; }
    public required string Destination { get; init; }
    public DateTime? ProcessedAt { get; init; }  // 可选
}

// 使用时必须设置所有 required 属性
var parcel = new ParcelInfo
{
    ParcelId = 12345,
    ChuteId = 10,
    Destination = "北京"
    // ProcessedAt 可选，不设置也可以
};
```

### 13.2 可空引用类型

**硬性规则**：
- 启用 nullable
- 严肃处理空引用相关警告

**项目配置**：
```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### 13.3 文件作用域类型

**硬性规则**：
- 工具类和内部辅助类型使用文件作用域（`file` 关键字）
- 避免污染全局命名空间

**示例**：
```csharp
// 文件：StringHelper.cs

namespace ZakYip.NarrowBeltDiverterSorter.Core;

// ✅ 公共 API 使用 public
public class StringValidator
{
    public static bool IsValid(string value) => 
        Helper.CheckLength(value) && Helper.CheckFormat(value);
}

// ✅ 内部辅助类使用 file
file static class Helper
{
    public static bool CheckLength(string value) => 
        value?.Length <= 100;
    
    public static bool CheckFormat(string value) => 
        !string.IsNullOrWhiteSpace(value);
}
```

### 13.4 record 优先

**硬性规则**：
- DTO 与不可变数据**优先使用 `record`**
- 事件载荷使用 `record struct`

**示例**：
```csharp
// DTO 使用 record
public record ParcelDto(long ParcelId, string Destination, DateTime ArrivalTime);

// 事件载荷使用 record struct
public record struct ParcelArrivedEventArgs(long ParcelId, DateTime Timestamp);

// 配置类使用 record
public record ChuteConfig
{
    public required long ChuteId { get; init; }
    public required int WindowSize { get; init; }
}
```

### 13.5 方法职责单一

**硬性规则**：
- 一个方法只负责一个职责
- 尽量保持短小（< 50 行为佳）

**错误示例**：
```csharp
// ❌ 错误：方法过长，职责不单一
public async Task ProcessParcelAsync(Parcel parcel)
{
    // 验证
    if (parcel.Id <= 0) throw new ArgumentException();
    
    // 查询路由
    var rule = await _db.QueryAsync(...);
    
    // 计算位置
    var position = CalculatePosition(...);
    
    // 等待窗口
    await WaitForWindow(...);
    
    // 触发分拣
    await _driver.TriggerAsync(...);
    
    // 更新状态
    await _db.UpdateAsync(...);
    
    // 发送通知
    await _notifier.NotifyAsync(...);
    
    // ... 100+ 行代码
}
```

**正确示例**：
```csharp
// ✅ 正确：拆分为多个职责单一的方法
public async Task ProcessParcelAsync(Parcel parcel)
{
    ValidateParcel(parcel);
    
    var rule = await GetRoutingRuleAsync(parcel);
    var position = CalculateTargetPosition(parcel, rule);
    
    await WaitForSortingWindowAsync(position);
    await TriggerSortingAsync(parcel, rule.ChuteId);
    
    await UpdateParcelStatusAsync(parcel.Id, SortStatus.Completed);
    await NotifyCompletionAsync(parcel);
}
```

### 13.6 readonly struct

**硬性规则**：
- 不需要可变性时**优先使用 `readonly struct`**
- 提升安全与性能

**示例**：
```csharp
// ✅ 正确：只读结构体
public readonly struct Position
{
    public readonly int X;
    public readonly int Y;
    
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public Position Add(Position other) =>
        new Position(X + other.X, Y + other.Y);
}
```

---

## 14. PR 提交前自检清单

在提交 PR 前，请确认以下所有项：

### 通讯与重试
- [ ] 连接失败重试策略未被破坏（客户端无限重试，最大退避 2 秒）
- [ ] 发送失败不重试，仅记录日志
- [ ] 没有新增发送失败自动重试

### API 与验证
- [ ] 新增/修改的 API 端点均使用特性标记做参数验证
- [ ] API 端点合理合并，避免碎片化
- [ ] 必须配置有 API 端点

### 日志
- [ ] 高频日志使用节流（>= 1 秒间隔）
- [ ] 日志保留天数可配置

### Host 层
- [ ] Host 层只包含 DI 配置和启动逻辑
- [ ] 未在 Host 层直接依赖 Infrastructure.* 或具体驱动实现
- [ ] 所有构造函数依赖都已在 DI 中注册

### Execution/Drivers 分层
- [ ] Execution 层通过接口调用 Drivers
- [ ] 具体硬件实现在 Drivers 层

### 时间使用
- [ ] 所有时间使用本地时间，而非 UTC
- [ ] 使用 ILocalTimeProvider 或等价机制
- [ ] UTC 转换仅在边界处理

### 异常处理
- [ ] 外部调用使用安全隔离器
- [ ] 异常被捕获并记录
- [ ] 不会导致未处理异常崩溃

### 并发安全
- [ ] 并发场景使用线程安全集合
- [ ] 没有不安全的并发访问

### C# 特性
- [ ] DTO 使用 record
- [ ] 对象使用 required + init
- [ ] 事件载荷命名以 EventArgs 结尾
- [ ] 启用 nullable，处理空引用警告
- [ ] 方法职责单一，行数合理

### 文档
- [ ] 文档按目录分类
- [ ] README 中有导航入口
- [ ] 架构图/流程图已更新（如需要）

### 测试与验证
- [ ] 构建通过（dotnet build）
- [ ] 所有测试通过（dotnet test）
- [ ] DI 验证测试通过
- [ ] 新功能有仿真测试（如适用）

---

## 15. 违规处理

若 PR 中无法满足任一规则：
1. 在 PR 描述中明确写明原因
2. 说明为何需要例外
3. 请求人工确认和批准

**未经批准的违规代码将被拒绝。**

---

## 参考文档

- [GitHub Copilot 强制约束](../../.github/copilot-instructions.md)
- [架构硬性规则](../../ARCHITECTURE_RULES.md)
- [永久约束规则](../../PERMANENT_CONSTRAINTS.md)
- [贡献指南](../../CONTRIBUTING.md)
- [分层架构详细说明](../Architecture/Layering.md)
- [依赖关系管理](../Architecture/Dependencies.md)

---

**版本**：v1.0  
**最后更新**：2025-11-21  
**维护者**：ZakYip Team

**本文档是项目的强制性规范，所有贡献者必须遵守。**
