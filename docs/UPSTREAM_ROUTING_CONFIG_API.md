# 上游路由配置 API 使用指南

本文档介绍如何通过 REST API 管理上游路由配置（TTL、异常格口等参数）。

## API 端点

### 获取当前配置

**请求**
```http
GET /api/settings/upstream-routing
```

**响应**
```json
{
  "upstreamResultTtlSeconds": 30,
  "errorChuteId": 9999
}
```

**说明**
- `upstreamResultTtlSeconds`: 上游结果超时时间（秒），范围：1-300
- `errorChuteId`: 异常格口ID，范围：1-99999

### 更新配置

**请求**
```http
PUT /api/settings/upstream-routing
Content-Type: application/json

{
  "upstreamResultTtlSeconds": 45,
  "errorChuteId": 8888
}
```

**响应**
```json
{
  "message": "上游路由配置已更新",
  "upstreamResultTtlSeconds": 45,
  "errorChuteId": 8888
}
```

## 使用示例

### 使用 curl

**获取配置**
```bash
curl -X GET http://localhost:5000/api/settings/upstream-routing
```

**更新配置**
```bash
curl -X PUT http://localhost:5000/api/settings/upstream-routing \
  -H "Content-Type: application/json" \
  -d '{
    "upstreamResultTtlSeconds": 60,
    "errorChuteId": 7777
  }'
```

### 使用 PowerShell

**获取配置**
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5000/api/settings/upstream-routing"
```

**更新配置**
```powershell
$body = @{
    upstreamResultTtlSeconds = 60
    errorChuteId = 7777
} | ConvertTo-Json

Invoke-RestMethod -Method Put -Uri "http://localhost:5000/api/settings/upstream-routing" `
    -ContentType "application/json" `
    -Body $body
```

### 使用 C# HttpClient

```csharp
using System.Net.Http.Json;

// 获取配置
var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
var settings = await httpClient.GetFromJsonAsync<UpstreamRoutingSettingsDto>(
    "/api/settings/upstream-routing");

Console.WriteLine($"当前 TTL: {settings.UpstreamResultTtlSeconds}秒");
Console.WriteLine($"异常格口: {settings.ErrorChuteId}");

// 更新配置
var newSettings = new UpstreamRoutingSettingsDto
{
    UpstreamResultTtlSeconds = 60,
    ErrorChuteId = 7777
};

var response = await httpClient.PutAsJsonAsync(
    "/api/settings/upstream-routing", newSettings);

if (response.IsSuccessStatusCode)
{
    Console.WriteLine("配置更新成功");
}
```

## 配置热更新机制

### 工作原理

1. **更新触发**：通过 PUT 请求更新配置
2. **持久化**：配置立即保存到 LiteDB 数据库
3. **内存更新**：配置提供器（ConfigProvider）更新内存缓存
4. **事件通知**：触发 `ConfigChanged` 事件通知订阅者
5. **立即生效**：新创建的包裹使用新配置

### 生效范围

**立即生效的场景：**
- ✅ 新创建的包裹使用新的 TTL 计算超时时间
- ✅ 新发生的超时使用新的异常格口ID
- ✅ 运行中的组件通过 `GetCurrentOptions()` 获取最新配置

**不受影响的场景：**
- ❌ 已创建的包裹保持原有的超时时间
- ❌ 已经完成的包裹不会重新处理

### 示例场景

**场景1：调整 TTL 应对高峰期**

```bash
# 高峰期到来，延长 TTL 防止超时
curl -X PUT http://localhost:5000/api/settings/upstream-routing \
  -H "Content-Type: application/json" \
  -d '{"upstreamResultTtlSeconds": 90, "errorChuteId": 9999}'

# 高峰期结束，恢复正常 TTL
curl -X PUT http://localhost:5000/api/settings/upstream-routing \
  -H "Content-Type: application/json" \
  -d '{"upstreamResultTtlSeconds": 30, "errorChuteId": 9999}'
```

**场景2：切换异常格口**

```bash
# 主异常格口满了，切换到备用格口
curl -X PUT http://localhost:5000/api/settings/upstream-routing \
  -H "Content-Type: application/json" \
  -d '{"upstreamResultTtlSeconds": 30, "errorChuteId": 8888}'
```

**场景3：测试超时行为**

```bash
# 设置很短的 TTL 用于测试
curl -X PUT http://localhost:5000/api/settings/upstream-routing \
  -H "Content-Type: application/json" \
  -d '{"upstreamResultTtlSeconds": 5, "errorChuteId": 9999}'

# 测试完成后恢复
curl -X PUT http://localhost:5000/api/settings/upstream-routing \
  -H "Content-Type: application/json" \
  -d '{"upstreamResultTtlSeconds": 30, "errorChuteId": 9999}'
```

## 验证配置更新

### 方法1：查询 API

```bash
curl -X GET http://localhost:5000/api/settings/upstream-routing
```

### 方法2：查看日志

更新配置时会产生日志：
```
[Information] 上游路由配置已更新：TTL=60秒，异常格口=7777
```

### 方法3：监控包裹超时行为

观察新创建的包裹是否按新 TTL 超时。

## 错误处理

### 验证错误（400 Bad Request）

**请求体缺少必填字段**
```json
{
  "errors": {
    "UpstreamResultTtlSeconds": ["The UpstreamResultTtlSeconds field is required."]
  }
}
```

**TTL 超出范围**
```json
{
  "errors": {
    "UpstreamResultTtlSeconds": ["字段 UpstreamResultTtlSeconds 必须在 1 和 300 之间"]
  }
}
```

### 服务器错误（500 Internal Server Error）

```json
{
  "error": "更新配置失败",
  "message": "详细错误信息"
}
```

## 监控和可观测性

### 推荐监控指标

1. **配置变更频率**：监控配置修改的频率和时间
2. **超时率变化**：观察 TTL 调整后超时率的变化
3. **异常格口使用率**：监控异常格口的包裹数量

### 日志记录

系统会记录以下关键日志：

- 配置获取：`返回上游路由配置：TTL={TtlSeconds}秒，异常格口={ErrorChuteId}`
- 配置更新：`上游路由配置已更新：TTL={TtlSeconds}秒，异常格口={ErrorChuteId}`
- 超时处理：`包裹 {ParcelId} 上游分配超时，已分配到异常格口 {ErrorChuteId}`

## 最佳实践

1. **谨慎调整 TTL**
   - 过短：增加超时率，影响分拣效率
   - 过长：延迟异常处理，占用系统资源

2. **备用异常格口**
   - 预先配置多个异常格口
   - 在需要时快速切换

3. **变更记录**
   - 记录配置变更的原因和时间
   - 便于问题追踪和回溯

4. **测试验证**
   - 在低峰期测试配置变更
   - 观察系统行为是否符合预期

5. **监控告警**
   - 设置超时率告警阈值
   - 异常格口使用率告警

## Swagger 文档

启动应用后，访问 Swagger UI 查看完整的 API 文档：

```
http://localhost:5000/swagger
```

在 Swagger UI 中可以：
- 查看详细的 API 文档
- 测试 API 端点
- 查看请求/响应示例
- 了解参数验证规则
