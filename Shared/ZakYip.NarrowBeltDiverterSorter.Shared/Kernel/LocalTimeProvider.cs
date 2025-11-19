namespace ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

/// <summary>
/// 本地时间提供器实现
/// 返回系统本地时间
/// </summary>
public class LocalTimeProvider : ILocalTimeProvider
{
    /// <summary>
    /// 获取当前本地时间
    /// </summary>
    public DateTime Now => DateTime.Now;
}
