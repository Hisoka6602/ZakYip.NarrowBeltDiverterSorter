# Infrastructure（基础设施层）

## 概述

Infrastructure 层提供技术基础设施能力，主要是配置持久化（LiteDB）和外部库封装。这一层不包含业务逻辑，只提供技术支持。

## 职责

- LiteDB 配置存储实现
- 配置仓储实现（`IMainLineOptionsRepository` 等）
- 外部库封装

## 架构原则

### ✅ 允许
- 依赖 Core（实现接口）
- 依赖 Communication（用于某些配置）
- 数据库操作
- 文件 IO

### ❌ 禁止
- 业务逻辑
- 硬件控制逻辑
- 依赖 Execution, Host 等上层

## 依赖

```
Infrastructure
  ├── Core ✅
  └── Communication ✅
```

## 参考

- [分层架构文档](../architecture/Layering.md)
