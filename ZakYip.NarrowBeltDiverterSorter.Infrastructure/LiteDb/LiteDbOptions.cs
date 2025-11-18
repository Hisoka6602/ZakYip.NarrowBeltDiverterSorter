namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

/// <summary>
/// LiteDB 配置选项
/// 定义 LiteDB 数据库文件的路径配置
/// </summary>
public sealed record LiteDbOptions
{
    /// <summary>
    /// LiteDB 配置文件相对路径或绝对路径
    /// 如果是相对路径，将以运行目录为根进行解析
    /// </summary>
    public required string FilePath { get; init; }
}
