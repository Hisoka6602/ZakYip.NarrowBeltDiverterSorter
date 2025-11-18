# ZakYip.NarrowBeltDiverterSorter 依赖规则

## 依赖方向规则

### 允许的引用关系

```
┌─────────┐
│  Host   │ ───┐
└─────────┘    │
               ▼
┌──────────────────────────────────────────────────────┐
│  Execution, Ingress, Observability, Infrastructure,  │
│  Communication, UpstreamContracts, Simulation        │
└──────────────────────────────────────────────────────┘
               │
               ▼
┌─────────┐
│  Core   │
└─────────┘
```

### 详细规则

#### 1. Core（核心层）
- ✅ **可以依赖**: BCL、基础库（如 `Microsoft.Extensions.Logging.Abstractions`）、UpstreamContracts
- ❌ **不可依赖**: 任何其他业务项目
- **原则**: Core 是最底层，不依赖任何业务逻辑层

#### 2. Execution（执行层）
- ✅ **可以依赖**: Core, Communication, Observability
- ❌ **不可依赖**: Host, Ingress, Infrastructure, Simulation
- **原则**: 执行层只关注业务逻辑执行，不关心输入来源和持久化

#### 3. Ingress（入口层）
- ✅ **可以依赖**: Core, Communication
- ❌ **不可依赖**: Host, Execution, Observability, Infrastructure, Simulation
- **原则**: 入口层只负责IO采集和事件转换，不依赖业务逻辑

#### 4. Observability（观测层）
- ✅ **可以依赖**: （理想情况下只依赖 BCL）
- ❌ **不可依赖**: 任何业务项目
- **当前状态**: ✓ 无项目依赖
- **原则**: 观测层是横切关注点，应保持独立

#### 5. Infrastructure（基础设施层）
- ✅ **可以依赖**: Core, Communication
- ❌ **不可依赖**: Host, Execution, Ingress, Observability, Simulation
- **原则**: 基础设施层提供技术能力，不包含业务逻辑

#### 6. Communication（通信层）
- ✅ **可以依赖**: Core, UpstreamContracts
- ❌ **不可依赖**: Host, Execution, Ingress, Observability, Infrastructure, Simulation
- **原则**: 通信层是底层技术能力，保持独立

#### 7. UpstreamContracts（上游合约层）
- ✅ **可以依赖**: BCL
- ❌ **不可依赖**: 任何业务项目
- **原则**: 合约层完全独立，定义接口标准

#### 8. Simulation（仿真层）
- ✅ **可以依赖**: Core, Execution, Ingress, Infrastructure, Communication, UpstreamContracts
- ⚠️  **当前架构债务**: Simulation -> Host（需要使用 Workers）
- ❌ **不应依赖**: Host
- **原则**: Simulation 是独立的组合根，应该复用 Execution 的逻辑而不是 Host 的 Workers

#### 9. Host（主机层）
- ✅ **可以依赖**: 所有其他业务项目
- **原则**: Host 是组合根，负责组装所有模块

#### 10. Tests（测试层）
- ✅ **可以依赖**: 任何业务项目
- **原则**: 测试可以测试任何模块

---

## 当前依赖关系矩阵

| 项目 | Core | Execution | Ingress | Observability | Infrastructure | Communication | Upstream | Simulation | Host |
|------|------|-----------|---------|---------------|----------------|---------------|----------|------------|------|
| **Core** | - | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| **Execution** | ✅ | - | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| **Ingress** | ✅ | ❌ | - | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| **Observability** | ❌ | ❌ | ❌ | - | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Infrastructure** | ✅ | ❌ | ❌ | ❌ | - | ✅ | ❌ | ❌ | ❌ |
| **Communication** | ✅ | ❌ | ❌ | ❌ | ❌ | - | ✅ | ❌ | ❌ |
| **UpstreamContracts** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | - | ❌ | ❌ |
| **Simulation** | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | - | ⚠️ |
| **Host** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | - |

**图例**:
- ✅ = 允许且已实现
- ❌ = 不允许
- ⚠️  = 架构债务，需要后续修复

---

## 违反规则的当前状态

### 1. Simulation -> Host（架构债务）
**状态**: ⚠️  已标记为架构债务

**问题**: Simulation 需要使用 Host 中的 Workers（`MainLineControlWorker`, `ParcelRoutingWorker`, `SortingExecutionWorker`）

**解决方案**:
1. **推荐**: 将 Workers 移至 Execution 层（它们本质上是执行编排）
2. **替代**: 创建共享的 Workers 库
3. **临时**: 在 Simulation.csproj 中标记为 TODO，后续 PR 修复

**标记位置**: `ZakYip.NarrowBeltDiverterSorter.Simulation/ZakYip.NarrowBeltDiverterSorter.Simulation.csproj`

```xml
<!-- TODO: 架构债务 - Simulation 不应依赖 Host，需要将 Workers 移至 Execution 或创建共享层 -->
<ProjectReference Include="..\ZakYip.NarrowBeltDiverterSorter.Host\ZakYip.NarrowBeltDiverterSorter.Host.csproj" />
```

---

## 如何验证依赖规则

### 方法 1: 手动检查
检查每个项目的 `.csproj` 文件中的 `<ProjectReference>` 节点。

### 方法 2: 架构约束测试（推荐）
创建 `ZakYip.NarrowBeltDiverterSorter.ArchTests` 项目，使用反射或 ArchUnit.NET 验证依赖规则。

示例测试结构:
```csharp
[Fact]
public void Core_ShouldNotDependOnOtherBusinessProjects()
{
    var coreAssembly = typeof(SomeCore Type).Assembly;
    var references = coreAssembly.GetReferencedAssemblies();
    
    // Assert: Core 不应引用 Execution, Ingress, Host 等
    Assert.DoesNotContain(references, r => r.Name.Contains("Execution"));
    Assert.DoesNotContain(references, r => r.Name.Contains("Ingress"));
    // ...
}
```

### 方法 3: CI/CD 集成
在 CI/CD 管道中运行架构测试，防止违反依赖规则的代码合并。

---

## 依赖注入原则

### 依赖倒置原则（DIP）
- 高层模块（Execution）不应依赖低层模块（Ingress），两者都应依赖抽象（Core 中的接口）
- 抽象（Core）不应依赖细节（Execution），细节应依赖抽象

### 示例

❌ **错误**:
```csharp
// Execution 直接依赖 Ingress 的具体类
public class SortingPlanner
{
    private readonly InfeedSensorMonitor _monitor; // 错误：依赖具体类
}
```

✅ **正确**:
```csharp
// Execution 依赖 Core 中的接口
public class SortingPlanner
{
    private readonly IInfeedSensorPort _sensor; // 正确：依赖抽象
}
```

---

## 循环依赖处理

### 问题识别
如果 A 依赖 B，B 依赖 A，则形成循环依赖，编译器会报错。

### 解决方案
1. **提取接口到 Core**: 将共享的接口提取到 Core 层
2. **事件驱动**: 使用事件总线（Observability.IEventBus）解耦
3. **重新设计**: 重新考虑职责划分，可能存在设计问题

---

## 后续工作

### 高优先级
1. **移除 Simulation -> Host 依赖**: 将 Workers 移至 Execution
2. **创建架构约束测试**: 自动化验证依赖规则
3. **CI/CD 集成**: 在管道中运行架构测试

### 中优先级
4. **Communication 项目定位**: 明确其角色（可能归入 Infrastructure）
5. **UpstreamContracts 整合**: 评估是否应合并到 Core

### 低优先级
6. **依赖可视化**: 使用工具生成依赖关系图
7. **依赖度量**: 追踪项目间的耦合度

---

## 参考资料

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Dependency Inversion Principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle)
- [ArchUnit.NET](https://github.com/TNG/ArchUnitNET)

---

## 版本历史

- **v1.0** (2025-11-18): 初始版本，建立依赖规则基线
