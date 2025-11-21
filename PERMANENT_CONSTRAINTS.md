# 永久约束规则 (Permanent Constraint Rules)

本文档定义了项目中必须永久遵守的约束规则，任何违反这些规则的代码都必须在PR中被修复。

## 1. 依赖注入规则

### 1.1 Host 层控制器依赖限制

**规则**: Host 层的所有 Controller 构造函数**禁止**直接依赖：
- `Infrastructure.*` 命名空间下的**具体类型**（concrete types）
- 任意具体的存储实现（例如 `LiteDb*`、某个具体 `HttpClient` 封装等）

**允许**: Controller 只能依赖：
- Application 层的应用服务接口
- `Core.Abstractions` / `Core.Configuration` 中的领域契约接口

**过渡期特例**: 
- 在接口尚未迁移到 Core 层之前，允许依赖 `Infrastructure` 命名空间中的**接口**（interface），但：
  - 必须在代码注释中标记为"待迁移"
  - 必须有对应的 issue 追踪接口迁移工作
  - 不允许依赖 Infrastructure 的具体类型
  - 示例：`IAppConfigurationStore`（标记为待迁移到 Core.Abstractions）

**示例**:
```csharp
// ❌ 错误 - 直接依赖具体实现
public class ChuteIoConfigurationController : ControllerBase
{
    private readonly LiteDbSorterConfigurationStore _configStore; // 具体类型
    
    public ChuteIoConfigurationController(
        LiteDbSorterConfigurationStore configStore) // Infrastructure 具体类型
    {
        _configStore = configStore;
    }
}

// ✅ 正确 - 依赖抽象接口
public class ChuteIoConfigurationController : ControllerBase
{
    private readonly IChuteTransmitterConfigurationPort _configPort; // Core 抽象接口
    
    public ChuteIoConfigurationController(
        IChuteTransmitterConfigurationPort configPort) // Core.Abstractions 接口
    {
        _configPort = configPort;
    }
}
```

### 1.2 新增依赖必须注册

**规则**: 对所有 Host Controller：
- 构造函数里出现的每一个参数类型**必须**保证在 DI 容器里有明确注册
- 若是接口，则必须在 Host 的服务注册扩展（通常在 `Program.cs`）中能找到对应实现
- 若是具体类型，视为设计问题，需要改造为依赖抽象

**验证**: 任何出现以下异常的PR都视为不合法：
```
Unable to resolve service for type 'XXX' while attempting to activate 'SomeController'
```

### 1.3 DI 验证测试

**规则**: 所有关键 Controller 必须有对应的 DI 验证测试，确保：
- Controller 能够成功从 DI 容器解析
- 所有依赖都正确注册
- 测试位于 `Tests/ZakYip.NarrowBeltDiverterSorter.E2ETests/DependencyInjectionValidationTests.cs`

## 2. 线程安全规则

**规则**: 所有的锁（locks）、数组（arrays）、集合（collections）都**必须**使用线程安全声明。

**禁止**:
```csharp
// ❌ 不安全的 lock
private readonly object _lock = new();
private Dictionary<string, int> _data = new();

lock (_lock)
{
    // Dictionary.Add 可能抛出异常
    if (!_data.ContainsKey(key))
    {
        _data.Add(key, value);
    }
}
```

**要求**:
```csharp
// ✅ 使用线程安全集合
private readonly ConcurrentDictionary<string, int> _data = new();

_data.TryAdd(key, value); // 线程安全，不会抛出异常
```

**线程安全集合类型**:
- `ConcurrentDictionary<TKey, TValue>`
- `ConcurrentQueue<T>`
- `ConcurrentStack<T>`
- `ConcurrentBag<T>`
- `ImmutableList<T>` / `ImmutableArray<T>` (对于不可变数据)
- `BlockingCollection<T>` (对于生产者-消费者模式)

## 3. 时间使用规则

**规则**: 时间**不能**使用 UTC 时间，**必须**使用本地时间。

**禁止**:
```csharp
// ❌ 错误 - UTC 时间
var now = DateTime.UtcNow;
var timestamp = DateTimeOffset.UtcNow;
var universal = someTime.ToUniversalTime();
```

**要求**:
```csharp
// ✅ 正确 - 本地时间
var now = DateTime.Now;
var timestamp = DateTimeOffset.Now;
// 或使用注入的时间提供器
var now = _timeProvider.GetLocalTime();
```

**例外**: 仅在需要与外部系统（如 API、数据库）交互且该系统明确要求 UTC 时间时，可以在边界处进行转换。

## 4. 异常安全规则

**规则**: 所有有概率抛出异常的方法都**必须**使用安全隔离器（SafetyIsolator），保证程序任何地方的异常都只记录，不崩溃。

**SafetyIsolator 使用**:
```csharp
// ✅ 正确 - 使用安全隔离器
public class SomeService
{
    private readonly SafetyIsolator _safetyIsolator;
    
    public SomeService(SafetyIsolator safetyIsolator)
    {
        _safetyIsolator = safetyIsolator;
    }
    
    public async Task ProcessAsync()
    {
        // 同步方法
        var success = _safetyIsolator.Execute(
            () => RiskyOperation(),
            "ProcessAsync",
            "Processing data"
        );
        
        // 异步方法
        var result = await _safetyIsolator.ExecuteAsync(
            async () => await RiskyAsyncOperation(),
            defaultValue: null,
            "ProcessAsync",
            "Async processing"
        );
    }
}
```

**SafetyIsolator 已注册在 DI**:
```csharp
// Program.cs 中
builder.Services.AddSingleton<SafetyIsolator>();
```

## 5. 接口完整性规则

**规则**: 当 Controller 需要调用某个方法时：
1. 该方法**必须**在接口中声明
2. 不允许在 Controller 中将接口强制转换为具体类型来访问额外方法

**示例**:
```csharp
// ❌ 错误 - 强制类型转换
public class SomeController : ControllerBase
{
    private readonly LiteDbProvider _provider; // 具体类型
    
    public SomeController(IProvider provider)
    {
        // 强制转换以访问 UpdateAsync 方法
        _provider = (provider as LiteDbProvider) 
            ?? throw new ArgumentException("Must be LiteDbProvider");
    }
    
    public async Task Update()
    {
        await _provider.UpdateAsync(...); // UpdateAsync 不在接口中
    }
}

// ✅ 正确 - 在接口中声明所需方法
public interface IProvider
{
    Task<Data> GetDataAsync();
    Task UpdateAsync(Data data); // 添加到接口
}

public class SomeController : ControllerBase
{
    private readonly IProvider _provider; // 接口类型
    
    public SomeController(IProvider provider)
    {
        _provider = provider;
    }
    
    public async Task Update()
    {
        await _provider.UpdateAsync(...); // 通过接口调用
    }
}
```

## 6. 违规扫描

定期运行以下命令扫描违规：

```bash
# 扫描 UTC 时间使用
grep -r "DateTimeOffset\.UtcNow\|DateTime\.UtcNow" --include="*.cs" .

# 扫描不安全的 lock 使用
grep -r "private.*readonly.*object.*lock\|lock\s*(" --include="*.cs" .

# 扫描 Host Controller 的 Infrastructure 依赖
find Host/*/Controllers -name "*.cs" -exec grep -l "using.*Infrastructure\|LiteDb[A-Z]" {} \;
```

## 7. PR 审查清单

在提交 PR 前，确保：

- [ ] Host Controller 不直接依赖 Infrastructure 具体类型
- [ ] 所有新增的 Controller 依赖都已在 DI 中注册
- [ ] 添加了对应的 DI 验证测试
- [ ] 没有使用不安全的 lock（使用线程安全集合）
- [ ] 没有使用 UTC 时间（使用本地时间）
- [ ] 所有可能抛异常的方法都使用了 SafetyIsolator
- [ ] 接口包含 Controller 需要的所有方法（无强制类型转换）

## 8. 已知违规（待修复）

当前代码库中存在以下违规，需要在后续 PR 中逐步修复：

### 8.1 UTC 时间使用
- **总数**: ~323 处
- **优先级**: 中等
- **影响**: 可能导致时区相关的 bug

### 8.2 不安全的 lock 使用  
- **总数**: ~133 处
- **优先级**: 高
- **影响**: 潜在的并发问题和死锁风险
- **位置**: 主要在 Core, Execution, Infrastructure 层

### 8.3 Infrastructure 接口位置
- **问题**: `IAppConfigurationStore` 等接口位于 Infrastructure 命名空间
- **优先级**: 低
- **影响**: 架构不够清晰，但不影响运行时行为
- **建议**: 未来将这些接口迁移到 Core.Abstractions

## 9. 更新历史

- 2024-XX-XX: 初始版本，定义核心约束规则
- 修复了 `ChuteIoConfigurationController` 和 `UpstreamRoutingSettingsController` 的 DI 问题
- 扩展了 `IChuteTransmitterConfigurationPort` 和 `IUpstreamRoutingConfigProvider` 接口
