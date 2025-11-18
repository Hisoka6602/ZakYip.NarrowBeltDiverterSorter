using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 分拣规则引擎客户端接口
/// 协议无关的客户端抽象，只接受 UpstreamContracts 的消息 DTO
/// </summary>
public interface ISortingRuleEngineClient
{
    /// <summary>
    /// 发布包裹创建消息
    /// </summary>
    ValueTask PublishParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken ct = default);

    /// <summary>
    /// 发布 DWS 数据消息
    /// </summary>
    ValueTask PublishDwsDataAsync(DwsDataMessage message, CancellationToken ct = default);

    /// <summary>
    /// 发布分拣结果消息
    /// </summary>
    ValueTask PublishSortingResultAsync(SortingResultMessage message, CancellationToken ct = default);

    /// <summary>
    /// 订阅分拣请求响应
    /// </summary>
    /// <param name="onSortingRequest">收到分拣请求时的回调，返回分配的格口编号</param>
    /// <param name="ct">取消令牌</param>
    ValueTask SubscribeToSortingRequestsAsync(Func<long, ValueTask<int>> onSortingRequest, CancellationToken ct = default);

    /// <summary>
    /// 连接到规则引擎
    /// </summary>
    ValueTask ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 断开与规则引擎的连接
    /// </summary>
    ValueTask DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 当前连接状态
    /// </summary>
    UpstreamConnectionState ConnectionState { get; }
}

/// <summary>
/// 上游连接状态
/// </summary>
public enum UpstreamConnectionState
{
    /// <summary>
    /// 未连接
    /// </summary>
    Disconnected,

    /// <summary>
    /// 正在连接
    /// </summary>
    Connecting,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected,

    /// <summary>
    /// 错误状态
    /// </summary>
    Error
}
