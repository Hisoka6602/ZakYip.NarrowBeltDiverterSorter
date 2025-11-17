using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Options;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 长跑场景专用的上游分拣系统API客户端，支持随机格口分配。
/// </summary>
public class LongRunRandomUpstreamClient : IUpstreamSortingApiClient
{
    private readonly LongRunLoadTestOptions _options;
    private readonly ILogger<LongRunRandomUpstreamClient> _logger;
    private readonly Random _random = new Random();

    public LongRunRandomUpstreamClient(
        LongRunLoadTestOptions options,
        ILogger<LongRunRandomUpstreamClient> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<ParcelRoutingResponseDto> RequestChuteAsync(
        ParcelRoutingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 随机分配格口 ID ∈ [1, ChuteCount]
        // 注意：包括异常口在内的所有格口都可能被随机分配
        int chuteId = _random.Next(1, _options.ChuteCount + 1);

        _logger.LogDebug("[上游系统 - Random] 包裹 {ParcelId} 随机分配到格口 {ChuteId}", 
            request.ParcelId, chuteId);

        var response = new ParcelRoutingResponseDto
        {
            ParcelId = request.ParcelId,
            ChuteId = chuteId,
            IsSuccess = true
        };

        return Task.FromResult(response);
    }

    public Task ReportSortingResultAsync(
        SortingResultReportDto report,
        CancellationToken cancellationToken = default)
    {
        var status = report.IsSuccess ? "成功" : $"失败({report.FailureReason})";
        _logger.LogDebug("[上游系统] 包裹 {ParcelId} 分拣{Status} - 格口 {ChuteId}", 
            report.ParcelId, status, report.ChuteId);
        return Task.CompletedTask;
    }
}
