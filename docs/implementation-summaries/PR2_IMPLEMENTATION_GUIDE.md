# PR2 API Refactor - 实施指南

## 已完成核心基础设施 ✅

### 1. API 基础架构
- **ApiResult<T>** 和 **ApiResult**：统一响应模型（`/DTOs/ApiResult.cs`）
- **GlobalExceptionHandlerMiddleware**：全局异常处理（`/Middleware/GlobalExceptionHandlerMiddleware.cs`）
- **ModelValidationFilter**：自动模型验证（`/Filters/ModelValidationFilter.cs`）
- **Program.cs**：已注册中间件和过滤器

### 2. Request/Response DTOs
- **ConfigurationRequests.cs**：配置相关请求 DTOs
  - UpdateMainLineControlOptionsRequest
  - UpdateInfeedLayoutOptionsRequest
  - UpdateUpstreamConnectionOptionsRequest
  - UpdateSimulationConfigurationRequest
  - UpdateFeedingCapacityConfigurationRequest
  - TestParcelRequest
- **UpstreamResponses.cs**：上游相关响应 DTOs
  - TestParcelResponse

### 3. 已重构控制器
- **ConfigController**（部分）：主线、入口布局、上游连接端点
- **UpstreamDiagnosticsController**：测试包裹端点
- **LineController**：已有 [ApiController]，无需修改
- **ParcelsController**：已有 [ApiController]，无需修改
- **SimulationsController**：已有 [ApiController]，需扩展

---

## 待完成工作清单

### 阶段 1: 完成 ConfigController 重构

#### 1.1 创建剩余 Request DTOs
在 `/DTOs/Requests/ConfigurationRequests.cs` 中添加：

```csharp
/// <summary>
/// 更新长跑高负载测试选项请求
/// </summary>
public sealed record UpdateLongRunLoadTestOptionsRequest
{
    [Required]
    [Range(1, 1000000)]
    public required int TargetParcelCount { get; init; }

    [Required]
    [Range(1, 60000)]
    public required int ParcelCreationIntervalMs { get; init; }

    [Required]
    [Range(1, 100)]
    public required int ChuteCount { get; init; }

    [Required]
    [Range(1, 10000)]
    public required int ChuteWidthMm { get; init; }

    [Required]
    [Range(1, 10000)]
    public required decimal MainLineSpeedMmps { get; init; }

    [Required]
    [Range(1, 10000)]
    public required int CartWidthMm { get; init; }

    [Required]
    [Range(1, 10000)]
    public required int CartSpacingMm { get; init; }

    [Required]
    [Range(1, 1000)]
    public required int CartCount { get; init; }

    [Required]
    public required string ExceptionChuteId { get; init; }

    [Required]
    [Range(1, 10000)]
    public required int MinParcelLengthMm { get; init; }

    [Required]
    [Range(1, 10000)]
    public required int MaxParcelLengthMm { get; init; }

    [Required]
    public required bool ForceToExceptionChuteOnConflict { get; init; }

    [Required]
    [Range(1, 100000)]
    public required int InfeedToDropDistanceMm { get; init; }

    [Required]
    [Range(1, 10000)]
    public required decimal InfeedConveyorSpeedMmps { get; init; }
}

/// <summary>
/// 更新安全配置请求
/// </summary>
public sealed record UpdateSafetyConfigurationRequest
{
    [Required]
    [Range(1, 300)]
    public required int EmergencyStopTimeoutSeconds { get; init; }

    [Required]
    public required bool AllowAutoRecovery { get; init; }

    [Range(1, 3600)]
    public int AutoRecoveryIntervalSeconds { get; init; }

    [Range(0, 100)]
    public int MaxAutoRecoveryAttempts { get; init; }

    [Required]
    [Range(10, 10000)]
    public required int SafetyInputCheckPeriodMs { get; init; }

    [Required]
    public required bool EnableChuteSafetyInterlock { get; init; }

    [Range(100, 60000)]
    public int ChuteSafetyInterlockTimeoutMs { get; init; }
}

/// <summary>
/// 更新录制配置请求
/// </summary>
public sealed record UpdateRecordingConfigurationRequest
{
    [Required]
    public required bool EnabledByDefault { get; init; }

    [Range(60, 86400)]
    public int MaxSessionDurationSeconds { get; init; }

    [Range(100, 1000000)]
    public int MaxEventsPerSession { get; init; }

    [Required]
    [StringLength(500)]
    public required string RecordingsDirectory { get; init; }

    [Required]
    public required bool AutoCleanupOldRecordings { get; init; }

    [Range(1, 365)]
    public int RecordingRetentionDays { get; init; }
}

/// <summary>
/// 更新 SignalR 推送配置请求
/// </summary>
public sealed record UpdateSignalRPushConfigurationRequest
{
    [Required]
    [Range(10, 60000)]
    public required int LineSpeedPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int ChuteCartPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int OriginCartPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int ParcelCreatedPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int ParcelDivertedPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int DeviceStatusPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int CartLayoutPushIntervalMs { get; init; }

    [Required]
    [Range(10, 60000)]
    public required int OnlineParcelsPushPeriodMs { get; init; }

    [Required]
    public required bool EnableOnlineParcelsPush { get; init; }
}

/// <summary>
/// 更新 Sorter 配置请求
/// </summary>
public sealed record UpdateSorterConfigurationRequest
{
    [Required]
    public required SorterMainLineConfigRequest MainLine { get; init; }
}

public sealed record SorterMainLineConfigRequest
{
    [Required]
    [RegularExpression("^(Simulation|RemaLm1000H)$")]
    public required string Mode { get; init; }

    [Required]
    public required RemaConnectionConfigRequest Rema { get; init; }
}

public sealed record RemaConnectionConfigRequest
{
    [Required]
    [StringLength(100)]
    public required string PortName { get; init; }

    [Required]
    [Range(1200, 115200)]
    public required int BaudRate { get; init; }

    [Required]
    [Range(5, 8)]
    public required int DataBits { get; init; }

    [Required]
    public required string Parity { get; init; }

    [Required]
    public required string StopBits { get; init; }

    [Required]
    [Range(1, 247)]
    public required byte SlaveAddress { get; init; }

    [Required]
    public required string ReadTimeout { get; init; }

    [Required]
    public required string WriteTimeout { get; init; }

    [Required]
    public required string ConnectTimeout { get; init; }

    [Required]
    [Range(0, 10)]
    public required int MaxRetries { get; init; }

    [Required]
    public required string RetryDelay { get; init; }
}
```

#### 1.2 更新 ConfigController 剩余端点

在 `ConfigController.cs` 中应用相同的模式：
1. 移除 try-catch 块（由全局中间件处理）
2. 移除手动参数验证（由数据注解处理）
3. 使用 Request DTO 作为输入
4. 返回 `ApiResult` 或 `ApiResult<T>`

**示例模式**：
```csharp
[HttpPut("endpoint-name")]
[ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> UpdateSomething(
    [FromBody] UpdateSomethingRequest request,
    CancellationToken cancellationToken)
{
    // 业务逻辑验证（如果需要）
    if (someBusinessRule)
    {
        return BadRequest(DTO.ApiResult.Fail("错误消息", "ErrorCode"));
    }

    // 业务逻辑
    await _repository.SaveAsync(mappedOptions, cancellationToken);
    
    _logger.LogInformation("配置已更新");
    return Ok(DTO.ApiResult.Ok("配置已更新"));
}

[HttpGet("endpoint-name")]
[ProducesResponseType(typeof(DTO.ApiResult<SomeDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetSomething(CancellationToken cancellationToken)
{
    var options = await _repository.LoadAsync(cancellationToken);
    var dto = MapToDto(options);
    return Ok(DTO.ApiResult<SomeDto>.Ok(dto));
}
```

需要更新的端点：
- `GetLongRunLoadTestOptions` / `UpdateLongRunLoadTestOptions`
- `GetSimulationConfiguration` / `UpdateSimulationConfiguration`
- `GetSafetyConfiguration` / `UpdateSafetyConfiguration`
- `GetRecordingConfiguration` / `UpdateRecordingConfiguration`
- `GetSignalRPushConfiguration` / `UpdateSignalRPushConfiguration`
- `GetSorterConfiguration` / `UpdateSorterConfiguration`
- `GetFeedingCapacityConfiguration` / `UpdateFeedingCapacityConfiguration`

---

### 阶段 2: 仿真场景扩展

#### 2.1 设计复杂仿真场景
在 `/ZakYip.NarrowBeltDiverterSorter.Simulation` 项目中：

1. **多车场景**：模拟多个小车同时运行
2. **多包裹场景**：模拟高密度包裹流
3. **异常路径场景**：模拟超时、分拣失败等异常情况

#### 2.2 扩展 SimulationsController
添加端点：
```csharp
[HttpPost("scenarios/{scenarioName}/start")]
public async Task<IActionResult> StartScenario(string scenarioName, CancellationToken cancellationToken)

[HttpPost("scenarios/stop")]
public async Task<IActionResult> StopCurrentScenario(CancellationToken cancellationToken)

[HttpGet("scenarios")]
public IActionResult ListAvailableScenarios()

[HttpGet("status")]
public IActionResult GetSimulationStatus()
```

---

### 阶段 3: 测试

#### 3.1 单元测试
在 `/Tests/ZakYip.NarrowBeltDiverterSorter.Host.Tests` 项目中（需要创建）：

```csharp
public class ConfigControllerTests
{
    [Fact]
    public async Task UpdateMainLineOptions_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateMainLineControlOptionsRequest
        {
            TargetSpeedMmps = 100,
            // ... 其他必需字段
        };

        // Act
        var result = await _controller.UpdateMainLineOptions(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResult = Assert.IsType<ApiResult>(okResult.Value);
        Assert.True(apiResult.Success);
    }

    [Fact]
    public async Task UpdateMainLineOptions_WithInvalidRange_ReturnsBadRequest()
    {
        // 测试参数验证
    }
}
```

#### 3.2 集成测试
在 `/Tests/ZakYip.NarrowBeltDiverterSorter.E2ETests` 项目中：

```csharp
public class ConfigurationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UpdateConfiguration_PersistsChanges()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateMainLineControlOptionsRequest { /* ... */ };

        // Act
        var response = await client.PutAsJsonAsync("/api/config/mainline", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResult>();
        Assert.True(result.Success);

        // Verify persistence
        var getResponse = await client.GetAsync("/api/config/mainline");
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResult<MainLineControlOptionsDto>>();
        Assert.Equal(request.TargetSpeedMmps, getResult.Data.TargetSpeedMmps);
    }
}
```

---

### 阶段 4: 代码审查与安全扫描

#### 4.1 运行 code_review
```bash
# 在完成代码更改后
# 工具会自动分析代码并提供反馈
```

#### 4.2 运行 codeql_checker
```bash
# 在代码审查完成后
# 扫描安全漏洞
```

#### 4.3 处理反馈
- 审查所有 code_review 评论
- 修复相关的安全漏洞
- 重新运行扫描以验证修复

---

### 阶段 5: 文档更新

#### 5.1 更新 API 文档
在 `/Host/ZakYip.NarrowBeltDiverterSorter.Host/API_DOCUMENTATION.md` 中：

1. 更新所有端点的请求/响应示例
2. 说明参数验证规则
3. 列出所有错误代码及其含义
4. 提供配置 API 使用指南

#### 5.2 更新 README
在项目根目录的 `README.md` 中：

1. 说明 API 层的新特性
2. 配置优先级说明（运行时 API 配置 > appsettings.json）
3. 错误处理机制说明

#### 5.3 创建 PR2 实施总结
创建 `PR2_API_REFACTOR_SUMMARY.md` 文档：

```markdown
# PR2 API 重构实施总结

## 概述
本 PR 完成了 API 层的全面重构，实现了统一的错误处理、参数验证和响应格式。

## 核心改进

### 1. 统一响应格式
所有 API 端点现在返回标准化的 `ApiResult` 或 `ApiResult<T>` 响应...

### 2. 自动参数验证
使用数据注解实现声明式验证...

### 3. 全局异常处理
通过中间件捕获所有未处理异常...

## 验收标准完成情况
✅ 所有控制器已标记 [ApiController]
✅ 统一路由前缀 api/[controller]
...
```

---

## 快速开始指南

### 立即开始下一步工作：

1. **完成 ConfigController 重构**
   ```bash
   # 编辑 /DTOs/Requests/ConfigurationRequests.cs
   # 添加上述 Request DTOs
   
   # 编辑 /Controllers/ConfigController.cs
   # 按照示例模式更新剩余端点
   ```

2. **构建并测试**
   ```bash
   dotnet build
   dotnet test
   ```

3. **提交进度**
   ```bash
   git add .
   git commit -m "Complete ConfigController refactoring with Request DTOs"
   git push
   ```

---

## 注意事项

1. **保持一致性**：所有新的或更新的端点都应遵循相同的模式
2. **最小化更改**：只修改必要的代码，不要重构不相关的部分
3. **增量提交**：频繁提交小的、可验证的更改
4. **文档同步**：代码更改时同步更新文档
5. **测试覆盖**：为所有新功能编写测试

---

## 验收标准对照表

### 控制器与端点整合
- [x] 所有 API 控制器标记为 `[ApiController]`
- [x] 使用统一路由前缀
- [x] 端点按领域归类
- [x] 删除重复端点

### 请求模型与参数验证
- [x] POST/PUT/PATCH 使用 Request DTO
- [x] Request DTO 使用数据注解验证
- [ ] 所有配置端点已完成重构（进行中）
- [x] 模型验证失败返回统一错误响应

### 配置 API 化
- [x] 配置可通过 API 读取和更新
- [x] 支持运行时热更新
- [x] 配置持久化到 LiteDB
- [ ] 文档说明配置优先级

### 仿真场景与 API
- [ ] 新增复杂仿真场景
- [ ] SimulationController 扩展
- [ ] 集成测试验证

### API 层时间与异常安全
- [x] 全局异常处理中间件
- [x] 统一错误响应
- [ ] 所有控制器使用本地时间提供器

---

## 需要帮助？

如果在实施过程中遇到问题，请参考：
1. 已完成的代码示例（ConfigController 的前几个端点）
2. UpstreamDiagnosticsController 的重构示例
3. 本文档的模式和示例

祝工作顺利！🚀
