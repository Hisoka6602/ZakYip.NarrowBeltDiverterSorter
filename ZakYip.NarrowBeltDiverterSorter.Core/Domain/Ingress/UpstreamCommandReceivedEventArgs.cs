using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Ingress;

/// <summary>
/// 上游指令接收事件参数
/// 当从上游系统接收到分拣指令时发布
/// </summary>
public class UpstreamCommandReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 接收时间
    /// </summary>
    public required DateTimeOffset ReceivedTime { get; init; }

    /// <summary>
    /// 包裹路由请求
    /// </summary>
    public required ParcelRoutingRequestDto Request { get; init; }

    /// <summary>
    /// 包裹路由响应
    /// </summary>
    public required ParcelRoutingResponseDto Response { get; init; }
}
