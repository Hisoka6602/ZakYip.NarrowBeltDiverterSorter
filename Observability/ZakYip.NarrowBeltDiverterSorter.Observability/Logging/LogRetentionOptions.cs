namespace ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

/// <summary>
/// 日志保留配置选项
/// </summary>
public sealed class LogRetentionOptions
{
    /// <summary>
    /// 日志保留天数，默认 3 天
    /// </summary>
    public int RetentionDays { get; set; } = 3;

    /// <summary>
    /// 日志文件目录路径
    /// </summary>
    public string? LogDirectory { get; set; }
}
