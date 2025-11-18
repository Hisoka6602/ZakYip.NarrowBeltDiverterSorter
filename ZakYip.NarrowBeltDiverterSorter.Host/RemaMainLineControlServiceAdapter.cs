using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// RemaLm1000H 主线控制服务适配器
/// 将 IMainLineControlService 接口适配到 RemaLm1000HMainLineDrive
/// RemaLm1000H 驱动内置 PID 控制，不需要外部控制循环
/// </summary>
internal sealed class RemaMainLineControlServiceAdapter : IMainLineControlService
{
    private readonly ILogger<RemaMainLineControlServiceAdapter> _logger;
    private readonly RemaLm1000HMainLineDrive _drive;
    private bool _isRunning;
    private readonly object _lock = new();

    public RemaMainLineControlServiceAdapter(
        ILogger<RemaMainLineControlServiceAdapter> logger,
        RemaLm1000HMainLineDrive drive)
    {
        _logger = logger;
        _drive = drive;
    }

    /// <inheritdoc/>
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    /// <inheritdoc/>
    public void SetTargetSpeed(decimal targetSpeedMmps)
    {
        // RemaLm1000HMainLineDrive 通过 SetTargetSpeedAsync 设置速度
        // 这个方法在 MainLineControlWorker 中已经直接调用 IMainLineDrive.SetTargetSpeedAsync
        // 所以这里不需要额外操作
        _logger.LogDebug("目标速度将通过 IMainLineDrive.SetTargetSpeedAsync 设置为 {TargetSpeed} mm/s", targetSpeedMmps);
    }

    /// <inheritdoc/>
    public decimal GetTargetSpeed()
    {
        return _drive.TargetSpeedMmps;
    }

    /// <inheritdoc/>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("RemaLm1000H 主线驱动已在运行中");
                return false;
            }
        }

        try
        {
            // 启动 RemaLm1000H 驱动
            await _drive.StartAsync(cancellationToken);
            
            lock (_lock)
            {
                _isRunning = true;
            }
            
            _logger.LogInformation("RemaLm1000H 主线驱动已启动");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 RemaLm1000H 主线驱动失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("RemaLm1000H 主线驱动未在运行");
                return false;
            }
        }

        try
        {
            // 停止 RemaLm1000H 驱动
            await _drive.StopAsync(cancellationToken);
            
            lock (_lock)
            {
                _isRunning = false;
            }
            
            _logger.LogInformation("RemaLm1000H 主线驱动已停止");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 RemaLm1000H 主线驱动失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExecuteControlLoopAsync(CancellationToken cancellationToken = default)
    {
        // RemaLm1000H 驱动内置 PID 控制循环，不需要外部控制
        // 只需要返回成功即可
        return Task.FromResult(true);
    }
}
