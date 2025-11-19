using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Upstream;

/// <summary>
/// 规则引擎端口适配器
/// 将 Core 层的 ISortingRuleEnginePort 映射到 Communication 层的 ISortingRuleEngineClient
/// </summary>
public class SortingRuleEnginePortAdapter : ISortingRuleEnginePort
{
    private readonly ISortingRuleEngineClient _client;
    private readonly ILogger<SortingRuleEnginePortAdapter> _logger;

    public SortingRuleEnginePortAdapter(
        ISortingRuleEngineClient client,
        ILogger<SortingRuleEnginePortAdapter> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask RequestSortingAsync(SortingRequestEventArgs eventArgs, CancellationToken ct = default)
    {
        try
        {
            // 1. 发送包裹创建消息
            var parcelCreatedMessage = new ParcelCreatedMessage
            {
                ParcelId = eventArgs.ParcelId,
                CartNumber = eventArgs.CartNumber,
                Barcode = eventArgs.Barcode,
                CreatedTime = eventArgs.RequestTime
            };

            await _client.SendParcelCreatedAsync(parcelCreatedMessage, ct);

            // 2. 如果包含 DWS 数据，则发送 DWS 数据消息
            if (HasDwsData(eventArgs))
            {
                var dwsDataMessage = new DwsDataMessage
                {
                    ParcelId = eventArgs.ParcelId,
                    Barcode = eventArgs.Barcode,
                    Weight = eventArgs.Weight,
                    Length = eventArgs.Length,
                    Width = eventArgs.Width,
                    Height = eventArgs.Height,
                    Volume = eventArgs.Volume,
                    MeasuredTime = eventArgs.RequestTime
                };

                await _client.SendDwsDataAsync(dwsDataMessage, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求分拣时发生异常: ParcelId={ParcelId}", eventArgs.ParcelId);
            throw;
        }
    }

    /// <inheritdoc/>
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
                ResultTime = eventArgs.AckTime
            };

            await _client.SendSortingResultAsync(sortingResultMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通知分拣结果时发生异常: ParcelId={ParcelId}", eventArgs.ParcelId);
            throw;
        }
    }

    /// <summary>
    /// 检查事件参数是否包含 DWS 数据
    /// </summary>
    private static bool HasDwsData(SortingRequestEventArgs eventArgs)
    {
        return eventArgs.Weight.HasValue
            || eventArgs.Length.HasValue
            || eventArgs.Width.HasValue
            || eventArgs.Height.HasValue
            || eventArgs.Volume.HasValue;
    }
}
