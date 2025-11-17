# LiteDB 配置存储重构文档

## 概述

本次重构将 LiteDB 的使用范围明确限定为**配置存储**，所有运行时日志、统计、历史记录改为使用 `.log` 文件（NLog/Serilog），避免 LiteDB 成为"时序库"。

## 架构变更

### Core 层 - 配置抽象

在 `ZakYip.NarrowBeltDiverterSorter.Core/Configuration/` 目录下定义了统一的配置访问抽象：

#### ISorterConfigurationStore 接口

```csharp
public interface ISorterConfigurationStore
{
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
```

**接口特点：**
- 仅用于系统运行所需的配置对象（拓扑、分拣模式、设备连接参数等）
- **不用于**日志、高频事件或统计数据
- 提供加载、保存和存在性检查功能

### Infrastructure 层 - LiteDB 实现

在 `ZakYip.NarrowBeltDiverterSorter.Infrastructure/LiteDb/` 目录下实现了配置存储：

#### LiteDbSorterConfigurationStore 实现

```csharp
public sealed class LiteDbSorterConfigurationStore : ISorterConfigurationStore, IDisposable
{
    private const string CollectionName = "config_entries";
    // ...
}
```

**实现特点：**
- 使用统一的 `config_entries` 集合存储所有配置
- 键值对结构：`{ Key: "配置键", Data: BsonDocument }`
- 支持任意配置对象的序列化/反序列化

## 配置类型

系统中使用 LiteDB 存储的配置包括：

1. **MainLineControlOptions** - 主线控制选项
   - 目标速度、加速度限制、稳定死区等
   
2. **InfeedLayoutOptions** - 入口布局选项
   - 入口到主线距离、时间容差、校准偏移等
   
3. **ChuteConfigSet** - 格口配置集
   - 格口数量、格口位置、强排口ID等
   
4. **UpstreamConnectionOptions** - 上游连接选项
   - API地址、超时配置、认证令牌等

## 使用方式

### 服务注册

在 Host 或 Simulation 项目中：

```csharp
// 注册配置存储
builder.Services.AddSingleton<ISorterConfigurationStore, LiteDbSorterConfigurationStore>();

// 注册配置仓储
builder.Services.AddSingleton<IMainLineOptionsRepository, LiteDbMainLineOptionsRepository>();
builder.Services.AddSingleton<IInfeedLayoutOptionsRepository, LiteDbInfeedLayoutOptionsRepository>();
builder.Services.AddSingleton<IChuteConfigRepository, LiteDbChuteConfigRepository>();
builder.Services.AddSingleton<IUpstreamConnectionOptionsRepository, LiteDbUpstreamConnectionOptionsRepository>();
```

### 配置访问

通过仓储接口访问配置：

```csharp
public class LiteDbMainLineOptionsRepository : IMainLineOptionsRepository
{
    private readonly ISorterConfigurationStore _configStore;
    
    public async Task<MainLineControlOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        var options = await _configStore.LoadAsync<MainLineControlOptions>("MainLineControlOptions", cancellationToken);
        if (options == null)
        {
            options = MainLineControlOptions.CreateDefault();
            await SaveAsync(options, cancellationToken);
        }
        return options;
    }
    
    // ...
}
```

## 日志记录

所有运行时事件、操作日志应使用标准日志框架：

```csharp
// 正确：使用 ILogger
_logger.LogInformation("包裹 {ParcelId} 已上车到小车 {CartId}", parcelId, cartId);

// 错误：不要写入 LiteDB
// await _database.GetCollection("events").Insert(new { ParcelId = parcelId, ... });
```

## 迁移指南

### 从旧接口迁移

如果您的代码使用了旧的 `IConfigStore` 或 `LiteDbConfigStore`：

**旧代码：**
```csharp
builder.Services.AddSingleton<IConfigStore, LiteDbConfigStore>();
```

**新代码：**
```csharp
builder.Services.AddSingleton<ISorterConfigurationStore, LiteDbSorterConfigurationStore>();
```

### 命名空间变更

- 旧命名空间：`ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration`
- 新命名空间（接口）：`ZakYip.NarrowBeltDiverterSorter.Core.Configuration`
- 新命名空间（实现）：`ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb`

## 验证标准

重构完成后，系统应满足以下条件：

1. ✅ **配置存储集中**：所有 LiteDB 使用都在 `LiteDbSorterConfigurationStore` 中
2. ✅ **无事件记录**：LiteDB 中不存在 "events"、"logs"、"history" 等集合
3. ✅ **统一集合名**：所有配置存储在 `config_entries` 集合中
4. ✅ **日志走文件**：运行时日志、统计数据写入 `.log` 文件
5. ✅ **测试通过**：所有单元测试和集成测试通过

## 文件结构

```
ZakYip.NarrowBeltDiverterSorter.Core/
└── Configuration/
    ├── ISorterConfigurationStore.cs          # 配置存储接口
    └── ConfigurationAccessException.cs       # 配置访问异常

ZakYip.NarrowBeltDiverterSorter.Infrastructure/
├── Configuration/
│   ├── IConfigStore.cs                       # [已过时] 旧接口
│   ├── LiteDbConfigStore.cs                  # [已过时] 旧实现
│   ├── IChuteConfigRepository.cs
│   ├── LiteDbChuteConfigRepository.cs
│   ├── IMainLineOptionsRepository.cs
│   ├── LiteDbMainLineOptionsRepository.cs
│   ├── IInfeedLayoutOptionsRepository.cs
│   ├── LiteDbInfeedLayoutOptionsRepository.cs
│   ├── IUpstreamConnectionOptionsRepository.cs
│   └── LiteDbUpstreamConnectionOptionsRepository.cs
└── LiteDb/
    └── LiteDbSorterConfigurationStore.cs     # 新的统一实现

ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests/
├── LiteDbConfigStoreTests.cs                 # [已过时] 旧测试
├── LiteDbSorterConfigurationStoreTests.cs    # 新测试
├── LiteDbChuteConfigRepositoryTests.cs
├── LiteDbMainLineOptionsRepositoryTests.cs
├── LiteDbInfeedLayoutOptionsRepositoryTests.cs
└── LiteDbUpstreamConnectionOptionsRepositoryTests.cs
```

## 注意事项

1. **向后兼容**：旧的 `IConfigStore` 和 `LiteDbConfigStore` 已标记为 `[Obsolete]`，但仍可使用
2. **性能考虑**：配置加载通常在启动时进行，性能影响可忽略
3. **并发安全**：LiteDB 使用共享连接模式，支持多线程访问
4. **数据迁移**：从旧格式到新格式无需迁移，因为底层存储结构兼容

## 相关文档

- [NarrowBeltDesign.md](./NarrowBeltDesign.md) - 系统整体设计
- [BringUpGuide.md](./BringUpGuide.md) - 系统启动指南

## 更新历史

- 2024-11-17：完成 LiteDB 配置存储重构，将接口移至 Core 层，实现移至 Infrastructure/LiteDb
