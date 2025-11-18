using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Controllers;

/// <summary>
/// 配置管理 API 控制器。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IMainLineOptionsRepository _mainLineRepo;
    private readonly IInfeedLayoutOptionsRepository _infeedLayoutRepo;
    private readonly IUpstreamConnectionOptionsRepository _upstreamConnectionRepo;
    private readonly ILongRunLoadTestOptionsRepository _longRunLoadTestRepo;
    private readonly ILogger<ConfigController> _logger;
    private readonly ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider? _hostConfigProvider;
    private readonly ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.IAppConfigurationStore? _appConfigStore;

    public ConfigController(
        IMainLineOptionsRepository mainLineRepo,
        IInfeedLayoutOptionsRepository infeedLayoutRepo,
        IUpstreamConnectionOptionsRepository upstreamConnectionRepo,
        ILongRunLoadTestOptionsRepository longRunLoadTestRepo,
        ILogger<ConfigController> logger,
        ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider? hostConfigProvider = null,
        ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration.IAppConfigurationStore? appConfigStore = null)
    {
        _mainLineRepo = mainLineRepo ?? throw new ArgumentNullException(nameof(mainLineRepo));
        _infeedLayoutRepo = infeedLayoutRepo ?? throw new ArgumentNullException(nameof(infeedLayoutRepo));
        _upstreamConnectionRepo = upstreamConnectionRepo ?? throw new ArgumentNullException(nameof(upstreamConnectionRepo));
        _longRunLoadTestRepo = longRunLoadTestRepo ?? throw new ArgumentNullException(nameof(longRunLoadTestRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostConfigProvider = hostConfigProvider;
        _appConfigStore = appConfigStore;
    }

    /// <summary>
    /// 获取主线控制选项。
    /// </summary>
    [HttpGet("mainline")]
    [ProducesResponseType(typeof(MainLineControlOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMainLineOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _mainLineRepo.LoadAsync(cancellationToken);
            var dto = new MainLineControlOptionsDto
            {
                TargetSpeedMmps = options.TargetSpeedMmps,
                LoopPeriodMs = (int)options.LoopPeriod.TotalMilliseconds,
                ProportionalGain = options.ProportionalGain,
                IntegralGain = options.IntegralGain,
                DerivativeGain = options.DerivativeGain,
                StableDeadbandMmps = options.StableDeadbandMmps,
                StableHoldSeconds = (int)options.StableHold.TotalSeconds,
                MinOutputMmps = options.MinOutputMmps,
                MaxOutputMmps = options.MaxOutputMmps,
                IntegralLimit = options.IntegralLimit
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取主线控制选项失败");
            return StatusCode(500, new { error = "获取主线控制选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新主线控制选项。
    /// </summary>
    [HttpPut("mainline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMainLineOptions(
        [FromBody] MainLineControlOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 基础验证
            if (dto.TargetSpeedMmps <= 0)
                return BadRequest(new { error = "目标速度必须大于 0" });

            if (dto.LoopPeriodMs <= 0)
                return BadRequest(new { error = "控制循环周期必须大于 0" });

            if (dto.MinOutputMmps < 0)
                return BadRequest(new { error = "最小输出不能小于 0" });

            if (dto.MaxOutputMmps <= dto.MinOutputMmps)
                return BadRequest(new { error = "最大输出必须大于最小输出" });

            var options = new MainLineControlOptions
            {
                TargetSpeedMmps = dto.TargetSpeedMmps,
                LoopPeriod = TimeSpan.FromMilliseconds(dto.LoopPeriodMs),
                ProportionalGain = dto.ProportionalGain,
                IntegralGain = dto.IntegralGain,
                DerivativeGain = dto.DerivativeGain,
                StableDeadbandMmps = dto.StableDeadbandMmps,
                StableHold = TimeSpan.FromSeconds(dto.StableHoldSeconds),
                MinOutputMmps = dto.MinOutputMmps,
                MaxOutputMmps = dto.MaxOutputMmps,
                IntegralLimit = dto.IntegralLimit
            };

            await _mainLineRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("主线控制选项已更新");
            return Ok(new { message = "主线控制选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新主线控制选项失败");
            return StatusCode(500, new { error = "更新主线控制选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取入口布局选项。
    /// </summary>
    [HttpGet("infeed-layout")]
    [ProducesResponseType(typeof(InfeedLayoutOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfeedLayoutOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _infeedLayoutRepo.LoadAsync(cancellationToken);
            var dto = new InfeedLayoutOptionsDto
            {
                InfeedToMainLineDistanceMm = options.InfeedToMainLineDistanceMm,
                TimeToleranceMs = options.TimeToleranceMs,
                CartOffsetCalibration = options.CartOffsetCalibration
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取入口布局选项失败");
            return StatusCode(500, new { error = "获取入口布局选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新入口布局选项。
    /// </summary>
    [HttpPut("infeed-layout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateInfeedLayoutOptions(
        [FromBody] InfeedLayoutOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (dto.InfeedToMainLineDistanceMm <= 0)
                return BadRequest(new { error = "入口到主线距离必须大于 0" });

            if (dto.TimeToleranceMs <= 0)
                return BadRequest(new { error = "时间容差必须大于 0" });

            var options = new InfeedLayoutOptions
            {
                InfeedToMainLineDistanceMm = dto.InfeedToMainLineDistanceMm,
                TimeToleranceMs = dto.TimeToleranceMs,
                CartOffsetCalibration = dto.CartOffsetCalibration
            };

            await _infeedLayoutRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("入口布局选项已更新");
            return Ok(new { message = "入口布局选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新入口布局选项失败");
            return StatusCode(500, new { error = "更新入口布局选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取上游连接选项。
    /// </summary>
    [HttpGet("upstream-connection")]
    [ProducesResponseType(typeof(UpstreamConnectionOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpstreamConnectionOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _upstreamConnectionRepo.LoadAsync(cancellationToken);
            var dto = new UpstreamConnectionOptionsDto
            {
                BaseUrl = options.BaseUrl,
                RequestTimeoutSeconds = options.RequestTimeoutSeconds,
                AuthToken = options.AuthToken
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上游连接选项失败");
            return StatusCode(500, new { error = "获取上游连接选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新上游连接选项。
    /// </summary>
    [HttpPut("upstream-connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUpstreamConnectionOptions(
        [FromBody] UpstreamConnectionOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.BaseUrl))
                return BadRequest(new { error = "BaseUrl 不能为空" });

            if (dto.RequestTimeoutSeconds <= 0)
                return BadRequest(new { error = "请求超时时间必须大于 0" });

            var options = new UpstreamConnectionOptions
            {
                BaseUrl = dto.BaseUrl,
                RequestTimeoutSeconds = dto.RequestTimeoutSeconds,
                AuthToken = dto.AuthToken
            };

            await _upstreamConnectionRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("上游连接选项已更新");
            return Ok(new { message = "上游连接选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新上游连接选项失败");
            return StatusCode(500, new { error = "更新上游连接选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取长跑高负载测试选项。
    /// </summary>
    [HttpGet("long-run-load-test")]
    [ProducesResponseType(typeof(LongRunLoadTestOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLongRunLoadTestOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _longRunLoadTestRepo.LoadAsync(cancellationToken);
            var dto = new LongRunLoadTestOptionsDto
            {
                TargetParcelCount = options.TargetParcelCount,
                ParcelCreationIntervalMs = options.ParcelCreationIntervalMs,
                ChuteCount = options.ChuteCount,
                ChuteWidthMm = options.ChuteWidthMm,
                MainLineSpeedMmps = options.MainLineSpeedMmps,
                CartWidthMm = options.CartWidthMm,
                CartSpacingMm = options.CartSpacingMm,
                CartCount = options.CartCount,
                ExceptionChuteId = options.ExceptionChuteId,
                MinParcelLengthMm = options.MinParcelLengthMm,
                MaxParcelLengthMm = options.MaxParcelLengthMm,
                ForceToExceptionChuteOnConflict = options.ForceToExceptionChuteOnConflict,
                InfeedToDropDistanceMm = options.InfeedToDropDistanceMm,
                InfeedConveyorSpeedMmps = options.InfeedConveyorSpeedMmps
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取长跑测试选项失败");
            return StatusCode(500, new { error = "获取长跑测试选项失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新长跑高负载测试选项。
    /// </summary>
    [HttpPut("long-run-load-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLongRunLoadTestOptions(
        [FromBody] LongRunLoadTestOptionsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 基础验证
            if (dto.TargetParcelCount <= 0)
                return BadRequest(new { error = "目标包裹数必须大于 0" });

            if (dto.ParcelCreationIntervalMs <= 0)
                return BadRequest(new { error = "包裹创建间隔必须大于 0" });

            if (dto.ChuteCount <= 0)
                return BadRequest(new { error = "格口数量必须大于 0" });

            if (dto.ChuteWidthMm <= 0)
                return BadRequest(new { error = "格口宽度必须大于 0" });

            if (dto.MainLineSpeedMmps <= 0)
                return BadRequest(new { error = "主线速度必须大于 0" });

            if (dto.CartWidthMm <= 0)
                return BadRequest(new { error = "小车宽度必须大于 0" });

            if (dto.CartSpacingMm <= 0)
                return BadRequest(new { error = "小车节距必须大于 0" });

            if (dto.CartCount <= 0)
                return BadRequest(new { error = "小车数量必须大于 0" });

            if (dto.MinParcelLengthMm <= 0 || dto.MaxParcelLengthMm <= 0)
                return BadRequest(new { error = "包裹长度必须大于 0" });

            if (dto.MinParcelLengthMm > dto.MaxParcelLengthMm)
                return BadRequest(new { error = "最小包裹长度不能大于最大包裹长度" });

            var options = new LongRunLoadTestOptions
            {
                TargetParcelCount = dto.TargetParcelCount,
                ParcelCreationIntervalMs = dto.ParcelCreationIntervalMs,
                ChuteCount = dto.ChuteCount,
                ChuteWidthMm = dto.ChuteWidthMm,
                MainLineSpeedMmps = dto.MainLineSpeedMmps,
                CartWidthMm = dto.CartWidthMm,
                CartSpacingMm = dto.CartSpacingMm,
                CartCount = dto.CartCount,
                ExceptionChuteId = dto.ExceptionChuteId,
                MinParcelLengthMm = dto.MinParcelLengthMm,
                MaxParcelLengthMm = dto.MaxParcelLengthMm,
                ForceToExceptionChuteOnConflict = dto.ForceToExceptionChuteOnConflict,
                InfeedToDropDistanceMm = dto.InfeedToDropDistanceMm,
                InfeedConveyorSpeedMmps = dto.InfeedConveyorSpeedMmps
            };

            await _longRunLoadTestRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("长跑测试选项已更新");
            return Ok(new { message = "长跑测试选项已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新长跑测试选项失败");
            return StatusCode(500, new { error = "更新长跑测试选项失败", message = ex.Message });
        }
    }

    // ============================================================================
    // 新增统一配置中心端点 - 使用 IHostConfigurationProvider 和 IAppConfigurationStore
    // ============================================================================

    /// <summary>
    /// 获取仿真配置
    /// </summary>
    [HttpGet("simulation")]
    [ProducesResponseType(typeof(SimulationConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSimulationConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_hostConfigProvider == null)
            {
                return StatusCode(500, new { error = "配置提供器未初始化" });
            }

            var config = await _hostConfigProvider.GetSimulationOptionsAsync(cancellationToken);
            var dto = new SimulationConfigurationDto
            {
                TimeBetweenParcelsMs = config.TimeBetweenParcelsMs,
                TotalParcels = config.TotalParcels,
                MinParcelLengthMm = config.MinParcelLengthMm,
                MaxParcelLengthMm = config.MaxParcelLengthMm,
                RandomSeed = config.RandomSeed,
                ParcelTtlSeconds = config.ParcelTtlSeconds
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仿真配置失败");
            return StatusCode(500, new { error = "获取仿真配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新仿真配置
    /// </summary>
    [HttpPut("simulation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSimulationConfiguration(
        [FromBody] SimulationConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_appConfigStore == null)
            {
                return StatusCode(500, new { error = "配置存储未初始化" });
            }

            var config = new NarrowBeltSimulationOptions
            {
                TimeBetweenParcelsMs = dto.TimeBetweenParcelsMs,
                TotalParcels = dto.TotalParcels,
                MinParcelLengthMm = dto.MinParcelLengthMm,
                MaxParcelLengthMm = dto.MaxParcelLengthMm,
                RandomSeed = dto.RandomSeed,
                ParcelTtlSeconds = dto.ParcelTtlSeconds
            };

            await _appConfigStore.SaveAsync("Simulation", config, cancellationToken);
            _logger.LogInformation("仿真配置已更新");
            return Ok(new { message = "仿真配置已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新仿真配置失败");
            return StatusCode(500, new { error = "更新仿真配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取安全配置
    /// </summary>
    [HttpGet("safety")]
    [ProducesResponseType(typeof(SafetyConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSafetyConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_hostConfigProvider == null)
            {
                return StatusCode(500, new { error = "配置提供器未初始化" });
            }

            var config = await _hostConfigProvider.GetSafetyConfigurationAsync(cancellationToken);
            var dto = new SafetyConfigurationDto
            {
                EmergencyStopTimeoutSeconds = config.EmergencyStopTimeoutSeconds,
                AllowAutoRecovery = config.AllowAutoRecovery,
                AutoRecoveryIntervalSeconds = config.AutoRecoveryIntervalSeconds,
                MaxAutoRecoveryAttempts = config.MaxAutoRecoveryAttempts,
                SafetyInputCheckPeriodMs = config.SafetyInputCheckPeriodMs,
                EnableChuteSafetyInterlock = config.EnableChuteSafetyInterlock,
                ChuteSafetyInterlockTimeoutMs = config.ChuteSafetyInterlockTimeoutMs
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取安全配置失败");
            return StatusCode(500, new { error = "获取安全配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新安全配置
    /// </summary>
    [HttpPut("safety")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSafetyConfiguration(
        [FromBody] SafetyConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_appConfigStore == null)
            {
                return StatusCode(500, new { error = "配置存储未初始化" });
            }

            var config = new SafetyConfiguration
            {
                EmergencyStopTimeoutSeconds = dto.EmergencyStopTimeoutSeconds,
                AllowAutoRecovery = dto.AllowAutoRecovery,
                AutoRecoveryIntervalSeconds = dto.AutoRecoveryIntervalSeconds,
                MaxAutoRecoveryAttempts = dto.MaxAutoRecoveryAttempts,
                SafetyInputCheckPeriodMs = dto.SafetyInputCheckPeriodMs,
                EnableChuteSafetyInterlock = dto.EnableChuteSafetyInterlock,
                ChuteSafetyInterlockTimeoutMs = dto.ChuteSafetyInterlockTimeoutMs
            };

            await _appConfigStore.SaveAsync("Safety", config, cancellationToken);
            _logger.LogInformation("安全配置已更新");
            return Ok(new { message = "安全配置已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新安全配置失败");
            return StatusCode(500, new { error = "更新安全配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取录制配置
    /// </summary>
    [HttpGet("recording")]
    [ProducesResponseType(typeof(RecordingConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordingConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_hostConfigProvider == null)
            {
                return StatusCode(500, new { error = "配置提供器未初始化" });
            }

            var config = await _hostConfigProvider.GetRecordingConfigurationAsync(cancellationToken);
            var dto = new RecordingConfigurationDto
            {
                EnabledByDefault = config.EnabledByDefault,
                MaxSessionDurationSeconds = config.MaxSessionDurationSeconds,
                MaxEventsPerSession = config.MaxEventsPerSession,
                RecordingsDirectory = config.RecordingsDirectory,
                AutoCleanupOldRecordings = config.AutoCleanupOldRecordings,
                RecordingRetentionDays = config.RecordingRetentionDays
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取录制配置失败");
            return StatusCode(500, new { error = "获取录制配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新录制配置
    /// </summary>
    [HttpPut("recording")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRecordingConfiguration(
        [FromBody] RecordingConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_appConfigStore == null)
            {
                return StatusCode(500, new { error = "配置存储未初始化" });
            }

            var config = new RecordingConfiguration
            {
                EnabledByDefault = dto.EnabledByDefault,
                MaxSessionDurationSeconds = dto.MaxSessionDurationSeconds,
                MaxEventsPerSession = dto.MaxEventsPerSession,
                RecordingsDirectory = dto.RecordingsDirectory,
                AutoCleanupOldRecordings = dto.AutoCleanupOldRecordings,
                RecordingRetentionDays = dto.RecordingRetentionDays
            };

            await _appConfigStore.SaveAsync("Recording", config, cancellationToken);
            _logger.LogInformation("录制配置已更新");
            return Ok(new { message = "录制配置已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新录制配置失败");
            return StatusCode(500, new { error = "更新录制配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取 SignalR 推送配置
    /// </summary>
    [HttpGet("signalr-push")]
    [ProducesResponseType(typeof(SignalRPushConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignalRPushConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_hostConfigProvider == null)
            {
                return StatusCode(500, new { error = "配置提供器未初始化" });
            }

            var config = await _hostConfigProvider.GetSignalRPushConfigurationAsync(cancellationToken);
            var dto = new SignalRPushConfigurationDto
            {
                LineSpeedPushIntervalMs = config.LineSpeedPushIntervalMs,
                ChuteCartPushIntervalMs = config.ChuteCartPushIntervalMs,
                OriginCartPushIntervalMs = config.OriginCartPushIntervalMs,
                ParcelCreatedPushIntervalMs = config.ParcelCreatedPushIntervalMs,
                ParcelDivertedPushIntervalMs = config.ParcelDivertedPushIntervalMs,
                DeviceStatusPushIntervalMs = config.DeviceStatusPushIntervalMs,
                CartLayoutPushIntervalMs = config.CartLayoutPushIntervalMs,
                OnlineParcelsPushPeriodMs = config.OnlineParcelsPushPeriodMs,
                EnableOnlineParcelsPush = config.EnableOnlineParcelsPush
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 SignalR 推送配置失败");
            return StatusCode(500, new { error = "获取 SignalR 推送配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新 SignalR 推送配置
    /// </summary>
    [HttpPut("signalr-push")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSignalRPushConfiguration(
        [FromBody] SignalRPushConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_appConfigStore == null)
            {
                return StatusCode(500, new { error = "配置存储未初始化" });
            }

            var config = new SignalRPushConfiguration
            {
                LineSpeedPushIntervalMs = dto.LineSpeedPushIntervalMs,
                ChuteCartPushIntervalMs = dto.ChuteCartPushIntervalMs,
                OriginCartPushIntervalMs = dto.OriginCartPushIntervalMs,
                ParcelCreatedPushIntervalMs = dto.ParcelCreatedPushIntervalMs,
                ParcelDivertedPushIntervalMs = dto.ParcelDivertedPushIntervalMs,
                DeviceStatusPushIntervalMs = dto.DeviceStatusPushIntervalMs,
                CartLayoutPushIntervalMs = dto.CartLayoutPushIntervalMs,
                OnlineParcelsPushPeriodMs = dto.OnlineParcelsPushPeriodMs,
                EnableOnlineParcelsPush = dto.EnableOnlineParcelsPush
            };

            await _appConfigStore.SaveAsync("SignalRPush", config, cancellationToken);
            _logger.LogInformation("SignalR 推送配置已更新");
            return Ok(new { message = "SignalR 推送配置已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 SignalR 推送配置失败");
            return StatusCode(500, new { error = "更新 SignalR 推送配置失败", message = ex.Message });
        }
    }
}
