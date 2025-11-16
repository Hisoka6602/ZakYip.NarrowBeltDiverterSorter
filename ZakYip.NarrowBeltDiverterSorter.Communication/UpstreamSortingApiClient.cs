using System.Net.Http.Json;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication;

/// <summary>
/// 上游分拣系统API客户端实现
/// 通过HttpClient调用上游系统，路由和字段按照WheelDiverterSorter的API规范对齐
/// </summary>
public class UpstreamSortingApiClient : IUpstreamSortingApiClient
{
    private readonly HttpClient _httpClient;

    public UpstreamSortingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<ParcelRoutingResponseDto> RequestChuteAsync(
        ParcelRoutingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // POST /api/sorting/request-chute
            var response = await _httpClient.PostAsJsonAsync(
                "/api/sorting/request-chute",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ParcelRoutingResponseDto>(
                cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("上游系统返回空响应");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            // 返回失败响应而不是抛出异常，保证业务连续性
            return new ParcelRoutingResponseDto
            {
                ParcelId = request.ParcelId,
                ChuteId = 0,
                IsSuccess = false,
                ErrorMessage = $"请求上游系统失败: {ex.Message}",
                ResponseTime = DateTimeOffset.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task ReportSortingResultAsync(
        SortingResultReportDto report,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // POST /api/sorting/report-result
            var response = await _httpClient.PostAsJsonAsync(
                "/api/sorting/report-result",
                report,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            // 上报失败只记录日志，不影响本地业务流程
            // TODO: 考虑添加日志记录
            throw new InvalidOperationException($"上报分拣结果失败: {ex.Message}", ex);
        }
    }
}
