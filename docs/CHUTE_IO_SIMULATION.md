# 格口 IO 仿真联动测试文档
# Chute IO Simulation Integration Test Documentation

## 概述 (Overview)

本功能为智嵌格口 IO 驱动与仿真系统的集成测试场景，支持验证多 IP 节点映射、格口开闭顺序以及安全关闭策略。

This feature provides integration test scenarios for ZhiQian chute IO drivers with the simulation system, supporting verification of multi-IP node mapping, chute open/close sequencing, and safe shutdown strategies.

## 功能特性 (Features)

### 1. 多 IP 节点支持 (Multi-IP Node Support)

- 单个 IP 控制 1-32 个格口 (Single IP controlling 1-32 chutes)
- 多个 IP 节点支持超过 32 个格口 (Multiple IP nodes supporting >32 chutes)
- 格口到节点的灵活映射配置 (Flexible chute-to-node mapping configuration)

### 2. 仿真模式 (Simulation Modes)

#### ChuteIoHardwareDryRun 模式
专门用于验证格口 IO 行为，不需要真实硬件反馈。

Specifically for verifying chute IO behavior without requiring real hardware feedback.

**特性:**
- 启动主线仿真和包裹生成 (Start mainline simulation and parcel generation)
- 只关注格口开闭节奏 (Focus only on chute open/close timing)
- 可以使用回环地址或假端点 (Can use loopback address or fake endpoints)

### 3. 安全关闭策略 (Safe Shutdown Strategy)

系统在以下情况会自动关闭所有格口：
The system automatically closes all chutes in the following scenarios:

1. 程序启动时 (On application startup)
2. 程序正常退出时 (On normal application shutdown)
3. 程序异常退出时 (On abnormal application termination)

通过 `SafetyControlWorker` 和 `IHostApplicationLifetime.ApplicationStopping` 事件实现。
Implemented via `SafetyControlWorker` and `IHostApplicationLifetime.ApplicationStopping` event.

### 4. 未映射格口处理 (Unmapped Chute Handling)

当尝试操作未配置映射的格口时：
When attempting to operate on unmapped chutes:

- 记录错误日志 (Log error messages)
- 系统继续运行不崩溃 (System continues without crashing)
- 便于诊断配置问题 (Facilitates configuration issue diagnosis)

## 配置示例 (Configuration Examples)

### 单 IP 节点配置 (Single IP Node)

控制 1-32 个格口的配置：
Configuration for controlling 1-32 chutes:

```json
{
  "ChuteIo": {
    "Mode": "Simulation",
    "Nodes": [
      {
        "NodeKey": "zq-node-1",
        "Brand": "ZhiQian32Relay",
        "IpAddress": "192.168.1.100",
        "Port": 8080,
        "MaxChannelCount": 32,
        "Channels": [
          { "ChuteId": 1, "ChannelIndex": 1 },
          { "ChuteId": 2, "ChannelIndex": 2 },
          // ... up to 32
          { "ChuteId": 32, "ChannelIndex": 32 }
        ]
      }
    ]
  }
}
```

### 多 IP 节点配置 (Multi-IP Nodes)

控制 64 个格口的配置（2 个 IP 节点）：
Configuration for controlling 64 chutes (2 IP nodes):

```json
{
  "ChuteIo": {
    "Mode": "Simulation",
    "Nodes": [
      {
        "NodeKey": "zq-node-1",
        "Brand": "ZhiQian32Relay",
        "IpAddress": "192.168.1.100",
        "Port": 8080,
        "MaxChannelCount": 32,
        "Channels": [
          { "ChuteId": 1, "ChannelIndex": 1 },
          // ... chutes 1-32
          { "ChuteId": 32, "ChannelIndex": 32 }
        ]
      },
      {
        "NodeKey": "zq-node-2",
        "Brand": "ZhiQian32Relay",
        "IpAddress": "192.168.1.101",
        "Port": 8080,
        "MaxChannelCount": 32,
        "Channels": [
          { "ChuteId": 33, "ChannelIndex": 1 },
          // ... chutes 33-64
          { "ChuteId": 64, "ChannelIndex": 32 }
        ]
      }
    ]
  }
}
```

## 运行测试 (Running Tests)

### 单元测试 (Unit Tests)

```bash
dotnet test --filter "FullyQualifiedName~ChuteIoServiceTests"
```

测试涵盖：
Tests cover:
- ✓ 单 IP 节点 32 格口场景
- ✓ 多 IP 节点 64 格口场景
- ✓ 未映射格口错误处理
- ✓ 智嵌继电器服务基本功能
- ✓ 格口映射配置正确性

### 仿真场景测试 (Simulation Scenario Test)

运行 ChuteIoHardwareDryRun 场景：
Run the ChuteIoHardwareDryRun scenario:

```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Simulation -- \
  --environment ChuteIoHardwareDryRun
```

预期输出示例：
Expected output example:

```
[仿真启动] 步骤 1/7: 连接现场总线...
[仿真启动] 步骤 2/7: 启动入口输送线...
...
[模拟格口IO服务] 打开格口 5 (端点=zq-node-1, 通道=5)
[模拟格口IO服务] 关闭格口 5 (端点=zq-node-1, 通道=5)
[模拟格口IO服务] 打开格口 35 (端点=zq-node-2, 通道=3)
[模拟格口IO服务] 关闭格口 35 (端点=zq-node-2, 通道=3)
...
[仿真停止] 关闭所有格口...
[模拟格口IO服务] 关闭所有格口 (共 2 个端点)
[模拟格口IO端点] 端点 zq-node-1 所有通道 (1..32) 状态设置为 关
[模拟格口IO端点] 端点 zq-node-2 所有通道 (1..32) 状态设置为 关
[仿真停止] 已关闭所有格口
```

## 验证清单 (Verification Checklist)

运行仿真后，应能观察到：
After running the simulation, you should observe:

- [x] 每个落格动作输出"格口→节点→通道"映射信息
- [x] 仿真结束时输出"关闭全部格口"日志
- [x] 格口数量超过 32 时，多个 IP 节点被正确使用
- [x] 不同节点控制的格口范围正确（1-32 由节点1，33-64 由节点2）
- [x] 未映射格口触发落格时记录错误但不崩溃

## 仿真报告 (Simulation Report)

仿真结束后会生成包含格口 IO 统计的报告：
After simulation completion, a report with chute IO statistics is generated:

```json
{
  "ChuteIo": {
    "Mode": "Simulation",
    "MappedChuteCount": 64,
    "NodeCount": 2,
    "Nodes": [
      {
        "NodeKey": "zq-node-1",
        "IpAddress": "192.168.1.100",
        "Port": 8080,
        "ControlledChutes": [1, 2, 3, ..., 32]
      },
      {
        "NodeKey": "zq-node-2",
        "IpAddress": "192.168.1.101",
        "Port": 8080,
        "ControlledChutes": [33, 34, 35, ..., 64]
      }
    ],
    "OpenActionCount": 50,
    "CloseActionCount": 50,
    "CloseAllExecuted": true
  }
}
```

## 架构说明 (Architecture Notes)

### 关键组件 (Key Components)

1. **IChuteIoService**: 格口 IO 服务接口
   - OpenAsync(chuteId): 打开格口
   - CloseAsync(chuteId): 关闭格口
   - CloseAllAsync(): 关闭所有格口

2. **SimulationChuteIoService**: 模拟实现
   - 只记录日志，不连接真实硬件
   - 用于仿真和测试场景

3. **ZhiQian32RelayChuteIoService**: 智嵌继电器实现
   - 通过 TCP 连接智嵌 32 路网络继电器
   - 支持多节点映射

4. **ChuteSafetyService**: 安全控制服务
   - 优先使用 IChuteIoService（如果可用）
   - 回退到 IChuteTransmitterPort（向后兼容）
   - 在启动和停止时调用 CloseAllAsync()

5. **SimulationOrchestrator**: 仿真编排器
   - 协调仿真系统各组件
   - 在停止时调用 IChuteIoService.CloseAllAsync()

## 故障排查 (Troubleshooting)

### 问题：格口未响应
**Problem: Chute not responding**

检查：
Check:
1. 格口 ID 是否在配置中有映射
2. 节点 IP 地址和端口是否正确
3. 网络连接是否正常（仅硬件模式）

### 问题：多 IP 节点未工作
**Problem: Multi-IP nodes not working**

检查：
Check:
1. 每个节点的 NodeKey 是否唯一
2. ChuteId 范围是否有重叠
3. ChannelIndex 是否在 1 到 MaxChannelCount 范围内

### 问题：CloseAll 未执行
**Problem: CloseAll not executed**

检查：
Check:
1. SafetyControlWorker 是否已注册
2. IChuteIoService 是否在 DI 容器中
3. 查看应用程序停止时的日志

## 后续改进 (Future Improvements)

- [ ] 实现包裹异常口/强排策略（当格口无映射时）
- [ ] 添加格口 IO 性能监控指标
- [ ] 支持更多品牌的继电器/IO 模块
- [ ] 添加格口状态实时查询功能

## 参考资料 (References)

- [智嵌 32 路网络继电器协议文档](docs/zhiqian-32relay-protocol.md)
- [系统架构文档](SORTING_SYSTEM.md)
- [配置说明](docs/configuration.md)
