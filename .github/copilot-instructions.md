# GitHub Copilot 强制约束规则

本文档定义了 GitHub Copilot 在生成或修改代码时必须遵守的硬性规则。所有后续 PR（包括 Copilot 生成的代码）都必须严格遵守这些约束。

> **⚠️ 重要警告**：违反以下任何规则的代码将被拒绝。如果无法满足某条规则，必须在 PR 中明确说明原因并请求人工审核。

---

## 目录

1. [通讯与重试策略](#1-通讯与重试策略)
2. [API 设计与验证](#2-api-设计与验证)
3. [日志管理](#3-日志管理)
4. [架构分层](#4-架构分层)
5. [Host 层约束](#5-host-层约束)
6. [Execution 与 Drivers 分层](#6-execution-与-drivers-分层)
7. [文档维护](#7-文档维护)
8. [性能与资源](#8-性能与资源)
9. [仿真场景](#9-仿真场景)
10. [配置策略](#10-配置策略)
11. [时间使用规范](#11-时间使用规范)
12. [异常安全隔离](#12-异常安全隔离)
13. [并发安全](#13-并发安全)
14. [C# 语言特性](#14-c-语言特性)

---

## 1. 通讯与重试策略

### 规则 1.1：客户端连接失败重试

**要求**：
- 作为客户端连接上游时，连接失败必须采用退避重试
- 最大退避时间不超过 2 秒
- 重试次数为无限重试，除非热更新了连接参数
- 热更新连接参数后，使用新参数继续无限重试

**禁止**：
- ❌ 修改为有限次数重试
- ❌ 修改为不重试
- ❌ 修改退避时间超过 2 秒

**验证点**：
- [ ] 连接重试逻辑使用指数退避算法
- [ ] 最大退避时间 <= 2 秒
- [ ] 没有设置重试次数上限
- [ ] 支持热更新连接参数

### 规则 1.2：发送失败不重试

**要求**：
- 数据发送失败时，只记录日志，不进行重试
- 不允许新增"发送失败自动重试"的行为

**禁止**：
- ❌ 对发送失败实现自动重试
- ❌ 对发送失败实现队列缓冲重发

**验证点**：
- [ ] 发送失败仅记录错误日志
- [ ] 没有发送重试逻辑
- [ ] 没有发送队列缓冲

---

## 2. API 设计与验证

### 规则 2.1：API 端点合并

**要求**：
- 能合并在同一控制器的端点，尽量放在同一控制器下
- 避免过度碎片化的控制器

**验证点**：
- [ ] 相关功能的 API 在同一控制器中
- [ ] 控制器命名清晰反映职责范围

### 规则 2.2：参数验证特性

**要求**：
- 所有 API 入参必须通过特性（Attribute）标记进行验证
- 必填参数使用 `[Required]`
- 范围验证使用 `[Range]`
- 格式验证使用 `[RegularExpression]` 或自定义验证特性
- 不允许仅依赖手写 if 判断与抛异常来做参数校验

**禁止**：
- ❌ 仅用 if 语句做参数校验
- ❌ 手动抛出 ArgumentException 而不是使用验证特性

**正确示例**：
```csharp
public class UpdateConfigRequest
{
    [Required(ErrorMessage = "配置 ID 不能为空")]
    public required string ConfigId { get; init; }
    
    [Range(1, 1000, ErrorMessage = "数值必须在 1-1000 之间")]
    public required int Value { get; init; }
}

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    [HttpPost]
    public IActionResult Update([FromBody] UpdateConfigRequest request)
    {
        // 验证由特性自动完成，无需手动检查
        // ...
    }
}
```

**验证点**：
- [ ] API 请求类有验证特性
- [ ] 没有仅依赖 if 语句的参数校验
- [ ] ModelState.IsValid 被正确检查

---

## 3. 日志管理

### 规则 3.1：日志节流

**要求**：
- 相同内容的日志至少间隔 1 秒以上才允许再次记录
- 避免高频重复日志淹没有效信息

**验证点**：
- [ ] 使用节流日志记录器（ThrottledLogger）
- [ ] 高频日志点已配置节流间隔
- [ ] 节流间隔 >= 1 秒

### 规则 3.2：日志保留配置

**要求**：
- 必须在 `appsettings.json` 中提供日志保留天数配置项
- 默认保留最近 3 天日志
- 可通过配置调整保留上限

**验证点**：
- [ ] appsettings.json 中有日志保留配置
- [ ] 默认值为 3 天
- [ ] 日志清理逻辑使用此配置

---

## 4. 架构分层

### 规则 4.1：分层职责

项目采用严格分层架构，各层职责如下：

**Host 层**：
- 应用程序启动
- 依赖注入（DI）配置
- 路由映射
- 基础中间件配置
- ❌ 不包含业务逻辑
- ❌ 不包含设备控制逻辑
- ❌ 不定义复杂类型或持久化逻辑

**Execution 层**：
- 调度逻辑
- 小车控制逻辑
- 分拣执行逻辑
- 控制回路（如 PID）
- 通过抽象接口调用 Drivers / Infrastructure

**Drivers 层**：
- 具体厂商硬件驱动实现
- 硬件通讯协议实现
- 设备控制接口实现

**Core 层**：
- 领域模型与实体
- 业务逻辑接口
- 领域事件定义
- ❌ 不依赖任何硬件库
- ❌ 不依赖具体实现

**Infrastructure 层**：
- 数据持久化实现
- 配置存储实现
- 外部服务集成

### 规则 4.2：依赖方向

**允许的依赖关系**：
```
Host → Execution, Drivers, Infrastructure, Core
Execution → Core
Drivers → Core
Infrastructure → Core
Core → 无外部依赖
```

**禁止的依赖关系**：
- ❌ Core → Infrastructure
- ❌ Core → Drivers
- ❌ Core → Host
- ❌ Host 直接依赖具体硬件库

---

## 5. Host 层约束

### 规则 5.1：Host 层打薄原则

**要求**：
- Host 层尽量打薄，只负责组合与启动
- 不在 Host 中写业务逻辑
- 不在 Host 定义领域实体或复杂类型

**禁止**：
- ❌ Host 控制器中包含业务计算逻辑
- ❌ Host 中定义领域模型
- ❌ Host 中实现复杂算法

### 规则 5.2：Host 层依赖约束

**要求**：
- Host 层控制器禁止直接依赖 `Infrastructure.*` 命名空间下的具体类型
- 禁止直接依赖具体驱动类（某厂商专用实现）
- 必须依赖 Core/Application 层的抽象接口

**禁止**：
```csharp
// ❌ 错误：直接依赖 Infrastructure 具体类型
public class ConfigController : ControllerBase
{
    private readonly LiteDbSorterConfigurationStore _store;
    
    public ConfigController(LiteDbSorterConfigurationStore store)
    {
        _store = store;
    }
}
```

**正确**：
```csharp
// ✅ 正确：依赖 Core 层抽象接口
public class ConfigController : ControllerBase
{
    private readonly IConfigurationStore _store;
    
    public ConfigController(IConfigurationStore store)
    {
        _store = store;
    }
}
```

**验证点**：
- [ ] Host 控制器构造函数只依赖接口
- [ ] 没有直接依赖 Infrastructure.* 具体类型
- [ ] 没有直接依赖具体驱动实现

### 规则 5.3：DI 注册完整性

**要求**：
- 任何已注册到 DI 容器的服务，其构造函数新增依赖时必须同步在 DI 中注册该依赖
- 避免出现 "Unable to resolve service for type 'XXX'" 错误

**验证点**：
- [ ] 所有服务构造函数依赖都已在 DI 中注册
- [ ] 运行 DI 验证测试通过
- [ ] 应用能够正常启动

---

## 6. Execution 与 Drivers 分层

### 规则 6.1：Execution 层职责

**要求**：
- Execution 层承担核心执行逻辑
- 调度、小车逻辑、分拣逻辑、控制回路都在此层
- 通过抽象接口调用 Drivers 和 Infrastructure

**验证点**：
- [ ] 核心业务逻辑在 Execution 层
- [ ] 不直接依赖具体硬件驱动
- [ ] 使用接口与 Drivers 交互

### 规则 6.2：Drivers 层存在性

**要求**：
- 如不存在 Drivers 层，需要建立专门层来承载具体厂商硬件实现
- 让程序更容易对接同类能力的多厂商设备

**验证点**：
- [ ] Drivers 层存在
- [ ] 具体硬件实现在 Drivers 层
- [ ] 支持多厂商设备扩展

---

## 7. 文档维护

### 规则 7.1：文档目录分类

**要求**：
- 文档不要分散，使用目录分类
- 必须能从 README.md 导航到各类文档

**目录结构**：
```
docs/
├── Architecture/      # 系统架构、分层说明、拓扑图
├── Simulation/        # 仿真场景说明与运行方式
├── Operations/        # 部署与运维
└── Conventions/       # 编码规范、异常处理规范、日志规范
```

**验证点**：
- [ ] 文档按目录分类
- [ ] README.md 有文档导航
- [ ] 新文档放在正确目录

### 规则 7.2：README.md 必需内容

**要求**：
- README.md 必须包含以下内容：
  1. 项目简介与运行流程概述
  2. 系统拓扑图（上游通讯、Host、Execution、Drivers、小车/格口）
  3. 异常处理流程图（从异常发生 → 捕获 → 日志 → 降级/健康检查）
  4. 系统架构图/项目结构图（分层及各命名空间职责）
  5. 项目规范/约束章节，链接到规范文档

**验证点**：
- [ ] README.md 包含所有必需章节
- [ ] 拓扑图、流程图、架构图完整
- [ ] 链接到规范文档

---

## 8. 性能与资源

### 规则 8.1：性能优先

**要求**：
- 减少代码量和复杂度
- 提升执行性能
- 降低资源消耗
- 优先选择高性能实现

**禁止**：
- ❌ 不必要的对象分配
- ❌ 昂贵的反射操作（除非必要）
- ❌ 过度复杂的 LINQ 查询

**建议**：
- ✅ 使用对象池（ObjectPool）重用对象
- ✅ 使用 Span<T> 和 Memory<T> 减少分配
- ✅ 使用 ValueTask<T> 优化异步性能

**验证点**：
- [ ] 热路径代码已优化
- [ ] 避免不必要的内存分配
- [ ] 使用性能分析工具验证

---

## 9. 仿真场景

### 规则 9.1：复杂仿真场景

**要求**：
- 增加更复杂的仿真场景
- 至少支持 1000 包裹的全流程仿真
- 全流程：启动按钮 → IO 识别小车 → 包裹创建绑定 → 正确落格

**验证点**：
- [ ] 存在大规模仿真测试（>= 1000 包裹）
- [ ] 覆盖完整分拣流程
- [ ] 验证正确性和性能

### 规则 9.2：仿真验证新逻辑

**要求**：
- 对新逻辑和关键路径通过仿真进行验证
- 确保功能不退化

**验证点**：
- [ ] 新功能有对应仿真测试
- [ ] 关键路径有回归测试
- [ ] 仿真测试全部通过

---

## 10. 配置策略

### 规则 10.1：必须配置的 API 端点

**要求**：
- 所有必须配置都需要有 API 端点用于设置和读取
- 非必要不放在 `appsettings.json`
- `appsettings.json` 用作默认值而非唯一配置入口

**验证点**：
- [ ] 关键配置有 API 端点
- [ ] 支持运行时配置更新
- [ ] appsettings.json 仅作为默认值

---

## 11. 时间使用规范

### 规则 11.1：统一使用本地时间

**要求**：
- 所有时间统一使用本地时间，而不是 UTC 时间
- 日志、事件、数据库字段、配置更新时间等均使用本地时间
- 与外部系统对接需要 UTC 时仅在边界做转换

**禁止**：
```csharp
// ❌ 错误：使用 UTC 时间
var now = DateTime.UtcNow;
var timestamp = DateTimeOffset.UtcNow;
```

**正确**：
```csharp
// ✅ 正确：使用本地时间
var now = _timeProvider.Now;  // 通过时间提供器
// 或在边界处
var localNow = DateTime.Now;
```

**验证点**：
- [ ] 没有直接使用 DateTime.UtcNow
- [ ] 使用 ILocalTimeProvider 或 DateTime.Now
- [ ] UTC 转换仅在边界处理

---

## 12. 异常安全隔离

### 规则 12.1：安全隔离器使用

**要求**：
- 所有有概率异常的方法必须使用安全隔离器
- 捕获异常，记录日志
- 返回安全的结果/状态，不让异常冒泡导致进程崩溃

**禁止**：
```csharp
// ❌ 错误：未处理的异常可能导致崩溃
public async Task ProcessAsync()
{
    await _hardware.WriteAsync(data); // 可能抛异常
}
```

**正确**：
```csharp
// ✅ 正确：使用安全隔离器
public async Task ProcessAsync()
{
    var success = await _isolator.ExecuteAsync(
        async () => await _hardware.WriteAsync(data),
        onError: ex => _logger.LogError(ex, "写入失败"),
        defaultValue: false
    );
}
```

**验证点**：
- [ ] 外部调用（硬件、网络、文件 IO）使用安全隔离器
- [ ] 异常被捕获并记录
- [ ] 不会导致应用崩溃

### 规则 12.2：整体异常安全

**要求**：
- 程序任何地方的异常都只记录，不崩溃
- 除非人为明确要终止进程

**验证点**：
- [ ] 顶层有全局异常处理
- [ ] 关键方法有异常保护
- [ ] 异常不会导致未处理崩溃

---

## 13. 并发安全

### 规则 13.1：线程安全集合

**要求**：
- 所有存在并发访问的数组、集合、字典必须使用线程安全声明
- 使用 `ConcurrentDictionary`、`ConcurrentQueue` 等线程安全类型
- 如必须使用锁，需保证安全使用，不导致死锁

**禁止**：
```csharp
// ❌ 错误：非线程安全集合
private readonly Dictionary<long, Data> _cache = new();

public void Update(long key, Data value)
{
    _cache[key] = value; // 并发访问不安全
}
```

**正确**：
```csharp
// ✅ 正确：线程安全集合
private readonly ConcurrentDictionary<long, Data> _cache = new();

public void Update(long key, Data value)
{
    _cache.AddOrUpdate(key, value, (_, _) => value);
}
```

**验证点**：
- [ ] 多线程共享集合使用线程安全类型
- [ ] 没有不安全的并发访问
- [ ] 锁使用正确（如需要）

---

## 14. C# 语言特性

### 规则 14.1：对象构造

**要求**：
- 使用 `required` + `init` 确保关键属性在创建时被设置
- 避免半初始化对象

**正确**：
```csharp
public class Config
{
    public required string Name { get; init; }
    public required int Value { get; init; }
}

// 使用时
var config = new Config 
{ 
    Name = "test", 
    Value = 100 
};
```

**验证点**：
- [ ] 必填属性使用 required
- [ ] 属性使用 init 访问器

### 规则 14.2：可空引用类型

**要求**：
- 启用 nullable
- 严肃处理空引用相关警告

**验证点**：
- [ ] 项目启用 nullable
- [ ] 没有空引用警告

### 规则 14.3：文件作用域类型

**要求**：
- 工具类和内部辅助类型使用文件作用域
- 避免污染全局命名空间

**验证点**：
- [ ] 内部类型使用 file 关键字
- [ ] 公共 API 使用 public

### 规则 14.4：record 优先

**要求**：
- DTO 与不可变数据优先使用 `record`

**验证点**：
- [ ] DTO 使用 record
- [ ] 事件载荷使用 record struct

### 规则 14.5：方法职责单一

**要求**：
- 一个方法只负责一个职责
- 尽量保持短小

**验证点**：
- [ ] 方法行数合理（< 50 行为佳）
- [ ] 方法职责单一清晰

### 规则 14.6：readonly struct

**要求**：
- 不需要可变性时优先使用 `readonly struct`
- 提升安全与性能

**验证点**：
- [ ] 值类型使用 readonly struct
- [ ] 不可变语义正确

---

## PR 提交前自检清单

在提交 PR 前，请确认以下所有项：

### 通讯与重试
- [ ] 连接失败重试策略未被破坏（客户端无限重试，最大退避 2 秒）
- [ ] 发送失败不重试，仅记录日志

### API 与验证
- [ ] 新增/修改的 API 端点均使用特性标记做参数验证
- [ ] API 端点合理合并，避免碎片化

### Host 层
- [ ] 未在 Host 层直接依赖 Infrastructure.* 或具体驱动实现
- [ ] Host 层只包含 DI 配置和启动逻辑

### 时间使用
- [ ] 所有新增时间字段/逻辑均使用本地时间，而非 UTC
- [ ] 使用 ILocalTimeProvider 或等价机制

### 异常处理
- [ ] 异常通过安全隔离器处理
- [ ] 不会导致未处理异常崩溃

### 并发安全
- [ ] 并发场景使用线程安全集合或合理锁控制

### C# 特性
- [ ] 新增 DTO/不可变数据使用 record
- [ ] 对象使用 required + init
- [ ] 事件载荷命名以 EventArgs 结尾

### 文档
- [ ] 文档按目录分类
- [ ] README 中有导航入口

### 测试与验证
- [ ] 构建通过
- [ ] 所有测试通过
- [ ] DI 验证测试通过

---

## 违规处理

若 PR 中无法满足任一规则：
1. 在 PR 描述中明确写明原因
2. 说明为何需要例外
3. 请求人工确认和批准

**未经批准的违规代码将被拒绝。**

---

## 参考文档

- [docs/Conventions/架构硬性规则.md](../docs/Conventions/架构硬性规则.md) - 架构硬性规则详细说明
- [docs/Conventions/永久约束规则.md](../docs/Conventions/永久约束规则.md) - 永久约束规则
- [CONTRIBUTING.md](../CONTRIBUTING.md) - 贡献指南
- [docs/Conventions/项目规则集.md](../docs/Conventions/项目规则集.md) - 项目规则详细文档

---

**版本**：v1.0  
**最后更新**：2025-11-21  
**维护者**：ZakYip Team
