# Host（主机层）

## 概述

Host 是整个系统的组合根和程序入口，负责 DI 注册、配置加载、Web API 暴露和后台服务启动。Host 不包含业务逻辑，只负责组装和协调各个模块。

## 职责

- DI 容器配置和服务注册
- 配置加载（LiteDB + 默认配置）
- Kestrel/ASP.NET Core 配置
- SignalR Hub 注册
- API Controllers
- 后台工作器（Workers）
- 健康检查

## 关键文件

- **Program.cs**: 整个系统的组合根，所有 DI 注册在此完成
- **ProductionMainLineSetpointProvider.cs**: 生产环境的主线设定点提供者
- **StubInfeedConveyorPort.cs**: 入口输送线占位实现

## 架构原则

### ✅ 允许
- 依赖所有其他业务项目
- 注册服务到 DI 容器
- 暴露 Web API 和 SignalR Hub

### ❌ 禁止
- 实现业务逻辑（应在 Execution 层）
- 实现领域模型（应在 Core 层）
- 实现持久化逻辑（应在 Infrastructure 层）

## 配置加载

Host 使用统一的配置加载策略：

1. **LiteDB 配置**: 通过 `ISorterConfigurationStore` 从 LiteDB 加载
2. **默认配置**: 如果 LiteDB 不可用，使用 `NarrowBeltDefaultConfigSeeder` 提供的默认值
3. **appsettings.json**: 用于非业务配置（如日志级别、URL等）

## 启动模式

通过 `StartupModeConfiguration` 控制启动模式：

- **Normal**: 正常模式，所有服务启动
- **BringupMainline**: 主线调试模式
- **BringupInfeed**: 入口调试模式
- **BringupChutes**: 格口调试模式

## 依赖

```
Host
  ├── Core ✅
  ├── Execution ✅
  ├── Ingress ✅
  ├── Observability ✅
  ├── Infrastructure ✅
  ├── Communication ✅
  └── UpstreamContracts ✅
```

## 参考

- [分层架构文档](../architecture/Layering.md)
