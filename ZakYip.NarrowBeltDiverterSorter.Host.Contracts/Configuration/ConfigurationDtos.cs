using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration;

/// <summary>
/// 主线控制选项 DTO
/// </summary>
/// <remarks>
/// 配置主线速度控制的PID参数和稳定性判据
/// </remarks>
/// <example>
/// {
///   "targetSpeedMmps": 2000,
///   "loopPeriodMs": 100,
///   "proportionalGain": 1.0,
///   "integralGain": 0.1,
///   "derivativeGain": 0.05,
///   "stableDeadbandMmps": 10,
///   "stableHoldSeconds": 2,
///   "minOutputMmps": 0,
///   "maxOutputMmps": 2500,
///   "integralLimit": 1000
/// }
/// </example>
public sealed record MainLineControlOptionsDto
{
    /// <summary>
    /// 目标速度（毫米/秒）
    /// </summary>
    /// <example>2000</example>
    [Required]
    [Range(0, 10000)]
    public required decimal TargetSpeedMmps { get; init; }

    /// <summary>
    /// 控制循环周期（毫秒）
    /// </summary>
    /// <example>100</example>
    [Required]
    [Range(10, 1000)]
    public required int LoopPeriodMs { get; init; }

    /// <summary>
    /// PID 控制器比例系数 (Kp)
    /// </summary>
    /// <example>1.0</example>
    [Required]
    [Range(0, 100)]
    public required decimal ProportionalGain { get; init; }

    /// <summary>
    /// PID 控制器积分系数 (Ki)
    /// </summary>
    /// <example>0.1</example>
    [Required]
    [Range(0, 10)]
    public required decimal IntegralGain { get; init; }

    /// <summary>
    /// PID 控制器微分系数 (Kd)
    /// </summary>
    /// <example>0.05</example>
    [Required]
    [Range(0, 10)]
    public required decimal DerivativeGain { get; init; }

    /// <summary>
    /// 稳定判据死区（毫米/秒）
    /// </summary>
    /// <remarks>
    /// 当速度误差在此范围内时认为达到稳定
    /// </remarks>
    /// <example>10</example>
    [Required]
    [Range(0, 100)]
    public required decimal StableDeadbandMmps { get; init; }

    /// <summary>
    /// 稳定判据保持时间（秒）
    /// </summary>
    /// <remarks>
    /// 速度必须在死区内持续此时间才认为稳定
    /// </remarks>
    /// <example>2</example>
    [Required]
    [Range(0, 60)]
    public required int StableHoldSeconds { get; init; }

    /// <summary>
    /// 输出限幅最小值（毫米/秒）
    /// </summary>
    /// <example>0</example>
    [Required]
    [Range(0, 10000)]
    public required decimal MinOutputMmps { get; init; }

    /// <summary>
    /// 输出限幅最大值（毫米/秒）
    /// </summary>
    /// <example>2500</example>
    [Required]
    [Range(0, 10000)]
    public required decimal MaxOutputMmps { get; init; }

    /// <summary>
    /// 积分限幅
    /// </summary>
    /// <remarks>
    /// 防止积分饱和的限制值
    /// </remarks>
    /// <example>1000</example>
    [Required]
    [Range(0, 10000)]
    public required decimal IntegralLimit { get; init; }
}

/// <summary>
/// 入口布局选项 DTO
/// </summary>
/// <remarks>
/// 配置入口传感器到主线落车点的距离和时间容差
/// </remarks>
/// <example>
/// {
///   "infeedToMainLineDistanceMm": 1500,
///   "timeToleranceMs": 50,
///   "cartOffsetCalibration": 0
/// }
/// </example>
public sealed record InfeedLayoutOptionsDto
{
    /// <summary>
    /// 入口IO到主线落车点距离（毫米）
    /// </summary>
    /// <example>1500</example>
    [Required]
    [Range(0, 100000)]
    public required decimal InfeedToMainLineDistanceMm { get; init; }

    /// <summary>
    /// 时间容差（毫秒）
    /// </summary>
    /// <remarks>
    /// 小车预测的时间误差容忍范围
    /// </remarks>
    /// <example>50</example>
    [Required]
    [Range(0, 1000)]
    public required int TimeToleranceMs { get; init; }

    /// <summary>
    /// 以小车数计的偏移校准
    /// </summary>
    /// <remarks>
    /// 用于微调小车预测的校准偏移量
    /// </remarks>
    /// <example>0</example>
    [Required]
    [Range(-10, 10)]
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
/// <remarks>
/// 配置窄带分拣机仿真参数，包括包裹生成频率和大小范围
/// </remarks>
/// <example>
/// {
///   "timeBetweenParcelsMs": 300,
///   "totalParcels": 1000,
///   "minParcelLengthMm": 200,
///   "maxParcelLengthMm": 800,
///   "randomSeed": null,
///   "parcelTtlSeconds": 60
/// }
/// </example>
public sealed record SimulationConfigurationDto
{
    /// <summary>
    /// 包裹创建间隔（毫秒）
    /// </summary>
    /// <example>300</example>
    [Range(10, 10000)]
    [DefaultValue(300)]
    public int TimeBetweenParcelsMs { get; set; } = 300;
    
    /// <summary>
    /// 总包裹数量
    /// </summary>
    /// <example>1000</example>
    [Range(1, 1000000)]
    [DefaultValue(1000)]
    public int TotalParcels { get; set; } = 1000;
    
    /// <summary>
    /// 最小包裹长度（毫米）
    /// </summary>
    /// <example>200</example>
    [Range(10, 10000)]
    [DefaultValue(200)]
    public decimal MinParcelLengthMm { get; set; } = 200m;
    
    /// <summary>
    /// 最大包裹长度（毫米）
    /// </summary>
    /// <example>800</example>
    [Range(10, 10000)]
    [DefaultValue(800)]
    public decimal MaxParcelLengthMm { get; set; } = 800m;
    
    /// <summary>
    /// 随机数种子（可选）
    /// </summary>
    /// <remarks>
    /// 用于可重现的随机序列，null表示使用时间种子
    /// </remarks>
    /// <example>null</example>
    public int? RandomSeed { get; set; }
    
    /// <summary>
    /// 包裹生存时间（秒）
    /// </summary>
    /// <remarks>
    /// 超过此时间未完成的包裹将被标记为超时
    /// </remarks>
    /// <example>60</example>
    [Range(10, 3600)]
    [DefaultValue(60)]
    public int ParcelTtlSeconds { get; set; } = 60;
}

/// <summary>
/// 安全配置DTO
/// </summary>
/// <remarks>
/// 配置系统安全相关参数，包括紧急停止、自动恢复和格口安全联锁
/// </remarks>
/// <example>
/// {
///   "emergencyStopTimeoutSeconds": 5,
///   "allowAutoRecovery": false,
///   "autoRecoveryIntervalSeconds": 10,
///   "maxAutoRecoveryAttempts": 3,
///   "safetyInputCheckPeriodMs": 100,
///   "enableChuteSafetyInterlock": true,
///   "chuteSafetyInterlockTimeoutMs": 5000
/// }
/// </example>
public sealed record SafetyConfigurationDto
{
    /// <summary>
    /// 紧急停止超时时间（秒）
    /// </summary>
    /// <remarks>
    /// 系统必须在此时间内响应紧急停止命令
    /// </remarks>
    /// <example>5</example>
    [Range(1, 60)]
    [DefaultValue(5)]
    public int EmergencyStopTimeoutSeconds { get; set; } = 5;
    
    /// <summary>
    /// 允许自动恢复
    /// </summary>
    /// <remarks>
    /// 启用后系统将尝试从某些故障中自动恢复
    /// </remarks>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool AllowAutoRecovery { get; set; } = false;
    
    /// <summary>
    /// 自动恢复尝试间隔（秒）
    /// </summary>
    /// <example>10</example>
    [Range(5, 300)]
    [DefaultValue(10)]
    public int AutoRecoveryIntervalSeconds { get; set; } = 10;
    
    /// <summary>
    /// 最大自动恢复尝试次数
    /// </summary>
    /// <example>3</example>
    [Range(0, 10)]
    [DefaultValue(3)]
    public int MaxAutoRecoveryAttempts { get; set; } = 3;
    
    /// <summary>
    /// 安全输入检查周期（毫秒）
    /// </summary>
    /// <remarks>
    /// 系统检查安全输入（如紧急停止按钮）的频率
    /// </remarks>
    /// <example>100</example>
    [Range(10, 1000)]
    [DefaultValue(100)]
    public int SafetyInputCheckPeriodMs { get; set; } = 100;
    
    /// <summary>
    /// 启用格口安全联锁
    /// </summary>
    /// <remarks>
    /// 启用后格口异常将触发系统安全响应
    /// </remarks>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool EnableChuteSafetyInterlock { get; set; } = true;
    
    /// <summary>
    /// 格口安全联锁超时时间（毫秒）
    /// </summary>
    /// <example>5000</example>
    [Range(100, 60000)]
    [DefaultValue(5000)]
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
