# ZakYip.NarrowBeltDiverterSorter.Shared

## 概述

Shared 层是整个解决方案的通用工具库，提供跨层使用的工具类、扩展方法和通用模式。

## 设计原则

1. **单向依赖**: Shared 只依赖 .NET BCL，不依赖任何业务层
2. **纯工具性**: 只包含通用工具，不包含业务逻辑
3. **无状态**: 所有方法应该是纯函数或扩展方法
4. **完整文档**: 所有公共 API 必须有中文 XML 文档注释
5. **性能意识**: 注释中说明性能特性（复杂度、内存分配等）

## 目录结构

```
Shared/
├── Kernel/              # 核心模式和基础类型
│   └── OperationResult.cs
├── Extensions/          # 扩展方法（集合、字符串等）
├── Math/                # 数学和物理量换算
│   └── PhysicsConversionExtensions.cs
└── Time/                # 时间相关工具
```

## 当前内容

### Kernel (核心)

#### OperationResult
通用操作结果模式，用于封装操作的成功/失败状态及错误消息。

```csharp
using ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

// 成功结果
var success = OperationResult.Success();

// 失败结果
var failure = OperationResult.Failure("错误消息");

// 使用
if (result.IsSuccess)
{
    // 处理成功情况
}
else
{
    Logger.LogError(result.ErrorMessage);
}
```

### Math (数学)

#### PhysicsConversionExtensions
通用物理量单位换算扩展方法。

```csharp
using ZakYip.NarrowBeltDiverterSorter.Shared.Math;

// 速度单位换算
decimal speedMmps = 1500m;  // 毫米每秒
decimal speedMps = speedMmps.ToMps();  // 转换为米每秒: 1.5 m/s
decimal speedCmps = speedMmps.ToCmps(); // 转换为厘米每秒: 150 cm/s
```

**注意**: 设备特定的换算（如包含设备参数的 Hz ↔ mm/s）应保留在对应的业务层（如 `Execution.Vendors.Rema.RemaScaling`）。

## 添加新工具的指南

### 何时添加到 Shared

✅ **应该添加的**:
- 通用的数据结构和模式（如 Result, Option, Guard）
- 通用的扩展方法（如字符串、集合操作）
- 纯数学/物理换算（不包含业务参数）
- 时间/周期计算工具
- 通用验证逻辑

❌ **不应该添加的**:
- 包含业务逻辑的代码
- 依赖具体业务实体的工具
- 设备特定的参数和配置
- 特定于某一层的帮助类

### 添加步骤

1. 确定合适的命名空间（Kernel / Extensions / Math / Time）
2. 创建文件并实现功能
3. 添加完整的中文 XML 文档注释
4. 在注释中说明性能特性
5. 确保不引入任何外部依赖
6. 运行构建确保 0 警告

### 代码示例模板

```csharp
namespace ZakYip.NarrowBeltDiverterSorter.Shared.Extensions;

/// <summary>
/// 字符串扩展方法
/// </summary>
/// <remarks>
/// 提供常用的字符串操作扩展方法，所有方法为纯函数，线程安全。
/// </remarks>
public static class StringExtensions
{
    /// <summary>
    /// 判断字符串是否为空或仅包含空白字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果字符串为 null、空或仅包含空白字符，返回 true；否则返回 false</returns>
    /// <remarks>
    /// 性能：O(n)，其中 n 是字符串长度。无额外内存分配。
    /// </remarks>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
}
```

## 依赖关系

### Shared 的依赖
- ✅ 仅依赖 .NET 8 BCL

### 依赖 Shared 的项目
- ✅ Core
- ✅ Execution
- ✅ Ingress
- ✅ Communication
- ✅ Observability
- ✅ Infrastructure
- ✅ Host

## 验证清单

在提交涉及 Shared 的更改前，确保：

- [ ] Shared 项目仍然只依赖 BCL
- [ ] 没有引入业务逻辑或业务实体引用
- [ ] 所有公共方法都有完整的中文 XML 注释
- [ ] 注释中包含性能特性说明
- [ ] 解决方案构建 0 警告（Debug + Release）
- [ ] 通过 CodeQL 安全检查

## 参考

- [Architecture Baseline Summary](../../ARCHITECTURE_BASELINE_SUMMARY.md)
- [Domain Purification Summary](../../DOMAIN_PURIFICATION_SUMMARY_CN.md)
