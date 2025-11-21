using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.SignalR;
using ZakYip.NarrowBeltDiverterSorter.Observability.LiveView;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Host.SignalR;

/// <summary>
/// 窄带分拣机实时监控 SignalR Hub
/// 提供实时状态推送给前端
/// </summary>
public class NarrowBeltLiveHub : Hub
{
    private readonly INarrowBeltLiveView _liveView;
    private readonly ILogger<NarrowBeltLiveHub> _logger;

    public NarrowBeltLiveHub(INarrowBeltLiveView liveView, ILogger<NarrowBeltLiveHub> logger)
    {
        _liveView = liveView ?? throw new ArgumentNullException(nameof(liveView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 客户端连接时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端已连接: {ConnectionId}", Context.ConnectionId);
        
        // 发送初始状态
        await SendInitialStateAsync();
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开时触发
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "客户端异常断开: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("客户端已断开: {ConnectionId}", Context.ConnectionId);
        }
        
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 加入格口分组
    /// </summary>
    public async Task JoinChuteGroup(long chuteId)
    {
        var groupName = $"chute:{chuteId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug("客户端 {ConnectionId} 加入格口分组 {ChuteId}", Context.ConnectionId, chuteId);
        
        // 发送当前格口的小车状态
        var cartId = _liveView.GetChuteCart(chuteId);
        await Clients.Caller.SendAsync("ChuteCartChanged", new ChuteCartDto
        {
            ChuteId = chuteId,
            CartId = cartId
        });
    }

    /// <summary>
    /// 离开格口分组
    /// </summary>
    public async Task LeaveChuteGroup(long chuteId)
    {
        var groupName = $"chute:{chuteId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug("客户端 {ConnectionId} 离开格口分组 {ChuteId}", Context.ConnectionId, chuteId);
    }

    /// <summary>
    /// 获取当前状态快照
    /// </summary>
    public async Task GetCurrentSnapshot()
    {
        await SendInitialStateAsync();
    }

    /// <summary>
    /// 发送初始状态给调用者
    /// </summary>
    private async Task SendInitialStateAsync()
    {
        try
        {
            // 主线速度
            var lineSpeed = _liveView.GetLineSpeed();
            await Clients.Caller.SendAsync("LineSpeedUpdated", MapToLineSpeedDto(lineSpeed));

            // 设备状态
            var deviceStatus = _liveView.GetDeviceStatus();
            await Clients.Caller.SendAsync("DeviceStatusUpdated", MapToDeviceStatusDto(deviceStatus));

            // 原点小车
            var originCart = _liveView.GetOriginCart();
            await Clients.Caller.SendAsync("OriginCartChanged", MapToOriginCartDto(originCart));

            // 格口小车映射
            var chuteCarts = _liveView.GetChuteCarts();
            var chuteCartDtos = chuteCarts.Mapping
                .Select(kvp => new ChuteCartDto { ChuteId = kvp.Key, CartId = kvp.Value })
                .ToList();
            await Clients.Caller.SendAsync("ChuteCartsUpdated", chuteCartDtos);

            // 小车布局
            var cartLayout = _liveView.GetCartLayout();
            await Clients.Caller.SendAsync("CartLayoutUpdated", MapToCartLayoutDto(cartLayout));

            // 线体运行状态
            var lineRunState = _liveView.GetLineRunState();
            await Clients.Caller.SendAsync("LineRunStateUpdated", MapToLineRunStateDto(lineRunState));

            // 安全状态
            var safetyState = _liveView.GetSafetyState();
            await Clients.Caller.SendAsync("SafetyStateUpdated", MapToSafetyStateDto(safetyState));

            // 在线包裹
            var onlineParcels = _liveView.GetOnlineParcels();
            await Clients.Caller.SendAsync("OnlineParcelsUpdated", 
                onlineParcels.Select(MapToParcelDto).ToList());

            // 最后创建的包裹
            var lastCreated = _liveView.GetLastCreatedParcel();
            if (lastCreated != null)
            {
                await Clients.Caller.SendAsync("LastCreatedParcelUpdated", MapToParcelDto(lastCreated));
            }

            // 最后落格的包裹
            var lastDiverted = _liveView.GetLastDivertedParcel();
            if (lastDiverted != null)
            {
                await Clients.Caller.SendAsync("LastDivertedParcelUpdated", MapToParcelDto(lastDiverted));
            }

            _logger.LogTrace("已发送初始状态给客户端 {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送初始状态失败: {ConnectionId}", Context.ConnectionId);
        }
    }

    // 映射方法
    private static LineSpeedDto MapToLineSpeedDto(LineSpeedSnapshot snapshot)
    {
        return new LineSpeedDto
        {
            ActualMmps = snapshot.ActualMmps,
            TargetMmps = snapshot.TargetMmps,
            Status = snapshot.Status.ToString(),
            LastUpdatedAt = snapshot.LastUpdatedAt
        };
    }

    private static DeviceStatusDto MapToDeviceStatusDto(DeviceStatusSnapshot snapshot)
    {
        return new DeviceStatusDto
        {
            Status = snapshot.Status.ToString(),
            Message = snapshot.Message,
            LastUpdatedAt = snapshot.LastUpdatedAt
        };
    }

    private static OriginCartDto MapToOriginCartDto(OriginCartSnapshot snapshot)
    {
        return new OriginCartDto
        {
            CartId = snapshot.CartId,
            LastUpdatedAt = snapshot.LastUpdatedAt
        };
    }

    private static CartLayoutDto MapToCartLayoutDto(CartLayoutSnapshot snapshot)
    {
        return new CartLayoutDto
        {
            CartPositions = snapshot.CartPositions
                .Select(cp => new CartPositionDto
                {
                    CartId = cp.CartId,
                    CartIndex = cp.CartIndex,
                    LinearPositionMm = cp.LinearPositionMm,
                    CurrentChuteId = cp.CurrentChuteId
                })
                .ToList(),
            LastUpdatedAt = snapshot.LastUpdatedAt
        };
    }

    private static ParcelDto MapToParcelDto(ParcelSummary summary)
    {
        return new ParcelDto
        {
            ParcelId = summary.ParcelId,
            Barcode = summary.Barcode,
            WeightKg = summary.WeightKg,
            VolumeCubicMm = summary.VolumeCubicMm,
            TargetChuteId = summary.TargetChuteId,
            ActualChuteId = summary.ActualChuteId,
            CreatedAt = summary.CreatedAt,
            DivertedAt = summary.DivertedAt
        };
    }

    private static LineRunStateDto MapToLineRunStateDto(LineRunStateSnapshot snapshot)
    {
        return new LineRunStateDto
        {
            State = snapshot.State,
            Message = snapshot.Message,
            LastUpdatedAt = snapshot.LastUpdatedAt
        };
    }

    private static SafetyStateDto MapToSafetyStateDto(SafetyStateSnapshot snapshot)
    {
        return new SafetyStateDto
        {
            State = snapshot.State,
            Source = snapshot.Source,
            Message = snapshot.Message,
            LastUpdatedAt = snapshot.LastUpdatedAt
        };
    }
}
