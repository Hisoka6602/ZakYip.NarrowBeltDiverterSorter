namespace ZakYip.NarrowBeltDiverterSorter.Observability;

/// <summary>
/// 主线状态变更事件参数
/// </summary>
public record struct MainLineStateChangedEventArgs
{
    /// <summary>
    /// 主线是否运行
    /// </summary>
    public required bool IsRunning { get; init; }

    /// <summary>
    /// 当前速度 (mm/s)
    /// </summary>
    public required double CurrentSpeed { get; init; }

    /// <summary>
    /// 目标速度 (mm/s)
    /// </summary>
    public required double TargetSpeed { get; init; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// 小车环构建完成事件参数
/// </summary>
public record struct CartRingBuiltEventArgs
{
    /// <summary>
    /// 小车数量
    /// </summary>
    public required int CartCount { get; init; }

    /// <summary>
    /// 构建时间
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
