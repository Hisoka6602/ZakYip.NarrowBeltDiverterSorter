namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration;

/// <summary>
/// 主线控制选项 DTO。
/// </summary>
public sealed record MainLineControlOptionsDto
{
    /// <summary>
    /// 目标速度（毫米/秒）。
    /// </summary>
    public required decimal TargetSpeedMmps { get; init; }

    /// <summary>
    /// 控制循环周期（毫秒）。
    /// </summary>
    public required int LoopPeriodMs { get; init; }

    /// <summary>
    /// PID 控制器比例系数。
    /// </summary>
    public required decimal ProportionalGain { get; init; }

    /// <summary>
    /// PID 控制器积分系数。
    /// </summary>
    public required decimal IntegralGain { get; init; }

    /// <summary>
    /// PID 控制器微分系数。
    /// </summary>
    public required decimal DerivativeGain { get; init; }

    /// <summary>
    /// 稳定判据死区（毫米/秒）。
    /// </summary>
    public required decimal StableDeadbandMmps { get; init; }

    /// <summary>
    /// 稳定判据保持时间（秒）。
    /// </summary>
    public required int StableHoldSeconds { get; init; }

    /// <summary>
    /// 输出限幅最小值（毫米/秒）。
    /// </summary>
    public required decimal MinOutputMmps { get; init; }

    /// <summary>
    /// 输出限幅最大值（毫米/秒）。
    /// </summary>
    public required decimal MaxOutputMmps { get; init; }

    /// <summary>
    /// 积分限幅。
    /// </summary>
    public required decimal IntegralLimit { get; init; }
}

/// <summary>
/// 入口布局选项 DTO。
/// </summary>
public sealed record InfeedLayoutOptionsDto
{
    /// <summary>
    /// 入口IO到主线落车点距离（毫米）。
    /// </summary>
    public required decimal InfeedToMainLineDistanceMm { get; init; }

    /// <summary>
    /// 时间容差（毫秒）。
    /// </summary>
    public required int TimeToleranceMs { get; init; }

    /// <summary>
    /// 以小车数计的偏移校准。
    /// </summary>
    public required int CartOffsetCalibration { get; init; }
}

/// <summary>
/// 上游连接选项 DTO。
/// </summary>
public sealed record UpstreamConnectionOptionsDto
{
    /// <summary>
    /// 上游 API 基础 URL。
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// 请求超时时间（秒）。
    /// </summary>
    public required int RequestTimeoutSeconds { get; init; }

    /// <summary>
    /// 认证令牌（可选）。
    /// </summary>
    public string? AuthToken { get; init; }
}

/// <summary>
/// 长跑高负载测试选项 DTO。
/// </summary>
public sealed record LongRunLoadTestOptionsDto
{
    /// <summary>
    /// 目标包裹总数。
    /// </summary>
    public required int TargetParcelCount { get; init; }

    /// <summary>
    /// 包裹创建间隔（毫秒）。
    /// </summary>
    public required int ParcelCreationIntervalMs { get; init; }

    /// <summary>
    /// 格口数量。
    /// </summary>
    public required int ChuteCount { get; init; }

    /// <summary>
    /// 单个格口宽度（毫米）。
    /// </summary>
    public required decimal ChuteWidthMm { get; init; }

    /// <summary>
    /// 主线稳态速度（毫米/秒）。
    /// </summary>
    public required decimal MainLineSpeedMmps { get; init; }

    /// <summary>
    /// 小车宽度（毫米）。
    /// </summary>
    public required decimal CartWidthMm { get; init; }

    /// <summary>
    /// 小车节距（毫米）。
    /// </summary>
    public required decimal CartSpacingMm { get; init; }

    /// <summary>
    /// 小车数量。
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 异常口格口编号。
    /// </summary>
    public required int ExceptionChuteId { get; init; }

    /// <summary>
    /// 包裹长度最小值（毫米）。
    /// </summary>
    public required decimal MinParcelLengthMm { get; init; }

    /// <summary>
    /// 包裹长度最大值（毫米）。
    /// </summary>
    public required decimal MaxParcelLengthMm { get; init; }

    /// <summary>
    /// 当预测无法安全分拣时是否强制改派至异常口。
    /// </summary>
    public required bool ForceToExceptionChuteOnConflict { get; init; }

    /// <summary>
    /// 入口到落车点距离（毫米）。
    /// </summary>
    public required decimal InfeedToDropDistanceMm { get; init; }

    /// <summary>
    /// 入口输送线速度（毫米/秒）。
    /// </summary>
    public required decimal InfeedConveyorSpeedMmps { get; init; }
}

/// <summary>
/// 仿真配置DTO
/// </summary>
public sealed record SimulationConfigurationDto
{
    public int TimeBetweenParcelsMs { get; set; } = 300;
    public int TotalParcels { get; set; } = 1000;
    public decimal MinParcelLengthMm { get; set; } = 200m;
    public decimal MaxParcelLengthMm { get; set; } = 800m;
    public int? RandomSeed { get; set; }
    public int ParcelTtlSeconds { get; set; } = 60;
}

/// <summary>
/// 安全配置DTO
/// </summary>
public sealed record SafetyConfigurationDto
{
    public int EmergencyStopTimeoutSeconds { get; set; } = 5;
    public bool AllowAutoRecovery { get; set; } = false;
    public int AutoRecoveryIntervalSeconds { get; set; } = 10;
    public int MaxAutoRecoveryAttempts { get; set; } = 3;
    public int SafetyInputCheckPeriodMs { get; set; } = 100;
    public bool EnableChuteSafetyInterlock { get; set; } = true;
    public int ChuteSafetyInterlockTimeoutMs { get; set; } = 5000;
}

/// <summary>
/// 录制配置DTO
/// </summary>
public sealed record RecordingConfigurationDto
{
    public bool EnabledByDefault { get; set; } = false;
    public int MaxSessionDurationSeconds { get; set; } = 3600;
    public int MaxEventsPerSession { get; set; } = 100000;
    public string RecordingsDirectory { get; set; } = "Recordings";
    public bool AutoCleanupOldRecordings { get; set; } = false;
    public int RecordingRetentionDays { get; set; } = 30;
}

/// <summary>
/// SignalR 推送配置DTO
/// </summary>
public sealed record SignalRPushConfigurationDto
{
    public int LineSpeedPushIntervalMs { get; set; } = 200;
    public int ChuteCartPushIntervalMs { get; set; } = 100;
    public int OriginCartPushIntervalMs { get; set; } = 100;
    public int ParcelCreatedPushIntervalMs { get; set; } = 50;
    public int ParcelDivertedPushIntervalMs { get; set; } = 50;
    public int DeviceStatusPushIntervalMs { get; set; } = 500;
    public int CartLayoutPushIntervalMs { get; set; } = 500;
    public int OnlineParcelsPushPeriodMs { get; set; } = 1000;
    public bool EnableOnlineParcelsPush { get; set; } = true;
}
