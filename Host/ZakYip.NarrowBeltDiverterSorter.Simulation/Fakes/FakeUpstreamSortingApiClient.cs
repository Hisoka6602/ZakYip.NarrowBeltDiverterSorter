using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟上游分拣系统API客户端
/// 支持三种分拣模式：Normal、FixedChute、RoundRobin
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
        int chuteId;
        string modeDescription;

        switch (_config.SortingMode)
        {
            case SortingMode.FixedChute:
                // 指定落格模式：始终分配到固定格口
                chuteId = _config.FixedChuteId ?? 1;
                modeDescription = "FixedChute";
                break;

            case SortingMode.RoundRobin:
                // 循环格口模式：按格口列表循环分配（跳过强排口）
                var availableChutes = Enumerable.Range(1, _config.NumberOfChutes)
                    .Where(id => id != _config.ForceEjectChuteId)
                    .ToList();

                chuteId = availableChutes[_nextChuteIndex % availableChutes.Count];
                _nextChuteIndex++;
                modeDescription = "RoundRobin";
                break;

            case SortingMode.Normal:
            default:
                // Normal 模式：模拟真实的上游规则引擎（这里简化为循环分配）
                var normalChutes = Enumerable.Range(1, _config.NumberOfChutes)
                    .Where(id => id != _config.ForceEjectChuteId)
                    .ToList();

                chuteId = normalChutes[_nextChuteIndex % normalChutes.Count];
                _nextChuteIndex++;
                modeDescription = "Normal";
                break;
        }

        Console.WriteLine($"[上游系统 - {modeDescription}] 包裹 {request.ParcelId} 分配到格口 {chuteId}");

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
