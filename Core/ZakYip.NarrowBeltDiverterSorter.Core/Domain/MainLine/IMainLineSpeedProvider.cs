namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

/// <summary>
/// 主线速度提供者接口
/// 供落车/吐件规划使用
/// </summary>
public interface IMainLineSpeedProvider
{
    /// <summary>
    /// 获取当前实际速度（mm/s）
    /// </summary>
    decimal CurrentMmps { get; }

    /// <summary>
    /// 速度是否稳定
    /// </summary>
    bool IsSpeedStable { get; }

    /// <summary>
    /// 获取稳定持续时间
    /// </summary>
    TimeSpan StableDuration { get; }
}
