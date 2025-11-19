## PR 描述 (Description)

<!-- 请简要描述本 PR 的目的和主要变更 -->

## 变更类型 (Type of Change)

<!-- 请勾选适用的选项 -->

- [ ] 🐛 Bug 修复 (Bug fix)
- [ ] ✨ 新功能 (New feature)
- [ ] 🔨 重构 (Refactoring)
- [ ] 📝 文档更新 (Documentation update)
- [ ] 🧪 测试相关 (Test changes)
- [ ] 🔧 配置更改 (Configuration change)

## 架构硬性规则检查 (Architecture Rules Checklist)

<!-- 在提交 PR 前，请务必检查以下项目。详见 ARCHITECTURE_RULES.md -->

### Host 层规则

- [ ] Host 层代码只包含 DI 配置和启动逻辑，没有业务逻辑
- [ ] 所有服务注册都使用依赖注入

### 时间使用规则

- [ ] 没有直接使用 `DateTime.Now` 或 `DateTime.UtcNow`
- [ ] 所有时间获取都通过 `ILocalTimeProvider` 或类似的时间提供器

### 异常处理规则

- [ ] 所有外部调用（硬件、网络、文件 IO）都使用了安全隔离器
- [ ] 异常处理不会导致应用崩溃

### 线程安全规则

- [ ] 多线程共享的集合使用了线程安全类型（`ConcurrentDictionary`、`ImmutableList` 等）
- [ ] 没有不安全的并发访问

### 语言特性规则

- [ ] DTO 和事件载荷使用了 `record` 或 `record struct`
- [ ] 必填属性使用了 `required` + `init`
- [ ] 事件载荷命名以 `EventArgs` 结尾
- [ ] 性能关键的值类型使用了 `readonly struct`（如适用）

### 文档更新规则

- [ ] README.md 已更新（如果功能影响项目整体）
- [ ] docs/ 目录下的相应文档已更新
- [ ] 新增功能有使用说明和示例
- [ ] 架构图已更新（如有架构变化）

## 例外说明 (Exceptions)

<!-- 如果某些规则在本 PR 中不适用或有特殊原因需要例外，请在此说明 -->

N/A

## 测试 (Testing)

<!-- 请描述如何测试本 PR 的变更 -->

- [ ] 已添加单元测试
- [ ] 已添加集成测试
- [ ] 已运行现有测试套件，全部通过
- [ ] 已手动测试功能

### 测试结果

<!-- 请粘贴测试结果或截图 -->

```
# dotnet test 输出
```

## 相关 Issue (Related Issues)

<!-- 请链接相关的 Issue -->

Closes #

## 额外说明 (Additional Notes)

<!-- 任何其他需要说明的信息 -->

---

**审查者注意事项 (Reviewer Notes):**

1. 请仔细检查上述架构规则检查项是否都已勾选
2. 如有例外情况，请确认例外原因合理
3. 确保所有测试通过
4. 检查代码风格是否符合 CONTRIBUTING.md
