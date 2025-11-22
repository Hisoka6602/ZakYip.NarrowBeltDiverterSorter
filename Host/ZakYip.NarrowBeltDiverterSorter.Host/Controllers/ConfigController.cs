using Microsoft.AspNetCore.Mvc;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using DTO = ZakYip.NarrowBeltDiverterSorter.Host.DTOs;
using ZakYip.NarrowBeltDiverterSorter.Host.DTOs.Requests;

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
    private readonly IFeedingCapacityOptionsRepository? _feedingCapacityRepo;
    private readonly IFeedingBackpressureController? _backpressureController;
    private readonly ILogger<ConfigController> _logger;
    private readonly ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider? _hostConfigProvider;
    private readonly IAppConfigurationStore? _appConfigStore;
    private readonly ISorterConfigurationProvider? _sorterConfigProvider;
    private readonly IPanelIoLinkageOptionsRepository? _panelIoLinkageRepo;

    public ConfigController(
        IMainLineOptionsRepository mainLineRepo,
        IInfeedLayoutOptionsRepository infeedLayoutRepo,
        IUpstreamConnectionOptionsRepository upstreamConnectionRepo,
        ILongRunLoadTestOptionsRepository longRunLoadTestRepo,
        ILogger<ConfigController> logger,
        ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider? hostConfigProvider = null,
        IAppConfigurationStore? appConfigStore = null,
        ISorterConfigurationProvider? sorterConfigProvider = null,
        IFeedingCapacityOptionsRepository? feedingCapacityRepo = null,
        IFeedingBackpressureController? backpressureController = null,
        IPanelIoLinkageOptionsRepository? panelIoLinkageRepo = null)
    {
        _mainLineRepo = mainLineRepo ?? throw new ArgumentNullException(nameof(mainLineRepo));
        _infeedLayoutRepo = infeedLayoutRepo ?? throw new ArgumentNullException(nameof(infeedLayoutRepo));
        _upstreamConnectionRepo = upstreamConnectionRepo ?? throw new ArgumentNullException(nameof(upstreamConnectionRepo));
        _longRunLoadTestRepo = longRunLoadTestRepo ?? throw new ArgumentNullException(nameof(longRunLoadTestRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostConfigProvider = hostConfigProvider;
        _appConfigStore = appConfigStore;
        _sorterConfigProvider = sorterConfigProvider;
        _feedingCapacityRepo = feedingCapacityRepo;
        _backpressureController = backpressureController;
        _panelIoLinkageRepo = panelIoLinkageRepo;
    }

    /// <summary>
    /// 获取主线控制选项。
    /// </summary>
    [HttpGet("mainline")]
    [ProducesResponseType(typeof(DTO.ApiResult<MainLineControlOptionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMainLineOptions(CancellationToken cancellationToken)
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
        return Ok(DTO.ApiResult<MainLineControlOptionsDto>.Ok(dto));
    }

    /// <summary>
    /// 更新主线控制选项。
    /// </summary>
    [HttpPut("mainline")]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMainLineOptions(
        [FromBody] UpdateMainLineControlOptionsRequest request,
        CancellationToken cancellationToken)
    {
        // 业务逻辑验证
        if (request.MaxOutputMmps <= request.MinOutputMmps)
        {
            return BadRequest(DTO.ApiResult.Fail("最大输出必须大于最小输出", "ValidationError"));
        }

        var options = new MainLineControlOptions
        {
            TargetSpeedMmps = request.TargetSpeedMmps,
            LoopPeriod = TimeSpan.FromMilliseconds(request.LoopPeriodMs),
            ProportionalGain = request.ProportionalGain,
            IntegralGain = request.IntegralGain,
            DerivativeGain = request.DerivativeGain,
            StableDeadbandMmps = request.StableDeadbandMmps,
            StableHold = TimeSpan.FromSeconds(request.StableHoldSeconds),
            MinOutputMmps = request.MinOutputMmps,
            MaxOutputMmps = request.MaxOutputMmps,
            IntegralLimit = request.IntegralLimit
        };

        await _mainLineRepo.SaveAsync(options, cancellationToken);
        _logger.LogInformation("主线控制选项已更新");
        return Ok(DTO.ApiResult.Ok("主线控制选项已更新"));
    }

    /// <summary>
    /// 获取入口布局选项。
    /// </summary>
    [HttpGet("infeed-layout")]
    [ProducesResponseType(typeof(DTO.ApiResult<InfeedLayoutOptionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfeedLayoutOptions(CancellationToken cancellationToken)
    {
        var options = await _infeedLayoutRepo.LoadAsync(cancellationToken);
        var dto = new InfeedLayoutOptionsDto
        {
            InfeedToMainLineDistanceMm = options.InfeedToMainLineDistanceMm,
            TimeToleranceMs = options.TimeToleranceMs,
            CartOffsetCalibration = options.CartOffsetCalibration
        };
        return Ok(DTO.ApiResult<InfeedLayoutOptionsDto>.Ok(dto));
    }

    /// <summary>
    /// 更新入口布局选项。
    /// </summary>
    [HttpPut("infeed-layout")]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateInfeedLayoutOptions(
        [FromBody] UpdateInfeedLayoutOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var options = new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = request.InfeedToMainLineDistanceMm,
            TimeToleranceMs = request.TimeToleranceMs,
            CartOffsetCalibration = request.CartOffsetCalibration
        };

        await _infeedLayoutRepo.SaveAsync(options, cancellationToken);
        _logger.LogInformation("入口布局选项已更新");
        return Ok(DTO.ApiResult.Ok("入口布局选项已更新"));
    }

    /// <summary>
    /// 获取上游连接选项。
    /// </summary>
    [HttpGet("upstream-connection")]
    [ProducesResponseType(typeof(DTO.ApiResult<UpstreamConnectionOptionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpstreamConnectionOptions(CancellationToken cancellationToken)
    {
        var options = await _upstreamConnectionRepo.LoadAsync(cancellationToken);
        var dto = new UpstreamConnectionOptionsDto
        {
            BaseUrl = options.BaseUrl,
            RequestTimeoutSeconds = options.RequestTimeoutSeconds,
            AuthToken = options.AuthToken
        };
        return Ok(DTO.ApiResult<UpstreamConnectionOptionsDto>.Ok(dto));
    }

    /// <summary>
    /// 更新上游连接选项。
    /// </summary>
    [HttpPut("upstream-connection")]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUpstreamConnectionOptions(
        [FromBody] UpdateUpstreamConnectionOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var options = new UpstreamConnectionOptions
        {
            BaseUrl = request.BaseUrl,
            RequestTimeoutSeconds = request.RequestTimeoutSeconds,
            AuthToken = request.AuthToken
        };

        await _upstreamConnectionRepo.SaveAsync(options, cancellationToken);
        _logger.LogInformation("上游连接选项已更新");
        return Ok(DTO.ApiResult.Ok("上游连接选项已更新"));
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

    /// <summary>
    /// 获取 Sorter 配置
    /// </summary>
    /// <remarks>
    /// 获取 Sorter 分拣机配置，包含主线驱动模式和 Rema 串口连接参数
    /// </remarks>
    /// <returns>Sorter 配置对象</returns>
    [HttpGet("sorter")]
    [ProducesResponseType(typeof(SorterConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSorterConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_sorterConfigProvider == null)
            {
                return StatusCode(500, new { error = "Sorter 配置提供器未初始化" });
            }

            var config = await _sorterConfigProvider.LoadAsync(cancellationToken);
            var dto = new SorterConfigurationDto
            {
                MainLine = new SorterMainLineConfigurationDto
                {
                    Mode = config.MainLine.Mode,
                    Rema = new RemaConnectionConfigurationDto
                    {
                        PortName = config.MainLine.Rema.PortName,
                        BaudRate = config.MainLine.Rema.BaudRate,
                        DataBits = config.MainLine.Rema.DataBits,
                        Parity = config.MainLine.Rema.Parity,
                        StopBits = config.MainLine.Rema.StopBits,
                        SlaveAddress = config.MainLine.Rema.SlaveAddress,
                        ReadTimeout = config.MainLine.Rema.ReadTimeout.ToString(),
                        WriteTimeout = config.MainLine.Rema.WriteTimeout.ToString(),
                        ConnectTimeout = config.MainLine.Rema.ConnectTimeout.ToString(),
                        MaxRetries = config.MainLine.Rema.MaxRetries,
                        RetryDelay = config.MainLine.Rema.RetryDelay.ToString()
                    }
                }
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Sorter 配置失败");
            return StatusCode(500, new { error = "获取 Sorter 配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新 Sorter 配置
    /// </summary>
    /// <remarks>
    /// 更新 Sorter 分拣机配置，包含主线驱动模式和 Rema 串口连接参数。
    /// 更新后配置将保存到 LiteDB 并立即生效（热更新）。
    /// 注意：切换主线驱动模式需要重启应用才能生效。
    /// </remarks>
    /// <param name="dto">Sorter 配置 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPut("sorter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSorterConfiguration(
        [FromBody] SorterConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_sorterConfigProvider == null)
            {
                return StatusCode(500, new { error = "Sorter 配置提供器未初始化" });
            }

            // 验证 Mode
            if (string.IsNullOrWhiteSpace(dto.MainLine.Mode))
            {
                return BadRequest(new { error = "Mode 不能为空" });
            }

            if (dto.MainLine.Mode != "Simulation" && dto.MainLine.Mode != "RemaLm1000H")
            {
                return BadRequest(new { error = "Mode 只能是 Simulation 或 RemaLm1000H" });
            }

            // 转换 DTO 到领域模型
            var config = new SorterOptions
            {
                MainLine = new SorterMainLineOptions
                {
                    Mode = dto.MainLine.Mode,
                    Rema = new RemaConnectionOptions
                    {
                        PortName = dto.MainLine.Rema.PortName,
                        BaudRate = dto.MainLine.Rema.BaudRate,
                        DataBits = dto.MainLine.Rema.DataBits,
                        Parity = dto.MainLine.Rema.Parity,
                        StopBits = dto.MainLine.Rema.StopBits,
                        SlaveAddress = dto.MainLine.Rema.SlaveAddress,
                        ReadTimeout = TimeSpan.Parse(dto.MainLine.Rema.ReadTimeout),
                        WriteTimeout = TimeSpan.Parse(dto.MainLine.Rema.WriteTimeout),
                        ConnectTimeout = TimeSpan.Parse(dto.MainLine.Rema.ConnectTimeout),
                        MaxRetries = dto.MainLine.Rema.MaxRetries,
                        RetryDelay = TimeSpan.Parse(dto.MainLine.Rema.RetryDelay)
                    }
                }
            };

            await _sorterConfigProvider.UpdateAsync(config, cancellationToken);
            _logger.LogInformation("Sorter 配置已更新");
            return Ok(new { message = "Sorter 配置已更新（切换主线驱动模式需要重启应用）" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Sorter 配置失败");
            return StatusCode(500, new { error = "更新 Sorter 配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取供包容量配置
    /// </summary>
    /// <remarks>
    /// 获取供包背压与在途包裹容量控制配置，包含当前负载统计信息
    /// </remarks>
    [HttpGet("feeding/capacity")]
    [ProducesResponseType(typeof(DTO.FeedingCapacityConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeedingCapacityConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_feedingCapacityRepo == null)
            {
                return StatusCode(500, new { error = "供包容量配置仓储未初始化" });
            }

            var config = await _feedingCapacityRepo.LoadAsync(cancellationToken);
            
            var dto = new DTO.FeedingCapacityConfigurationDto
            {
                MaxInFlightParcels = config.MaxInFlightParcels,
                MaxUpstreamPendingRequests = config.MaxUpstreamPendingRequests,
                ThrottleMode = config.ThrottleMode.ToString(),
                SlowDownMultiplier = config.SlowDownMultiplier,
                RecoveryThreshold = config.RecoveryThreshold,
                CurrentInFlightParcels = null,
                CurrentUpstreamPendingRequests = null,
                FeedingThrottledCount = null,
                FeedingPausedCount = null
            };

            // 如果背压控制器可用，添加实时统计信息
            if (_backpressureController != null)
            {
                var decision = _backpressureController.CheckFeedingAllowed();
                dto = dto with
                {
                    CurrentInFlightParcels = decision.CurrentInFlightCount,
                    CurrentUpstreamPendingRequests = decision.CurrentUpstreamPendingCount,
                    FeedingThrottledCount = _backpressureController.GetThrottleCount(),
                    FeedingPausedCount = _backpressureController.GetPauseCount()
                };
            }

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取供包容量配置失败");
            return StatusCode(500, new { error = "获取供包容量配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新供包容量配置
    /// </summary>
    /// <remarks>
    /// 更新供包背压与在途包裹容量控制配置。配置立即生效，无需重启。
    /// </remarks>
    [HttpPut("feeding/capacity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFeedingCapacityConfiguration(
        [FromBody] DTO.FeedingCapacityConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_feedingCapacityRepo == null)
            {
                return StatusCode(500, new { error = "供包容量配置仓储未初始化" });
            }

            // 参数验证
            if (dto.MaxInFlightParcels <= 0)
                return BadRequest(new { error = "最大在途包裹数必须大于 0" });

            if (dto.MaxUpstreamPendingRequests <= 0)
                return BadRequest(new { error = "最大上游等待数必须大于 0" });

            if (dto.SlowDownMultiplier <= 1.0)
                return BadRequest(new { error = "降速倍数必须大于 1.0" });

            // 验证节流模式
            if (!Enum.TryParse<FeedingThrottleMode>(dto.ThrottleMode, true, out var throttleMode))
            {
                return BadRequest(new { error = "节流模式无效，有效值为：None, SlowDown, Pause" });
            }

            // 构建配置对象
            var config = new FeedingCapacityOptions
            {
                MaxInFlightParcels = dto.MaxInFlightParcels,
                MaxUpstreamPendingRequests = dto.MaxUpstreamPendingRequests,
                ThrottleMode = throttleMode,
                SlowDownMultiplier = dto.SlowDownMultiplier,
                RecoveryThreshold = dto.RecoveryThreshold
            };

            await _feedingCapacityRepo.SaveAsync(config, cancellationToken);
            _logger.LogInformation("供包容量配置已更新");
            return Ok(new { message = "供包容量配置已更新，新配置立即生效" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新供包容量配置失败");
            return StatusCode(500, new { error = "更新供包容量配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取面板 IO 联动配置
    /// </summary>
    /// <remarks>
    /// 获取启动/停止/首次稳速/稳速后不稳速时的输出通道联动配置
    /// </remarks>
    [HttpGet("panel-io-linkage")]
    [ProducesResponseType(typeof(DTO.ApiResult<PanelIoLinkageConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPanelIoLinkageConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            if (_panelIoLinkageRepo == null)
            {
                return StatusCode(500, new { error = "面板 IO 联动配置仓储未初始化" });
            }

            var options = await _panelIoLinkageRepo.LoadAsync(cancellationToken);
            var dto = new PanelIoLinkageConfigurationDto
            {
                StartFollowOutputChannels = options.StartFollowOutputChannels.ToList(),
                StopFollowOutputChannels = options.StopFollowOutputChannels.ToList(),
                FirstStableSpeedFollowOutputChannels = options.FirstStableSpeedFollowOutputChannels.ToList(),
                UnstableAfterStableFollowOutputChannels = options.UnstableAfterStableFollowOutputChannels.ToList()
            };
            return Ok(DTO.ApiResult<PanelIoLinkageConfigurationDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取面板 IO 联动配置失败");
            return StatusCode(500, new { error = "获取面板 IO 联动配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新面板 IO 联动配置
    /// </summary>
    /// <remarks>
    /// 更新启动/停止/首次稳速/稳速后不稳速时的输出通道联动配置。配置立即生效，无需重启。
    /// </remarks>
    [HttpPut("panel-io-linkage")]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DTO.ApiResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePanelIoLinkageConfiguration(
        [FromBody] PanelIoLinkageConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_panelIoLinkageRepo == null)
            {
                return StatusCode(500, new { error = "面板 IO 联动配置仓储未初始化" });
            }

            // 参数验证
            if (dto.StartFollowOutputChannels == null)
                return BadRequest(DTO.ApiResult.Fail("启动联动输出通道列表不能为 null", "ValidationError"));

            if (dto.StopFollowOutputChannels == null)
                return BadRequest(DTO.ApiResult.Fail("停止联动输出通道列表不能为 null", "ValidationError"));

            if (dto.FirstStableSpeedFollowOutputChannels == null)
                return BadRequest(DTO.ApiResult.Fail("首次稳速联动输出通道列表不能为 null", "ValidationError"));

            if (dto.UnstableAfterStableFollowOutputChannels == null)
                return BadRequest(DTO.ApiResult.Fail("稳速后不稳速联动输出通道列表不能为 null", "ValidationError"));

            // 构建配置对象
            var options = new ZakYip.NarrowBeltDiverterSorter.Core.Configuration.PanelIoLinkageOptions
            {
                StartFollowOutputChannels = dto.StartFollowOutputChannels.ToList(),
                StopFollowOutputChannels = dto.StopFollowOutputChannels.ToList(),
                FirstStableSpeedFollowOutputChannels = dto.FirstStableSpeedFollowOutputChannels.ToList(),
                UnstableAfterStableFollowOutputChannels = dto.UnstableAfterStableFollowOutputChannels.ToList()
            };

            await _panelIoLinkageRepo.SaveAsync(options, cancellationToken);
            _logger.LogInformation("面板 IO 联动配置已更新");
            return Ok(DTO.ApiResult.Ok("面板 IO 联动配置已更新，新配置立即生效"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新面板 IO 联动配置失败");
            return StatusCode(500, new { error = "更新面板 IO 联动配置失败", message = ex.Message });
        }
    }
}
