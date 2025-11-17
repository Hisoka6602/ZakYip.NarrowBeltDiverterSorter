# 事件录制与回放功能 (Event Recording & Replay)

## 概述 (Overview)

本功能为窄带分拣系统提供了全链路事件录制与回放能力，可以：

- **录制真实运行场景**：记录生产环境中的所有关键事件
- **离线分析问题**：回放历史事件流，精确复现故障场景
- **算法回归测试**：基于真实数据验证算法改进
- **教学演示**：展示系统运行过程

## 架构设计 (Architecture)

### 分层结构

```
┌─────────────────────────────────────────┐
│         Host / API Layer                │
│  RecordingsController (REST API)        │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│      Observability Layer                │
│  - IEventRecordingManager               │
│  - FileEventRecordingManager            │
│  - RecordingEventSubscriber             │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│       Event Bus (IEventBus)             │
│  速度/包裹/小车/安全/状态事件             │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│      Simulation Layer                   │
│  - IRecordingReplayRunner               │
│  - RecordingReplayRunner                │
└─────────────────────────────────────────┘
```

### 核心组件

#### 1. 录制会话 (Recording Session)

每个录制会话包含：
- **SessionId**: 唯一标识符 (GUID)
- **Name**: 会话名称 (如 "2025-11-18_下午班_异常工况")
- **Description**: 可选描述
- **StartedAt / StoppedAt**: 开始和结束时间
- **IsCompleted**: 是否正常结束
- **EventCount**: 事件总数

#### 2. 事件封装 (Event Envelope)

```json
{
  "SessionId": "guid",
  "Timestamp": "2025-11-18T10:30:45.123Z",
  "EventType": "LineSpeedChanged",
  "PayloadJson": "{...}",
  "CorrelationId": "optional"
}
```

#### 3. 存储格式

使用 **NDJSON (Newline Delimited JSON)** 格式：
- 每行一个事件，独立的 JSON 对象
- 支持流式写入和读取
- 易于追加、解析和调试

文件结构：
```
recordings/
  ├── {session-id}/
  │   ├── session.json          # 会话元数据
  │   └── events.ndjson          # 事件流
  └── {another-session-id}/
      ├── session.json
      └── events.ndjson
```

## API 使用指南 (API Usage)

### 1. 启动录制

**请求**:
```http
POST /api/recordings/start
Content-Type: application/json

{
  "name": "压力测试-1000包裹",
  "description": "测试高负载场景下的系统表现"
}
```

**响应**:
```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "压力测试-1000包裹",
  "startedAt": "2025-11-18T10:30:00Z",
  "stoppedAt": null,
  "description": "测试高负载场景下的系统表现",
  "isCompleted": false,
  "eventCount": 0
}
```

### 2. 停止录制

**请求**:
```http
POST /api/recordings/{sessionId}/stop
```

**响应**:
```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "压力测试-1000包裹",
  "startedAt": "2025-11-18T10:30:00Z",
  "stoppedAt": "2025-11-18T11:45:30Z",
  "isCompleted": true,
  "eventCount": 15234
}
```

### 3. 查询所有会话

**请求**:
```http
GET /api/recordings
```

**响应**:
```json
[
  {
    "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "压力测试-1000包裹",
    "startedAt": "2025-11-18T10:30:00Z",
    "stoppedAt": "2025-11-18T11:45:30Z",
    "isCompleted": true,
    "eventCount": 15234
  }
]
```

### 4. 获取会话详情

**请求**:
```http
GET /api/recordings/{sessionId}
```

### 5. 回放会话

**请求**:
```http
POST /api/recordings/{sessionId}/replay
Content-Type: application/json

{
  "mode": "Accelerated",
  "speedFactor": 10.0,
  "fixedIntervalMs": 100
}
```

**回放模式**:
- `OriginalSpeed`: 原速回放，保持原始事件间隔
- `Accelerated`: 加速回放，speedFactor=10 表示快10倍
- `FixedInterval`: 固定间隔，每100ms发送一个事件

**注意**: 回放功能需要在 Simulation 模式下运行，并注册 `IRecordingReplayRunner` 服务。

## 录制的事件类型 (Recorded Event Types)

系统自动录制以下事件：

1. **速度相关**:
   - `LineSpeedChanged`: 主线速度变化

2. **包裹相关**:
   - `ParcelCreated`: 包裹创建
   - `ParcelDiverted`: 包裹落格

3. **小车/格口相关**:
   - `OriginCartChanged`: 原点小车变更
   - `CartAtChuteChanged`: 格口下方小车变更
   - `CartLayoutChanged`: 小车布局变更

4. **安全/运行状态**:
   - `LineRunStateChanged`: 线体运行状态变化
   - `SafetyStateChanged`: 安全状态变化

5. **设备状态**:
   - `DeviceStatusChanged`: 设备状态变化

## 使用场景示例 (Usage Scenarios)

### 场景1：记录生产环境异常

```bash
# 1. 检测到异常时立即启动录制
curl -X POST http://localhost:5000/api/recordings/start \
  -H "Content-Type: application/json" \
  -d '{"name":"急停故障-20251118","description":"15:30 急停按钮触发"}'

# 2. 系统恢复后停止录制
curl -X POST http://localhost:5000/api/recordings/{sessionId}/stop

# 3. 离线分析
curl http://localhost:5000/api/recordings/{sessionId}
```

### 场景2：长时间压力测试

```bash
# 1. 启动录制
curl -X POST http://localhost:5000/api/recordings/start \
  -H "Content-Type: application/json" \
  -d '{"name":"长跑测试-2000包裹"}'

# 2. 运行长时间仿真...

# 3. 停止录制并查看结果
curl -X POST http://localhost:5000/api/recordings/{sessionId}/stop
```

### 场景3：算法回归测试

```bash
# 1. 在仿真环境中回放历史数据
curl -X POST http://localhost:5000/api/recordings/{sessionId}/replay \
  -H "Content-Type: application/json" \
  -d '{"mode":"Accelerated","speedFactor":5.0}'

# 2. 观察新算法在相同场景下的表现
# 3. 对比关键指标
```

## 配置选项 (Configuration)

### 录制目录

默认录制目录为 `recordings/`，可通过依赖注入配置：

```csharp
services.AddSingleton<FileEventRecordingManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FileEventRecordingManager>>();
    return new FileEventRecordingManager("/custom/path/recordings", logger);
});
```

### 事件订阅

`RecordingEventSubscriber` 自动订阅所有关键事件。如需自定义：

```csharp
// 在 Program.cs 中取消默认注册
// builder.Services.AddSingleton<RecordingEventSubscriber>();

// 创建自定义订阅器
public class CustomRecordingEventSubscriber : RecordingEventSubscriber
{
    // 覆盖或扩展事件处理
}
```

## 性能考虑 (Performance Considerations)

### IO 影响

- **写入策略**: 使用 `StreamWriter` 的 `AutoFlush = true`，确保每个事件立即写入磁盘
- **文件锁**: 使用 `SemaphoreSlim` 保护并发写入
- **预估占用**: 
  - 每个事件约 200-500 字节
  - 10,000 事件约 2-5 MB
  - 长时间运行（数小时）可能产生数百 MB 数据

### 建议

1. **录制时机**: 仅在需要时启动录制，避免长期开启
2. **磁盘空间**: 监控 `recordings/` 目录大小，定期清理旧会话
3. **回放性能**: 加速回放时注意事件总线和下游处理能力

## 故障排查 (Troubleshooting)

### 1. 录制文件为空

**原因**: 可能没有事件被发布到事件总线

**解决**:
- 检查系统是否在运行
- 确认 `RecordingEventSubscriber` 已注册并初始化
- 查看日志确认事件订阅成功

### 2. 回放失败

**原因**: 事件反序列化失败或类型不匹配

**解决**:
- 检查 events.ndjson 文件格式
- 确认事件类型定义未发生破坏性变更
- 查看日志中的反序列化错误

### 3. 无法启动新会话

**原因**: 已有活动会话

**解决**:
- 先停止当前活动会话：`POST /api/recordings/{sessionId}/stop`
- 或等待会话自动结束

## 开发指南 (Development Guide)

### 添加新事件类型

1. 在 `RecordingEventSubscriber` 中订阅新事件：
```csharp
_eventBus.Subscribe<NewEventArgs>(OnNewEventAsync);
```

2. 添加处理方法：
```csharp
private async Task OnNewEventAsync(NewEventArgs e, CancellationToken ct)
{
    await _recorder.RecordAsync("NewEvent", e, e.OccurredAt, ct: ct);
}
```

3. 在 `RecordingReplayRunner.PublishEventAsync` 中添加回放支持：
```csharp
case "NewEvent":
    var newEvent = JsonSerializer.Deserialize<NewEventArgs>(envelope.PayloadJson);
    if (newEvent != null)
    {
        await _eventBus.PublishAsync(newEvent, ct);
    }
    break;
```

### 运行测试

```bash
# 运行录制功能测试
dotnet test --filter "FullyQualifiedName~FileEventRecordingManagerTests"

# 所有 Observability 测试
dotnet test ZakYip.NarrowBeltDiverterSorter.Observability.Tests
```

## 未来增强 (Future Enhancements)

- [ ] 支持时间段裁剪（回放部分事件）
- [ ] 添加 UI 界面查看和管理录制会话
- [ ] 支持数据库持久化（替代文件存储）
- [ ] 添加事件过滤器（只录制特定类型）
- [ ] 支持分布式录制（多节点聚合）
- [ ] 添加事件统计和可视化分析
- [ ] 支持导出为标准格式（CSV、Parquet）

## 许可证 (License)

本功能作为窄带分拣系统的一部分，遵循项目主许可证。
