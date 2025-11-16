using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟上游分拣系统API客户端
/// 自动为包裹分配格口（循环分配）
/// </summary>
public class FakeUpstreamSortingApiClient : IUpstreamSortingApiClient
{
    private readonly SimulationConfiguration _config;
    private int _nextChuteIndex = 0;

    public FakeUpstreamSortingApiClient(SimulationConfiguration config)
    {
        _config = config;
    }

    public Task<ParcelRoutingResponseDto> RequestChuteAsync(
        ParcelRoutingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 循环分配格口（跳过强排口）
        var availableChutes = Enumerable.Range(1, _config.NumberOfChutes)
            .Where(id => id != _config.ForceEjectChuteId)
            .ToList();

        var chuteId = availableChutes[_nextChuteIndex % availableChutes.Count];
        _nextChuteIndex++;

        Console.WriteLine($"[上游系统] 包裹 {request.ParcelId} 分配到格口 {chuteId}");

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
        Console.WriteLine($"[上游系统] 包裹 {report.ParcelId} 分拣{status} - 格口 {report.ChuteId}");
        return Task.CompletedTask;
    }
}
