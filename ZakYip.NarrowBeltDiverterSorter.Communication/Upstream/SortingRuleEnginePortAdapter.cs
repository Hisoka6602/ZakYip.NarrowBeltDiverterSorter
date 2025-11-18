using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 分拣规则引擎端口适配器
/// 将 Core 层的 ISortingRuleEnginePort 适配到 Communication 层的 ISortingRuleEngineClient
/// </summary>
public class SortingRuleEnginePortAdapter : ISortingRuleEnginePort
{
    private readonly ISortingRuleEngineClient _client;
    private readonly ILogger<SortingRuleEnginePortAdapter> _logger;
    private readonly int _defaultChuteNumber;

    public SortingRuleEnginePortAdapter(
        ISortingRuleEngineClient client,
        ILogger<SortingRuleEnginePortAdapter> logger,
        int defaultChuteNumber = 1)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultChuteNumber = defaultChuteNumber;
    }

    public async ValueTask<int> RequestSortingAsync(SortingRequestEventArgs eventArgs, CancellationToken ct = default)
    {
        try
        {
            // 发布包裹创建消息
            var parcelCreatedMessage = new ParcelCreatedMessage
            {
                ParcelId = eventArgs.ParcelId,
                CartNumber = eventArgs.CartNumber,
                Barcode = eventArgs.Barcode,
                CreatedTime = eventArgs.RequestTime
            };
            await _client.PublishParcelCreatedAsync(parcelCreatedMessage, ct);

            // 如果有 DWS 数据，发布 DWS 数据消息
            if (eventArgs.WeightKg.HasValue || eventArgs.LengthCm.HasValue)
            {
                var dwsDataMessage = new DwsDataMessage
                {
                    ParcelId = eventArgs.ParcelId,
                    Barcode = eventArgs.Barcode,
                    WeightKg = eventArgs.WeightKg,
                    LengthCm = eventArgs.LengthCm,
                    WidthCm = eventArgs.WidthCm,
                    HeightCm = eventArgs.HeightCm,
                    VolumeCm3 = eventArgs.VolumeCm3,
                    ScanTime = eventArgs.RequestTime
                };
                await _client.PublishDwsDataAsync(dwsDataMessage, ct);
            }

            // 对于 Disabled 模式，直接返回默认格口
            // 对于 MQTT/TCP 模式，这里可以等待响应或直接返回默认值
            // TODO: 在真实场景中，可能需要等待规则引擎的响应
            _logger.LogDebug("请求分拣 ParcelId={ParcelId}, 使用默认格口={DefaultChuteNumber}", 
                eventArgs.ParcelId, _defaultChuteNumber);
            
            return _defaultChuteNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求分拣失败 ParcelId={ParcelId}: {Message}", 
                eventArgs.ParcelId, ex.Message);
            return _defaultChuteNumber;
        }
    }

    public async ValueTask NotifySortingResultAckAsync(SortingResultAckEventArgs eventArgs, CancellationToken ct = default)
    {
        try
        {
            var sortingResultMessage = new SortingResultMessage
            {
                ParcelId = eventArgs.ParcelId,
                ChuteNumber = eventArgs.ChuteNumber,
                CartNumber = eventArgs.CartNumber,
                CartCount = eventArgs.CartCount,
                Success = eventArgs.Success,
                ProcessingTimeMs = eventArgs.ProcessingTimeMs,
                FailureReason = eventArgs.FailureReason,
                ReportTime = eventArgs.AckTime
            };

            await _client.PublishSortingResultAsync(sortingResultMessage, ct);
            
            _logger.LogDebug("已通知分拣结果 ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, Success={Success}",
                eventArgs.ParcelId, eventArgs.ChuteNumber, eventArgs.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通知分拣结果失败 ParcelId={ParcelId}: {Message}", 
                eventArgs.ParcelId, ex.Message);
        }
    }
}
