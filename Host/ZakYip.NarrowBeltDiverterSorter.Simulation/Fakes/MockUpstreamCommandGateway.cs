using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// Mock 上游指令网关
/// 用于测试和仿真，返回预定义的响应
/// </summary>
public class MockUpstreamCommandGateway : IUpstreamCommandGateway
{
    private readonly int _defaultChuteId;
    private readonly bool _alwaysSucceed;

    public MockUpstreamCommandGateway(int defaultChuteId = 1, bool alwaysSucceed = true)
    {
        _defaultChuteId = defaultChuteId;
        _alwaysSucceed = alwaysSucceed;
    }

    /// <inheritdoc/>
    public Task<ParcelRoutingResponseDto> RequestChuteAsync(
        ParcelRoutingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = new ParcelRoutingResponseDto
        {
            ParcelId = request.ParcelId,
            ChuteId = _alwaysSucceed ? _defaultChuteId : 0,
            IsSuccess = _alwaysSucceed,
            ErrorMessage = _alwaysSucceed ? null : "Mock failure",
            ResponseTime = DateTimeOffset.Now
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public Task ReportSortingResultAsync(
        SortingResultReportDto report,
        CancellationToken cancellationToken = default)
    {
        // Mock implementation - just return success
        return Task.CompletedTask;
    }
}
