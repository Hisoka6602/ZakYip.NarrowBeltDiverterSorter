# 配置中心 API 文档

## 概述

本文档描述了窄带分拣机系统的配置中心 API。所有配置通过 API 修改后会持久化到 LiteDB 数据库中。

## 基础信息

- **基础 URL**: `http://localhost:5000` (开发环境)
- **API 前缀**: `/api`
- **内容类型**: `application/json`
- **Swagger UI**: `/swagger` (仅开发环境)

## 配置 API 端点

### 1. 主线控制配置

#### GET /api/config/mainline
获取当前主线控制配置。

**响应示例**:
```json
{
  "targetSpeedMmps": 1000,
  "loopPeriodMs": 100,
  "proportionalGain": 1.0,
  "integralGain": 0.1,
  "derivativeGain": 0.01,
  "stableDeadbandMmps": 10,
  "stableHoldSeconds": 2,
  "minOutputMmps": 0,
  "maxOutputMmps": 5000,
  "integralLimit": 1000
}
```

#### PUT /api/config/mainline
更新主线控制配置。

**请求体**: 与 GET 响应格式相同

**验证规则**:
- `targetSpeedMmps` > 0
- `loopPeriodMs` > 0
- `minOutputMmps` >= 0
- `maxOutputMmps` > `minOutputMmps`

---

### 2. 入口布局配置

#### GET /api/config/infeed-layout
获取当前入口布局配置。

**响应示例**:
```json
{
  "infeedToMainLineDistanceMm": 5000,
  "timeToleranceMs": 100,
  "cartOffsetCalibration": 0
}
```

#### PUT /api/config/infeed-layout
更新入口布局配置。

**请求体**: 与 GET 响应格式相同

**验证规则**:
- `infeedToMainLineDistanceMm` > 0
- `timeToleranceMs` > 0

---

### 3. 上游连接配置

#### GET /api/config/upstream-connection
获取上游系统连接配置。

**响应示例**:
```json
{
  "baseUrl": "http://upstream-system:8080",
  "requestTimeoutSeconds": 30,
  "authToken": "Bearer xxx..."
}
```

#### PUT /api/config/upstream-connection
更新上游连接配置。

**请求体**: 与 GET 响应格式相同

**验证规则**:
- `baseUrl` 不能为空
- `requestTimeoutSeconds` > 0
- `authToken` 可选

---

### 4. 长跑高负载测试配置

#### GET /api/config/long-run-load-test
获取长跑测试配置。

**响应示例**:
```json
{
  "targetParcelCount": 1000,
  "parcelCreationIntervalMs": 300,
  "chuteCount": 60,
  "chuteWidthMm": 1000,
  "mainLineSpeedMmps": 1000,
  "cartWidthMm": 200,
  "cartSpacingMm": 500,
  "cartCount": 60,
  "exceptionChuteId": 60,
  "minParcelLengthMm": 200,
  "maxParcelLengthMm": 1000,
  "forceToExceptionChuteOnConflict": true,
  "infeedToDropDistanceMm": 2000,
  "infeedConveyorSpeedMmps": 1000
}
```

#### PUT /api/config/long-run-load-test
更新长跑测试配置。

**请求体**: 与 GET 响应格式相同

**验证规则**:
- 所有数量/长度/速度参数必须 > 0
- `minParcelLengthMm` <= `maxParcelLengthMm`

---

## 仿真 API 端点

### POST /api/simulations/long-run/start-from-panel
通过模拟电柜面板启动按钮触发长跑仿真。

此端点模拟用户在物理电柜面板上按下"启动"按钮，然后根据当前配置启动长时间高负载仿真（默认 300ms 间隔生成 1000 个包裹）。

**请求**: 无请求体

**响应示例**:
```json
{
  "runId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "triggered",
  "message": "长跑仿真已通过面板启动按钮触发",
  "configuration": {
    "targetParcelCount": 1000,
    "parcelCreationIntervalMs": 300,
    "chuteCount": 60,
    "mainLineSpeedMmps": 1000
  }
}
```

**工作流程**:
1. 加载所有相关配置（主线、长跑测试等）
2. 验证系统状态（主线就绪、无故障等）
3. 模拟电柜面板启动按钮被按下
4. 触发后台长跑仿真任务
5. 返回仿真运行 ID

**注意事项**:
- 仿真为异步执行，API 立即返回
- 使用返回的 `runId` 可以后续查询仿真状态（待实现）
- 仿真结果会输出到 `.md` 和 `.log` 文件

---

## 使用示例

### 完整配置 -> 启动仿真流程

```bash
# 1. 配置长跑测试参数
curl -X PUT http://localhost:5000/api/config/long-run-load-test \
  -H "Content-Type: application/json" \
  -d '{
    "targetParcelCount": 1000,
    "parcelCreationIntervalMs": 300,
    "chuteCount": 60,
    "chuteWidthMm": 1000,
    "mainLineSpeedMmps": 1000,
    "cartWidthMm": 200,
    "cartSpacingMm": 500,
    "cartCount": 60,
    "exceptionChuteId": 60,
    "minParcelLengthMm": 200,
    "maxParcelLengthMm": 1000,
    "forceToExceptionChuteOnConflict": true,
    "infeedToDropDistanceMm": 2000,
    "infeedConveyorSpeedMmps": 1000
  }'

# 2. 配置主线控制参数
curl -X PUT http://localhost:5000/api/config/mainline \
  -H "Content-Type: application/json" \
  -d '{
    "targetSpeedMmps": 1000,
    "loopPeriodMs": 100,
    "proportionalGain": 1.0,
    "integralGain": 0.1,
    "derivativeGain": 0.01,
    "stableDeadbandMmps": 10,
    "stableHoldSeconds": 2,
    "minOutputMmps": 0,
    "maxOutputMmps": 5000,
    "integralLimit": 1000
  }'

# 3. 通过面板启动按钮触发仿真
curl -X POST http://localhost:5000/api/simulations/long-run/start-from-panel

# 响应示例：
# {
#   "runId": "abc-123",
#   "status": "triggered",
#   "message": "长跑仿真已通过面板启动按钮触发",
#   "configuration": {...}
# }
```

### 快速验证配置

```bash
# 查看当前所有配置
curl http://localhost:5000/api/config/mainline
curl http://localhost:5000/api/config/infeed-layout
curl http://localhost:5000/api/config/upstream-connection
curl http://localhost:5000/api/config/long-run-load-test
```

---

## 错误处理

所有 API 端点遵循统一的错误响应格式：

```json
{
  "error": "错误类型描述",
  "message": "详细错误信息"
}
```

**常见 HTTP 状态码**:
- `200 OK`: 请求成功
- `400 Bad Request`: 请求参数验证失败
- `500 Internal Server Error`: 服务器内部错误

---

## 技术细节

### 配置持久化
- 所有配置通过 LiteDB 存储在 `sorter-config.db` 文件中
- 配置修改立即写入数据库
- 系统重启后自动加载最后保存的配置
- LiteDB 仅用于配置存储，不存储日志

### 仿真输出
- 日志文件：`*.log`
- E2E 报告：`LongRunLoadTest_yyyyMMddHHmmss.md`
- 包含包裹生命周期、分拣统计等详细信息

### 面板按钮联动
- API 模拟真实物理电柜面板操作
- 触发与硬件按钮相同的启动流程
- 适用于远程测试和自动化场景

---

## 后续扩展

计划中的功能：
- [ ] 仿真状态查询 API (`GET /api/simulations/long-run/{runId}`)
- [ ] 仿真停止 API (`POST /api/simulations/long-run/{runId}/stop`)
- [ ] 格口配置 API
- [ ] 实时仿真进度推送 (WebSocket)
- [ ] 配置版本管理
- [ ] 配置导入/导出
