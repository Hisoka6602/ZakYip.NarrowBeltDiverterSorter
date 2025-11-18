using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Upstream;

/// <summary>
/// 上游指令网关实现
/// 封装对 IUpstreamSortingApiClient 的调用，提供统一的上游指令接口
/// </summary>
public class UpstreamCommandGateway : IUpstreamCommandGateway
{
    private readonly IUpstreamSortingApiClient _upstreamClient;

    public UpstreamCommandGateway(IUpstreamSortingApiClient upstreamClient)
    {
        _upstreamClient = upstreamClient ?? throw new ArgumentNullException(nameof(upstreamClient));
    }

    /// <inheritdoc/>
    public Task<ParcelRoutingResponseDto> RequestChuteAsync(
        ParcelRoutingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _upstreamClient.RequestChuteAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task ReportSortingResultAsync(
        SortingResultReportDto report,
        CancellationToken cancellationToken = default)
    {
        return _upstreamClient.ReportSortingResultAsync(report, cancellationToken);
    }
}
