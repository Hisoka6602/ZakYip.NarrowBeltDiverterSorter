# 架构硬性规则 (Architecture Hard Rules)

本文档定义了项目必须遵守的架构硬性规则。所有贡献者在开发过程中必须遵循这些规则，违反规则的代码将不会被接受。

## 目录

- [Host 层规则](#host-层规则)
- [依赖注入规则](#依赖注入规则)
- [时间使用规则](#时间使用规则)
- [异常处理规则](#异常处理规则)
- [线程安全规则](#线程安全规则)
- [语言特性规则](#语言特性规则)
- [文档更新规则](#文档更新规则)
- [检查清单](#检查清单)

---

## Host 层规则

### 规则 1: Host 层禁止实现业务逻辑

**要求：** Host 层只负责应用程序启动、依赖注入（DI）配置和模块组合，不得包含任何业务逻辑。

**原因：** 保持 Host 层的简洁性和可维护性，业务逻辑应该在 Core、Execution、Ingress 等业务层实现。

**正确示例：**

```csharp
// ✅ Host 层：只做 DI 配置
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // 注册服务
        builder.Services.AddSingleton<IParcelSorter, WheelDiverterSorter>();
        builder.Services.AddSingleton<IDiverterDriver, PlcDiverterDriver>();
        
        var app = builder.Build();
        app.Run();
    }
}
```

**错误示例：**

```csharp
// ❌ Host 层：包含业务逻辑
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // ❌ 错误：在 Host 层实现包裹路由逻辑
        builder.Services.AddSingleton<IParcelRouter>(sp =>
        {
            var router = new ParcelRouter();
            router.AddRule(new RoutingRule { /* ... */ });
            return router;
        });
    }
}
```

---

## 依赖注入规则

### 规则 1.5: 构造函数依赖必须在DI容器中注册

**要求：** 任何已经注册到依赖注入容器的服务类型，当其实现类的构造函数新增任何依赖时，都必须同步在 DI 容器中注册对应的依赖服务。

**原因：** 
1. 防止应用在启动阶段因无法解析依赖而崩溃
2. 确保所有服务依赖关系完整且可验证
3. 避免"Some services are not able to be constructed"错误

**强约束：**
- 对于 `Core.Abstractions` 和 `Core.Configuration` 命名空间下的接口，它们都是通过 DI 提供的服务契约
- 一旦在某个实现类中被作为构造函数参数使用，就必须有明确的实现，并被注册到 DI 容器
- 不允许"写了构造函数依赖，但希望默认构造、可选依赖、或者后面再补"的情况
- 已经被注册为 Singleton 的服务，其构造函数中新增的依赖，生命周期必须是可被 Singleton 安全依赖的类型（例如 Singleton/Transient）

**正确示例：**

```csharp
// ✅ 在 Host/Program.cs 中正确注册所有依赖

// 1. 注册小车环配置提供器（ICartAtChuteResolver 的依赖）
builder.Services.AddSingleton<ICartRingConfigurationProvider, CartRingConfigurationProvider>();

// 2. 注册格口配置提供器（ICartAtChuteResolver 的另一个依赖）
builder.Services.AddSingleton<IChuteConfigProvider, RepositoryBackedChuteConfigProvider>();

// 3. 注册小车位置跟踪器（ICartAtChuteResolver 的依赖）
builder.Services.AddSingleton<ICartPositionTracker, CartPositionTracker>();

// 4. 注册格口小车号计算器（ICartAtChuteResolver 的依赖）
builder.Services.AddSingleton<IChuteCartNumberCalculator, ChuteCartNumberCalculator>();

// 5. 最后注册 ICartAtChuteResolver（所有依赖已就绪）
builder.Services.AddSingleton<ICartAtChuteResolver, CartAtChuteResolver>();
```

**错误示例：**

```csharp
// ❌ 错误：注册服务但遗漏其依赖

// 注册 ICartAtChuteResolver，但没有注册它依赖的 ICartRingConfigurationProvider
builder.Services.AddSingleton<ICartAtChuteResolver, CartAtChuteResolver>();
// 结果：启动时抛出 "Unable to resolve service for type 'ICartRingConfigurationProvider' 
//       while attempting to activate 'CartAtChuteResolver'"
```

**自检清单：**

修改实现类构造函数并新增构造参数时，必须：
1. 检查该实现类是否已经通过 DI 注册为某个接口的实现
2. 确认新增依赖在 DI 容器中已经有注册
3. 如果没有注册，则本次改动必须同时增加对应的 DI 注册
4. 确保 Host 可以正常启动，不出现"Some services are not able to be constructed"错误
5. 运行依赖注入验证测试（见 `E2ETests/DependencyInjectionValidationTests.cs`）

**验证方法：**

1. 运行 DI 验证测试：
```bash
dotnet test --filter "FullyQualifiedName~DependencyInjectionValidationTests"
```

2. 启用 DI 构建时验证（在测试中）：
```csharp
var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,  // 启用构建时验证
    ValidateScopes = true    // 启用作用域验证
});
```

---

## 时间使用规则

### 规则 2: 必须使用本地时间提供器

**要求：** 全局禁止直接使用 `DateTime.UtcNow`，必须通过本地时间提供器（如 `ILocalTimeProvider`）获取时间。

**原因：** 
1. 便于单元测试时模拟时间
2. 统一时间源，避免分散的时间获取方式
3. 支持时间回放和录制功能

**正确示例：**

```csharp
// ✅ 使用时间提供器
public class ParcelTracker
{
    private readonly ILocalTimeProvider _timeProvider;
    
    public ParcelTracker(ILocalTimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void TrackParcel(long parcelId)
    {
        var now = _timeProvider.Now;  // ✅
        // 业务逻辑...
    }
}
```

**错误示例：**

```csharp
// ❌ 直接使用 DateTime
public class ParcelTracker
{
    public void TrackParcel(long parcelId)
    {
        var now = DateTime.Now;  // ❌ 禁止
        var utcNow = DateTime.UtcNow;  // ❌ 禁止
        // 业务逻辑...
    }
}
```

---

## 异常处理规则

### 规则 3: 外部调用必须使用安全隔离器

**要求：** 所有可能抛出异常的外部调用（如硬件驱动、网络请求、文件 IO）必须通过安全隔离器（如 `ISafetyIsolator`）进行隔离。

**原因：** 
1. 统一异常处理和日志记录
2. 防止未捕获的异常导致应用崩溃
3. 提供统一的异常恢复策略

**正确示例：**

```csharp
// ✅ 使用安全隔离器
public class PlcDriver
{
    private readonly ISafetyIsolator _isolator;
    
    public async Task<bool> WriteAsync(int address, int value)
    {
        return await _isolator.ExecuteAsync(
            async () =>
            {
                // 调用可能抛异常的 PLC 写入操作
                await _plcClient.WriteAsync(address, value);
                return true;
            },
            onError: ex => _logger.LogError(ex, "PLC 写入失败"),
            defaultValue: false
        );
    }
}
```

**错误示例：**

```csharp
// ❌ 直接调用外部接口
public class PlcDriver
{
    public async Task<bool> WriteAsync(int address, int value)
    {
        // ❌ 没有异常隔离，可能导致应用崩溃
        await _plcClient.WriteAsync(address, value);
        return true;
    }
}
```

---

## 线程安全规则

### 规则 4: 多线程共享集合必须使用线程安全类型

**要求：** 在多线程环境中共享的集合必须使用线程安全类型（如 `ConcurrentDictionary`、`ImmutableList`）或适当的锁机制。

**原因：** 避免并发访问导致的数据竞争和状态不一致。

**正确示例：**

```csharp
// ✅ 使用线程安全集合
public class CartTracker
{
    private readonly ConcurrentDictionary<long, CartInfo> _carts = new();
    
    public void UpdateCart(long cartId, CartInfo info)
    {
        _carts.AddOrUpdate(cartId, info, (_, _) => info);
    }
}

// ✅ 使用不可变集合
public record RoutingRules(ImmutableList<RoutingRule> Rules);
```

**错误示例：**

```csharp
// ❌ 使用非线程安全集合
public class CartTracker
{
    private readonly Dictionary<long, CartInfo> _carts = new();  // ❌ 不安全
    
    public void UpdateCart(long cartId, CartInfo info)
    {
        _carts[cartId] = info;  // ❌ 可能导致竞争条件
    }
}
```

---

## 语言特性规则

### 规则 5: DTO 和事件载荷使用 record

**要求：** DTO（数据传输对象）和事件载荷必须使用 `record` 或 `record struct`。

**原因：** 
1. 值语义更适合数据传递
2. 内置不可变性支持
3. 自动生成 `Equals` 和 `GetHashCode`

**正确示例：**

```csharp
// ✅ 使用 record
public record ParcelDto(long ParcelId, string Destination, DateTime ArrivalTime);

public record struct SensorTriggeredEventArgs(long SensorId, DateTime Timestamp);
```

**错误示例：**

```csharp
// ❌ 使用 class
public class ParcelDto
{
    public long ParcelId { get; set; }
    public string Destination { get; set; }
    public DateTime ArrivalTime { get; set; }
}
```

### 规则 6: 必填属性使用 required + init

**要求：** 对于必填的属性，使用 `required` 关键字和 `init` 访问器。

**正确示例：**

```csharp
// ✅ 使用 required + init
public record ParcelInfo
{
    public required long ParcelId { get; init; }
    public required string Destination { get; init; }
    public DateTime? ProcessedTime { get; init; }  // 可选字段
}
```

### 规则 7: 事件载荷命名必须以 EventArgs 结尾

**要求：** 所有事件载荷的类型名称必须以 `EventArgs` 结尾。

**正确示例：**

```csharp
// ✅ 命名正确
public record struct ParcelArrivedEventArgs(long ParcelId, DateTime Timestamp);
public record struct SortCompletedEventArgs(long CartId, int ChuteId);
```

**错误示例：**

```csharp
// ❌ 命名错误
public record struct ParcelArrived(long ParcelId, DateTime Timestamp);
public record struct SortCompleted(long CartId, int ChuteId);
```

### 规则 8: 性能关键结构使用 readonly struct

**要求：** 对于性能关键且不需要修改的值类型，使用 `readonly struct`。

**正确示例：**

```csharp
// ✅ 使用 readonly struct
public readonly struct Position
{
    public readonly int X;
    public readonly int Y;
    
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }
}
```

---

## 文档更新规则

### 规则 9: 新功能必须同步更新文档

**要求：** 添加新功能或进行重大变更时，必须同步更新以下相关文档：

1. **README.md** - 如果影响项目结构或使用方式
2. **docs/** 目录下的相应文档 - 如果涉及具体功能设计
3. **CONTRIBUTING.md** - 如果引入新的开发规范
4. **API 文档** - 如果添加或修改公共 API

**检查项：**

- [ ] 功能描述已更新
- [ ] 使用示例已添加
- [ ] 配置说明已补充
- [ ] 架构图已更新（如有变化）

---

## 检查清单

在提交 PR 前，请确保以下所有项都已检查：

### Host 层检查

- [ ] Host 层代码只包含 DI 配置和启动逻辑
- [ ] 没有业务逻辑在 Host 层实现
- [ ] 所有服务注册都使用依赖注入

### 时间使用检查

- [ ] 没有直接使用 `DateTime.Now` 或 `DateTime.UtcNow`
- [ ] 所有时间获取都通过 `ILocalTimeProvider` 或类似的时间提供器
- [ ] 测试代码中正确模拟了时间提供器

### 异常处理检查

- [ ] 所有外部调用（硬件、网络、文件 IO）都使用了安全隔离器
- [ ] 异常日志记录完整且信息充足
- [ ] 异常处理不会导致应用崩溃

### 线程安全检查

- [ ] 多线程共享的集合使用了线程安全类型
- [ ] 没有不安全的并发访问
- [ ] 正确使用了锁机制（如需要）

### 语言特性检查

- [ ] DTO 和事件载荷使用了 `record` 或 `record struct`
- [ ] 必填属性使用了 `required` + `init`
- [ ] 事件载荷命名以 `EventArgs` 结尾
- [ ] 性能关键的值类型使用了 `readonly struct`（如适用）

### 文档更新检查

- [ ] README.md 已更新（如果功能影响项目整体）
- [ ] docs/ 目录下的相应文档已更新
- [ ] 新增功能有使用说明和示例
- [ ] 架构图已更新（如有架构变化）

---

## 违规处理

违反以上任何一条硬性规则的代码将被要求修改。PR 审查时会严格检查这些规则的遵守情况。

如果您认为某条规则在特定情况下不适用，请在 PR 描述中明确说明例外原因，并获得维护者的批准。

---

## 参考文档

- [CONTRIBUTING.md](CONTRIBUTING.md) - 完整的贡献指南
- [docs/architecture/Layering.md](docs/architecture/Layering.md) - 分层架构详细说明
- [docs/architecture/Dependencies.md](docs/architecture/Dependencies.md) - 依赖关系管理

---

**最后更新日期：** 2024-11-19  
**维护者：** ZakYip Team
