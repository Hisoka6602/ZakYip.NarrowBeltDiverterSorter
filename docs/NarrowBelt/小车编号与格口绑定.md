# 小车编号与格口窗口绑定系统

## 概述

本文档详细说明窄带分拣机中的小车编号与格口窗口映射机制，包括坐标体系、环形数组计算公式、典型配置示例以及调试方法。

---

## 一、术语定义

### 1.1 基本概念

- **TotalCartCount（总小车数量）**
  - 小车环上的总小车数量
  - 使用 1 基索引（范围：1 到 TotalCartCount）
  - 例如：TotalCartCount = 100，表示小车编号为 1 到 100

- **HeadCartNumber（首车编号）**
  - 当前位于"原点"位置的小车编号
  - 1 基索引（范围：1 到 TotalCartCount）
  - 原点是系统定义的参考位置，通常是传感器检测到的起始点
  - 首车编号随着小车环的移动而变化

- **CartNumberWhenHeadAtOrigin（格口基准小车号）**
  - 当首车编号为 1 且位于原点时，特定格口窗口下的小车编号
  - 1 基索引（范围：1 到 TotalCartCount）
  - 这是格口配置的关键参数，确定了格口与小车环的相对位置关系
  - 每个格口都有自己的 CartNumberWhenHeadAtOrigin 值

### 1.2 坐标体系

系统使用环形数组模型：

```
小车环：1 → 2 → 3 → ... → TotalCartCount → 1（循环）
         ↑
      原点位置
```

- 小车环是一个循环结构，最后一辆车之后是第一辆车
- 原点是固定的物理参考位置
- 首车编号表示当前在原点的小车
- 格口窗口是固定位置，但"看到"的小车号随着小车环移动而变化

---

## 二、环形数组计算公式

### 2.1 公式定义

给定：
- `TotalCartCount`：总小车数量
- `HeadCartNumber`：当前首车编号（1 基，范围 1..TotalCartCount）
- `CartNumberWhenHeadAtOrigin`：格口基准小车号（1 基，范围 1..TotalCartCount）

则当前格口窗口的小车编号为：

```
zeroBasedHead = HeadCartNumber - 1
zeroBasedChuteBase = CartNumberWhenHeadAtOrigin - 1
zeroBasedResult = (zeroBasedChuteBase + zeroBasedHead) mod TotalCartCount
CartNumber = zeroBasedResult + 1
```

### 2.2 公式解释

1. **转换为 0 基索引**：将 1 基索引转换为 0 基，便于模运算
2. **计算偏移量**：`zeroBasedChuteBase + zeroBasedHead` 表示格口基准位置加上首车移动的偏移
3. **环形回绕**：使用模运算 `mod TotalCartCount` 处理超过总车数的情况
4. **转换回 1 基索引**：加 1 得到最终的小车编号

### 2.3 为什么使用这个公式？

- **环形特性**：小车环是循环的，模运算自然处理环绕
- **相对位置**：格口看到的小车号 = 基准位置 + 首车移动的偏移量
- **一致性**：无论首车在哪里，公式都能正确计算格口当前的小车号

---

## 三、典型配置示例

### 3.1 示例配置

假设系统配置如下：
- `TotalCartCount = 100`（100 辆小车）
- 格口 1：`CartNumberWhenHeadAtOrigin = 90`
- 格口 3：`CartNumberWhenHeadAtOrigin = 80`

### 3.2 计算示例

#### 场景 1：首车在原点（HeadCartNumber = 1）

格口 1：
```
zeroBasedHead = 1 - 1 = 0
zeroBasedChuteBase = 90 - 1 = 89
zeroBasedResult = (89 + 0) mod 100 = 89
CartNumber = 89 + 1 = 90 ✓
```

格口 3：
```
zeroBasedHead = 0
zeroBasedChuteBase = 79
zeroBasedResult = (79 + 0) mod 100 = 79
CartNumber = 80 ✓
```

#### 场景 2：首车移动到 5 号位置（HeadCartNumber = 5）

格口 1：
```
zeroBasedHead = 5 - 1 = 4
zeroBasedChuteBase = 89
zeroBasedResult = (89 + 4) mod 100 = 93
CartNumber = 94 ✓
```

格口 3：
```
zeroBasedHead = 4
zeroBasedChuteBase = 79
zeroBasedResult = (79 + 4) mod 100 = 83
CartNumber = 84 ✓
```

#### 场景 3：首车接近末尾（HeadCartNumber = 11）

格口 1（演示环绕）：
```
zeroBasedHead = 11 - 1 = 10
zeroBasedChuteBase = 89
zeroBasedResult = (89 + 10) mod 100 = 99
CartNumber = 100 ✓（接近末尾）
```

#### 场景 4：首车在 12 号位置（HeadCartNumber = 12）

格口 1（演示环绕到起始）：
```
zeroBasedHead = 12 - 1 = 11
zeroBasedChuteBase = 89
zeroBasedResult = (89 + 11) mod 100 = 0
CartNumber = 1 ✓（环绕到第 1 辆车）
```

#### 场景 5：首车在 90 号位置（HeadCartNumber = 90）

格口 1：
```
zeroBasedHead = 90 - 1 = 89
zeroBasedChuteBase = 89
zeroBasedResult = (89 + 89) mod 100 = 78
CartNumber = 79 ✓
```

### 3.3 完整对照表

| HeadCartNumber | 格口 1 (基准=90) | 格口 3 (基准=80) |
|----------------|------------------|------------------|
| 1              | 90               | 80               |
| 2              | 91               | 81               |
| 5              | 94               | 84               |
| 10             | 99               | 89               |
| 11             | 100              | 90               |
| 12             | 1 (环绕)         | 91               |
| 20             | 9                | 99               |
| 21             | 10               | 100              |
| 22             | 11               | 1 (环绕)         |
| 50             | 39               | 29               |
| 90             | 79               | 69               |
| 100            | 89               | 79               |

---

## 四、包裹绑定流程

### 4.1 流程说明

当格口触发"包裹到达/创建"事件时，系统按以下步骤绑定小车号：

```
[格口触发事件] 
    ↓
[调用 IPackageCartBinder.BindCartForNewPackage]
    ↓
[调用 ICartAtChuteResolver.ResolveCurrentCartNumberForChute]
    ↓
[捕获快照 CaptureCartBindingSnapshot]
    ├─ 读取 TotalCartCount
    ├─ 读取 HeadCartNumber
    ├─ 读取 CartNumberWhenHeadAtOrigin
    └─ 验证所有参数有效性
    ↓
[调用 IChuteCartNumberCalculator.GetCartNumberAtChute]
    ↓
[计算环形数组公式]
    ↓
[返回小车号]
    ↓
[创建包裹并绑定 CartNumber]
    ↓
[触发 PackageBoundToCartEventArgs 事件]
```

### 4.2 关键设计原则

1. **快照机制**：所有配置和状态在同一时刻一次性读取，确保一致性
2. **统一入口**：所有包裹创建路径必须通过 `IPackageCartBinder`，禁止手工计算小车号
3. **错误处理**：任何配置错误或状态异常都会立即抛出明确的异常
4. **日志记录**：关键步骤都有详细日志，便于调试

### 4.3 代码示例

```csharp
// 错误做法：手工计算小车号
// ❌ 不要这样做
var headCart = _cartPositionTracker.CurrentOriginCartIndex.Value.Value + 1;
var cartNumber = (chuteConfig.CartNumberWhenHeadAtOrigin - 1 + headCart - 1) % totalCartCount + 1;

// 正确做法：使用统一接口
// ✓ 推荐做法
var cartNumber = _packageCartBinder.BindCartForNewPackage(packageId, chuteId);
```

---

## 五、调试与常见问题排查

### 5.1 常见错误与处理

#### 错误 1：小车总数量未完成学习或配置

**日志示例：**
```
[ERROR] 小车总数量未完成学习或配置，无法解析格口小车号。TotalCartCount=0, 格口ID=1, 场景=快照捕获
```

**原因：**
- `TotalCartCount` 未初始化或为 0
- 系统尚未学习到小车环的总数量

**排查步骤：**
1. 检查小车环学习功能是否运行
2. 确认配置中 `TotalCartCount` 是否正确设置
3. 查看自检服务 `ICartRingSelfCheckService` 的运行状态

**解决方法：**
- 等待系统完成小车环学习
- 或手动配置 `TotalCartCount`

#### 错误 2：当前首车状态未就绪

**日志示例：**
```
[ERROR] 当前首车状态未就绪，无法解析格口小车号。IsInitialized=false, CurrentOriginCartIndex=null, 格口ID=1, 场景=快照捕获
```

**原因：**
- 首车跟踪器 `ICartPositionTracker` 未初始化
- 系统尚未检测到首车通过原点

**排查步骤：**
1. 检查首车传感器是否正常
2. 确认首车跟踪服务是否运行
3. 查看原点传感器的信号

**解决方法：**
- 检查传感器连接
- 重启首车跟踪服务
- 手动触发首车检测

#### 错误 3：格口配置不存在

**日志示例：**
```
[ERROR] 格口 1 配置不存在。场景=快照捕获
```

**原因：**
- 格口配置未加载
- 格口 ID 不存在或配置文件缺失

**排查步骤：**
1. 检查格口配置文件是否存在
2. 确认格口 ID 是否正确
3. 查看配置加载日志

**解决方法：**
- 添加或修复格口配置
- 重新加载配置

#### 错误 4：CartNumberWhenHeadAtOrigin 配置无效

**日志示例：**
```
[ERROR] 格口 1 的 CartNumberWhenHeadAtOrigin=0 超出 [1, 100] 范围，配置无效。场景=快照捕获
```

**原因：**
- `CartNumberWhenHeadAtOrigin` 为 0 或负数
- 超过 `TotalCartCount` 的范围

**排查步骤：**
1. 检查格口配置中的 `CartNumberWhenHeadAtOrigin` 值
2. 确认该值在 `[1, TotalCartCount]` 范围内

**解决方法：**
- 修正格口配置，设置正确的 `CartNumberWhenHeadAtOrigin`
- 重新加载配置

#### 错误 5：首车编号超出有效范围

**日志示例：**
```
[ERROR] 首车编号 101 超出有效范围 [1, 100]，这表明系统状态不一致。格口ID=1, 场景=快照捕获
```

**原因：**
- 首车编号与 `TotalCartCount` 不一致
- 可能是首车跟踪逻辑错误或配置错误

**排查步骤：**
1. 检查首车跟踪器的状态
2. 确认 `TotalCartCount` 配置是否与现场实际一致
3. 查看首车编号的历史变化

**解决方法：**
- 重新学习小车总数
- 重置首车跟踪器
- 修正 `TotalCartCount` 配置

### 5.2 调试技巧

#### 技巧 1：启用详细日志

在 `appsettings.json` 中设置日志级别为 `Debug`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ZakYip.NarrowBeltDiverterSorter.Execution.Sorting": "Debug",
      "ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting": "Debug"
    }
  }
}
```

#### 技巧 2：使用快照进行诊断

调用 `CaptureCartBindingSnapshot` 方法可以获取当前配置和状态的完整快照：

```csharp
var snapshot = cartAtChuteResolver.CaptureCartBindingSnapshot(chuteId);
Console.WriteLine($"TotalCartCount: {snapshot.TotalCartCount}");
Console.WriteLine($"HeadCartNumber: {snapshot.HeadCartNumber}");
Console.WriteLine($"CartNumberWhenHeadAtOrigin: {snapshot.CartNumberWhenHeadAtOrigin}");
Console.WriteLine($"CapturedAt: {snapshot.CapturedAt}");
```

#### 技巧 3：验证环形算法

使用 `ChuteCartNumberCalculator` 独立验证计算结果：

```csharp
var calculator = new ChuteCartNumberCalculator(logger);
var result = calculator.GetCartNumberAtChute(
    totalCartCount: 100,
    headCartNumber: 5,
    cartNumberWhenHeadAtOrigin: 90);
// 期望结果：94
```

#### 技巧 4：穷举测试

对于特定配置，穷举所有首车位置，验证结果都在有效范围内：

```csharp
for (int headCart = 1; headCart <= 100; headCart++)
{
    var result = calculator.GetCartNumberAtChute(100, headCart, 90);
    Assert.InRange(result, 1, 100);
}
```

### 5.3 健康检查

系统提供了专门的健康检查服务 `ICartRingHealthService`，可以检测：
- 小车总数是否已配置
- 首车跟踪器是否就绪
- 所有格口配置是否有效

定期运行健康检查，可以提前发现潜在问题。

---

## 六、最佳实践

### 6.1 配置管理

1. **集中配置**：所有格口的 `CartNumberWhenHeadAtOrigin` 应在配置文件中统一管理
2. **配置验证**：系统启动时应验证所有格口配置的有效性
3. **配置备份**：重要配置应有备份机制

### 6.2 包裹绑定

1. **统一接口**：所有包裹创建必须通过 `IPackageCartBinder`
2. **禁止手算**：不允许在业务代码中手工计算小车号
3. **错误处理**：捕获绑定失败异常，给出明确的用户提示

### 6.3 测试覆盖

1. **穷举测试**：对关键配置（如 TotalCartCount=100）穷举所有首车位置
2. **边界测试**：测试环绕点附近的情况
3. **随机测试**：使用随机参数验证算法的健壮性
4. **一致性测试**：验证相同输入多次调用返回相同结果

---

## 八、仿真测试与回归验证

### 8.1 仿真测试目的

本项目提供了全面的仿真测试，用于验证以下关键功能：

1. **首车原点基准下的格口小车号计算正确性**
   - 验证环形数组公式在各种首车位置下的计算结果
   - 确保计算结果始终在有效范围 [1, TotalCartCount] 内

2. **包裹创建时的上车号绑定一致性**
   - 验证 `IPackageCartBinder` 绑定结果与 `ICartAtChuteResolver` 一致
   - 确保所有包裹创建路径统一使用标准接口

3. **配置热更新后的新旧配置切换正常性**
   - 验证运行中修改 `CartNumberWhenHeadAtOrigin` 后立即生效
   - 确保新旧配置不会混合，前后批次包裹分别使用对应配置

4. **异常场景下的错误处理机制**
   - 验证各种配置错误（TotalCartCount=0、越界等）的检测和报告
   - 确保错误信息为中文，包含关键参数，便于排查

### 8.2 如何执行仿真测试

#### 8.2.1 执行所有测试（包括仿真）

```bash
# 在仓库根目录执行
dotnet test
```

#### 8.2.2 仅执行仿真测试

仿真测试统一标记为 `Category=Simulation` 和 `Category=CartBinding`，可通过过滤器单独执行：

```bash
# 仅执行仿真相关测试
dotnet test --filter "TestCategory=Simulation"

# 仅执行小车绑定相关测试
dotnet test --filter "TestCategory=CartBinding"

# 同时匹配两个类别
dotnet test --filter "TestCategory=Simulation|TestCategory=CartBinding"
```

#### 8.2.3 执行仿真测试项目

```bash
# 指定项目路径执行
dotnet test Tests/ZakYip.NarrowBeltDiverterSorter.Simulator.Tests
```

### 8.3 仿真测试场景覆盖

仿真测试位于项目 `ZakYip.NarrowBeltDiverterSorter.Simulator.Tests` 中，按场景分为以下几类：

#### 8.3.1 基础正确性场景 (BasicCorrectnessSimulationTests)

验证典型配置下的计算正确性：
- **配置**：TotalCartCount = 100, Chute1.Base = 90, Chute3.Base = 80
- **测试首车位置**：1, 5, 98, 99, 100, 12（覆盖起始、中间、回绕等关键点）
- **验证内容**：
  - 每个首车位置对应的格口小车号与预期一致
  - 包裹绑定结果与 Resolver 计算结果一致
  - 所有结果在有效范围 [1, 100] 内

#### 8.3.2 连续移动场景 (ContinuousMovementSimulationTests)

验证首车连续移动时的行为：
- **完整循环测试**：首车从 1 → 100 → 1 循环，验证所有位置的小车号计算正确
- **包裹创建测试**：每 N 步创建包裹，验证绑定结果与 Resolver 一致，无 off-by-one 错误
- **环绕处理测试**：关注 10-13、98-100、1-2 等环绕点附近的行为
- **多格口测试**：验证格口 1 和格口 3 的计算互不干扰，偏移量保持恒定

#### 8.3.3 热更新场景 (HotUpdateSimulationTests)

验证运行中修改配置的行为：
- **单次更新测试**：
  - 前半段使用旧配置（Base=90）绑定包裹
  - 运行中修改配置（Base=85）
  - 后半段使用新配置绑定包裹
  - 验证新旧配置结果不同，且各自批次内部一致
- **批量更新测试**：
  - 连续绑定 5 个包裹使用旧配置
  - 更新配置
  - 再绑定 5 个包裹使用新配置
  - 验证两批包裹完全使用对应配置，无混合

#### 8.3.4 异常场景 (ErrorScenarioSimulationTests)

验证各种配置错误和状态异常的处理：
- **TotalCartCount 异常**：
  - TotalCartCount = 0（未完成学习）
  - TotalCartCount < 0（配置错误）
  - 预期：抛出 `InvalidOperationException`，包含中文错误提示
- **首车状态异常**：
  - CartPositionTracker 未初始化
  - CurrentOriginCartIndex 为 null
  - 预期：抛出异常，提示"当前首车状态未就绪"
- **格口配置异常**：
  - 格口配置不存在
  - CartNumberWhenHeadAtOrigin 为 0、负数或超过 TotalCartCount
  - 预期：抛出异常，提示配置无效，包含参数值和有效范围
- **首车编号越界**：
  - HeadCartNumber 超过 TotalCartCount
  - 预期：抛出异常，提示首车编号超出有效范围
- **包裹绑定失败传播**：
  - 当 Resolver 失败时，PackageCartBinder 应该传播异常
  - 预期：异常信息一致，便于上层处理

### 8.4 如何判断逻辑无退化

#### 8.4.1 自动化验证

1. **所有仿真测试全部通过**
   ```bash
   dotnet test --filter "TestCategory=Simulation"
   ```
   输出应显示：`Passed: XX, Failed: 0`

2. **日志中无意外错误**
   - 检查测试输出，确保没有"越界"、"未配置"等异常日志
   - 仿真测试使用 `NullLogger`，不产生日志干扰

3. **CI 流水线验证**
   - 每次 PR 和 push 都会自动执行全部测试
   - 查看 GitHub Actions 工作流运行记录，确认仿真测试通过

#### 8.4.2 手工验证（可选）

对于关键仿真测试，可以输出关键样本供人工抽样检查：

```csharp
// 示例：输出首车号、格口号、绑定车号
[Fact]
public void Manual_Verification_Sample()
{
    var samples = new[]
    {
        (Head: 1, Chute: 1, Expected: 90),
        (Head: 5, Chute: 1, Expected: 94),
        (Head: 12, Chute: 1, Expected: 1),
    };

    foreach (var (head, chute, expected) in samples)
    {
        var resolver = CreateResolver(head);
        var actual = resolver.ResolveCurrentCartNumberForChute(chute);
        Assert.Equal(expected, actual);
        Console.WriteLine($"首车={head}, 格口={chute}, 绑定车号={actual} (预期={expected}) ✓");
    }
}
```

### 8.5 回归测试策略

1. **每次代码变更后**：
   - 执行 `dotnet test` 确保所有测试通过
   - 重点关注与小车号计算、包裹绑定相关的修改

2. **每次配置变更后**：
   - 执行仿真测试验证新配置的正确性
   - 特别是修改 `TotalCartCount` 或 `CartNumberWhenHeadAtOrigin` 时

3. **重构或重大变更前**：
   - 先确保现有仿真测试全部通过（建立基线）
   - 变更后再次执行，对比结果无退化
   - 必要时增加新的仿真测试覆盖新逻辑

4. **生产环境上线前**：
   - 完整执行一次全部测试（包括仿真）
   - 检查 CI 记录，确认最近一次构建的测试结果
   - 审查测试覆盖率报告，确保关键路径被覆盖

---

## 七、扩展阅读

- [窄带分拣机设计文档](../NarrowBeltDesign.md)
- [格口 IO 架构](../ChuteIoArchitecture.md)
- [自检服务使用指南](../implementation-summaries/)

---

## 附录：快速参考

### 关键接口

- `ICartAtChuteResolver`：格口小车号解析器
- `IPackageCartBinder`：包裹小车绑定服务
- `IChuteCartNumberCalculator`：环形数组计算器
- `CartBindingSnapshot`：小车绑定快照

### 关键参数

- `TotalCartCount`：1 到 N（例如 100）
- `HeadCartNumber`：1 到 TotalCartCount
- `CartNumberWhenHeadAtOrigin`：1 到 TotalCartCount
- 所有编号都是 1 基索引

### 公式速查

```
CartNumber = ((CartNumberWhenHeadAtOrigin - 1 + HeadCartNumber - 1) % TotalCartCount) + 1
```

---

**文档版本**：1.0  
**最后更新**：2025-11-20  
**维护者**：ZakYip.NarrowBeltDiverterSorter 开发团队
