using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 上游指令网关接口
/// 统一封装与上游系统（WCS/规则引擎/指令API）的交互
/// </summary>
public interface IUpstreamCommandGateway
{
    /// <summary>
    /// 请求格口分配
    /// 向上游系统请求为指定包裹分配格口
    /// </summary>
    /// <param name="request">包裹路由请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包裹路由响应，包含分配的格口ID</returns>
    Task<ParcelRoutingResponseDto> RequestChuteAsync(
        ParcelRoutingRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上报分拣结果
    /// 向上游系统报告包裹的分拣结果（成功或失败）
    /// </summary>
    /// <param name="report">分拣结果报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task ReportSortingResultAsync(
        SortingResultReportDto report,
        CancellationToken cancellationToken = default);
}
