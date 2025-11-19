namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

/// <summary>
/// 格口 IO 端点抽象（一个 IP 对应一个端点）
/// </summary>
public interface IChuteIoEndpoint
{
    /// <summary>
    /// 端点键，用来区分同一品牌下的不同 IP
    /// </summary>
    string EndpointKey { get; }

    /// <summary>
    /// 设置指定通道的状态
    /// </summary>
    /// <param name="channelIndex">通道索引（1..N）</param>
    /// <param name="isOn">是否打开</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask SetChannelAsync(int channelIndex, bool isOn, CancellationToken ct = default);

    /// <summary>
    /// 设置所有通道的状态
    /// </summary>
    /// <param name="isOn">是否打开</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步任务</returns>
    ValueTask SetAllAsync(bool isOn, CancellationToken ct = default);
}
