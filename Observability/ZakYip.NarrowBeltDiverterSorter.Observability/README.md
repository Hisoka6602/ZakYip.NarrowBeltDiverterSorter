# Observability（观测层）

## 概述

Observability 层提供系统观测能力，包括日志、事件录制/回放、实时视图聚合等。这一层是横切关注点，理想情况下不依赖任何业务项目。

## 职责

- 事件总线（`IEventBus`）
- 事件录制/回放（Recording/Replay）
- 实时视图聚合（`INarrowBeltLiveView`）
- 分析（Analytics）

## 架构原则

### ✅ 允许
- 提供观测接口
- 事件聚合和转发
- 独立存在

### ❌ 禁止
- 控制硬件
- 业务逻辑决策
- 依赖业务项目

## 当前状态

✅ **架构符合**: Observability 当前无项目依赖，完全独立

## 依赖

```
Observability
  └── （无项目依赖）
```

## 参考

- [分层架构文档](../architecture/Layering.md)
