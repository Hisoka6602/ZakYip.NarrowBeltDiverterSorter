using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 规则引擎客户端接口
/// 协议无关的客户端抽象，只接受 UpstreamContracts 的消息 DTO
/// </summary>
/// <remarks>
/// 此接口不暴露任何协议细节（MQTT/TCP/HTTP），
/// 由具体实现类决定使用何种协议与规则引擎通信
/// </remarks>
public interface ISortingRuleEngineClient : IDisposable
{
    /// <summary>
    /// 客户端是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接到上游规则引擎
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否连接成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开与上游规则引擎的连接
    /// </summary>
    /// <returns>异步任务</returns>
    Task DisconnectAsync();

    /// <summary>
    /// 发送包裹创建消息
    /// </summary>
    /// <param name="message">包裹创建消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功发送</returns>
    Task<bool> SendParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送 DWS 数据消息
    /// </summary>
    /// <param name="message">DWS 数据消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功发送</returns>
    Task<bool> SendDwsDataAsync(DwsDataMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送分拣结果消息
    /// </summary>
    /// <param name="message">分拣结果消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功发送</returns>
    Task<bool> SendSortingResultAsync(SortingResultMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分拣结果接收事件（从上游规则引擎接收到分拣结果时触发）
    /// </summary>
    event EventHandler<SortingResultMessage>? SortingResultReceived;
}
