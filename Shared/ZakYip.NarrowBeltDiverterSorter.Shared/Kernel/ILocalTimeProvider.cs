namespace ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

/// <summary>
/// 本地时间提供器接口
/// 统一的时间获取抽象，用于运行时、通讯、执行路径
/// </summary>
public interface ILocalTimeProvider
{
    /// <summary>
    /// 获取当前本地时间
    /// </summary>
    DateTime Now { get; }
}
