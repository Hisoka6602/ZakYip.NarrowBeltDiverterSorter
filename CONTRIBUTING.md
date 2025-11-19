# 贡献指南

本文档定义了针对 GitHub Copilot 的全局编码约定，所有贡献者在使用 Copilot 生成或编辑代码时应遵循这些规范。

> **⚠️ 重要：** 在开始贡献之前，请务必阅读 [ARCHITECTURE_RULES.md](ARCHITECTURE_RULES.md)，了解项目的硬性架构规则。这些规则是强制性的，违反规则的代码将不会被接受。

## 快速链接

- [架构硬性规则](ARCHITECTURE_RULES.md) - **必读**：Host 层规则、时间使用、异常处理等硬性要求
- [文档导航](README.md#文档导航-documentation-navigation) - 完整的项目文档索引

## 语言与注释

### 注释语言
- **所有代码注释必须使用中文**
- 包括类注释、方法注释、字段注释、属性注释等
- XML 文档注释也应使用中文

### 事件载荷
- 事件载荷必须使用 `record struct` 或 `record class`
- 命名必须以 `EventArgs` 结尾
- 示例：
  ```csharp
  /// <summary>
  /// 分拣完成事件参数
  /// </summary>
  public record struct SortCompletedEventArgs(long CartId, int ChuteId);
  
  /// <summary>
  /// 包裹到达事件参数
  /// </summary>
  public record class ParcelArrivedEventArgs(long ParcelId, DateTime Timestamp);
  ```

### 枚举类型
- **所有 enum 必须添加 `Description` 特性**
- 每个枚举值都应包含中文注释
- 示例：
  ```csharp
  /// <summary>
  /// 分拣状态
  /// </summary>
  public enum SortStatus
  {
      /// <summary>
      /// 等待中
      /// </summary>
      [Description("等待中")]
      Pending = 0,
      
      /// <summary>
      /// 处理中
      /// </summary>
      [Description("处理中")]
      Processing = 1,
      
      /// <summary>
      /// 已完成
      /// </summary>
      [Description("已完成")]
      Completed = 2,
      
      /// <summary>
      /// 已失败
      /// </summary>
      [Description("已失败")]
      Failed = 3
  }
  ```

## 命名规范

### 布尔属性
- **所有布尔属性必须使用以下前缀之一：**
  - `Is` - 表示状态或属性（如 `IsEnabled`、`IsActive`）
  - `Has` - 表示拥有或包含（如 `HasChildren`、`HasError`）
  - `Can` - 表示能力或权限（如 `CanExecute`、`CanWrite`）
  - `Should` - 表示建议或推荐（如 `ShouldRetry`、`ShouldValidate`）

- 示例：
  ```csharp
  public bool IsRunning { get; set; }
  public bool HasItems { get; set; }
  public bool CanSort { get; set; }
  public bool ShouldNotify { get; set; }
  ```

### ID 类型约定
- **`ChuteId` 一律使用 `long` 类型**
- **`CartId` 一律使用 `long` 类型**
- 其他 ID 字段也应优先使用 `long` 类型，除非有特殊需求
- 示例：
  ```csharp
  public long ChuteId { get; init; }
  public long CartId { get; init; }
  public long ParcelId { get; init; }
  ```

### 领域模型和接口命名
- 命名风格应对齐 `ParcelSorter` / `WheelDiverterSorter` 的命名风格
- 使用清晰、具体的业务术语
- 接口使用 `I` 前缀
- 示例：
  ```csharp
  public interface IParcelSorter { }
  public class WheelDiverterSorter : IParcelSorter { }
  public interface IDiverterDriver { }
  public class BeltDiverter { }
  ```

## 技术偏好

### .NET 版本和特性
- **使用 .NET 8**
- **尽量使用新特性**：
  - 优先使用 `record` 和 `record struct`
  - 使用模式匹配（pattern matching）
  - 使用 `init` 属性和 `required` 关键字
  - 使用文件范围命名空间（file-scoped namespace）
  - 使用原始字符串字面量（raw string literals）
  - 使用集合表达式（collection expressions）

- 示例：
  ```csharp
  namespace ZakYip.NarrowBeltDiverterSorter.Core;
  
  public record ParcelInfo(long ParcelId, string Destination)
  {
      public required SortStatus Status { get; init; }
  }
  
  public class SorterService
  {
      public string GetStatusDescription(SortStatus status) => status switch
      {
          SortStatus.Pending => "等待处理",
          SortStatus.Processing => "正在处理",
          SortStatus.Completed => "处理完成",
          SortStatus.Failed => "处理失败",
          _ => throw new ArgumentOutOfRangeException(nameof(status))
      };
  }
  ```

### LINQ 优先原则
- **能用 LINQ 时优先使用 LINQ**
- **必须确保不会退化到客户端执行**
  - 在使用 Entity Framework Core 等 ORM 时特别注意
  - 避免在查询中使用无法翻译为 SQL 的方法
  - 如有疑虑，检查生成的 SQL 或启用敏感数据日志

- 良好示例：
  ```csharp
  // 优先使用 LINQ
  var activeParcels = parcels
      .Where(p => p.IsActive)
      .OrderBy(p => p.Timestamp)
      .ToList();
  
  var groupedByChute = parcels
      .GroupBy(p => p.ChuteId)
      .Select(g => new { ChuteId = g.Key, Count = g.Count() });
  ```

- 避免示例（客户端执行）：
  ```csharp
  // ❌ 错误：ComplexCalculation 无法翻译为 SQL，导致客户端执行
  var result = await dbContext.Parcels
      .Where(p => ComplexCalculation(p))
      .ToListAsync();
  ```

### 性能优化特性
- **必要时使用性能相关特性**
- 对热路径（hot path）方法使用 `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
- 对性能关键的结构体使用 `readonly struct`
- 示例：
  ```csharp
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsValidChuteId(long chuteId)
  {
      return chuteId > 0 && chuteId <= 1000;
  }
  
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

## 架构结构

### 严格分层原则

项目必须遵循以下分层架构，确保依赖关系清晰：

#### 1. Core 层
- **职责**：核心领域模型、业务逻辑、接口定义
- **依赖**：不依赖任何硬件库或具体实现
- **不允许**：引用任何硬件驱动、IO 库、第三方设备 SDK
- 示例内容：
  - 领域实体（Entity）
  - 值对象（Value Object）
  - 领域服务接口（Domain Service Interface）
  - 领域事件（Domain Event）

#### 2. Execution/Drivers 层
- **职责**：硬件驱动集成、具体设备实现
- **依赖**：可以引用 Core 层和具体硬件驱动库
- **职责**：实现 Core 层定义的接口，对接具体硬件
- 示例内容：
  - PLC 驱动实现
  - 传感器驱动实现
  - 电机控制器实现

#### 3. Ingress 层
- **职责**：IO 事件到领域事件的映射
- **依赖**：引用 Core 层
- **只做**：将外部 IO 事件（如 PLC 信号、传感器输入）转换为领域事件
- **不做**：业务逻辑处理（业务逻辑应在 Core 层）
- 示例：
  ```csharp
  // Ingress 层：只做事件映射
  public class PlcEventMapper
  {
      /// <summary>
      /// 将 PLC 信号转换为包裹到达事件
      /// </summary>
      public ParcelArrivedEventArgs MapToParcelArrived(PlcSignal signal)
      {
          return new ParcelArrivedEventArgs(
              ParcelId: signal.ParcelId,
              Timestamp: signal.Timestamp
          );
      }
  }
  ```

#### 4. Host 层
- **职责**：应用程序启动、依赖注入（DI）配置、模块组合
- **依赖**：引用所有其他层
- **职责**：
  - 配置依赖注入容器
  - 注册服务和实现
  - 配置中间件和管道
  - 应用程序生命周期管理
- 示例：
  ```csharp
  // Host 层：组合与 DI
  public class Program
  {
      public static void Main(string[] args)
      {
          var builder = WebApplication.CreateBuilder(args);
          
          // 注册 Core 服务
          builder.Services.AddSingleton<IParcelSorter, WheelDiverterSorter>();
          
          // 注册 Driver 实现
          builder.Services.AddSingleton<IDiverterDriver, PlcDiverterDriver>();
          
          // 注册 Ingress 映射器
          builder.Services.AddSingleton<PlcEventMapper>();
          
          var app = builder.Build();
          app.Run();
      }
  }
  ```

### 依赖关系图

```
Host
 ├─> Core (领域模型、接口)
 ├─> Execution/Drivers (硬件驱动实现)
 └─> Ingress (事件映射)

Execution/Drivers
 └─> Core (实现 Core 的接口)

Ingress
 └─> Core (生成领域事件)

Core
 └─> (无外部依赖)
```

## 示例代码结构

完整示例展示如何应用这些约定：

```csharp
namespace ZakYip.NarrowBeltDiverterSorter.Core;

/// <summary>
/// 分拣器状态
/// </summary>
public enum SorterState
{
    /// <summary>
    /// 空闲
    /// </summary>
    [Description("空闲")]
    Idle = 0,
    
    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running = 1,
    
    /// <summary>
    /// 已暂停
    /// </summary>
    [Description("已暂停")]
    Paused = 2
}

/// <summary>
/// 包裹到达事件参数
/// </summary>
public record struct ParcelArrivedEventArgs(
    long ParcelId,
    long ChuteId,
    DateTime Timestamp);

/// <summary>
/// 分拣器接口
/// </summary>
public interface IParcelSorter
{
    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 是否有错误
    /// </summary>
    bool HasError { get; }
    
    /// <summary>
    /// 能否处理包裹
    /// </summary>
    bool CanProcess { get; }
    
    /// <summary>
    /// 滑槽 ID
    /// </summary>
    long ChuteId { get; }
    
    /// <summary>
    /// 小车 ID
    /// </summary>
    long CartId { get; }
    
    /// <summary>
    /// 分拣包裹
    /// </summary>
    Task<bool> SortParcelAsync(long parcelId);
}

/// <summary>
/// 窄带转向轮分拣器
/// </summary>
public class NarrowBeltDiverterSorter : IParcelSorter
{
    public bool IsRunning { get; private set; }
    public bool HasError { get; private set; }
    public bool CanProcess => IsRunning && !HasError;
    public long ChuteId { get; init; }
    public long CartId { get; init; }
    
    /// <summary>
    /// 分拣包裹
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> SortParcelAsync(long parcelId)
    {
        if (!CanProcess)
        {
            return false;
        }
        
        // 分拣逻辑
        await Task.Delay(100);
        return true;
    }
    
    /// <summary>
    /// 获取当前状态描述
    /// </summary>
    public string GetStatusDescription(SorterState state) => state switch
    {
        SorterState.Idle => "分拣器空闲",
        SorterState.Running => "分拣器运行中",
        SorterState.Paused => "分拣器已暂停",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };
}
```

## 总结

遵循这些约定可以确保代码库的一致性、可维护性和高质量。在使用 GitHub Copilot 时，请将这些约定作为提示的一部分，以生成符合项目标准的代码。

---

如有疑问或需要补充，请提交 Issue 或 Pull Request。
