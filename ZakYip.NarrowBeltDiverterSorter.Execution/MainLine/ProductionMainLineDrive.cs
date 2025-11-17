using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

/// <summary>
/// 生产环境主驱动线控制实现
/// 包装真实的主线驱动端口和反馈端口
/// </summary>
public sealed class ProductionMainLineDrive : IMainLineDrive
{
    private readonly ILogger<ProductionMainLineDrive> _logger;
    private readonly IMainLineDrivePort _drivePort;
    private readonly IMainLineFeedbackPort _feedbackPort;
    private readonly IMainLineStabilityProvider _stabilityProvider;
    private decimal _targetSpeedMmps;
    private readonly object _lock = new();
    private bool _isReady;

    public ProductionMainLineDrive(
        ILogger<ProductionMainLineDrive> logger,
        IMainLineDrivePort drivePort,
        IMainLineFeedbackPort feedbackPort,
        IMainLineStabilityProvider stabilityProvider)
    {
        _logger = logger;
        _drivePort = drivePort;
        _feedbackPort = feedbackPort;
        _stabilityProvider = stabilityProvider;
        _targetSpeedMmps = 0m;
        _isReady = false;
    }

    /// <inheritdoc/>
    public async Task SetTargetSpeedAsync(decimal targetSpeedMmps, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _targetSpeedMmps = targetSpeedMmps;
        }
        
        // 调用底层驱动端口设置速度
        await _drivePort.SetTargetSpeedAsync((double)targetSpeedMmps, cancellationToken);
    }

    /// <inheritdoc/>
    public decimal CurrentSpeedMmps
    {
        get
        {
            // 从反馈端口获取当前实际速度
            return (decimal)_feedbackPort.GetCurrentSpeed();
        }
    }

    /// <inheritdoc/>
    public decimal TargetSpeedMmps
    {
        get
        {
            lock (_lock)
            {
                return _targetSpeedMmps;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsSpeedStable
    {
        get
        {
            // 从稳定性提供者获取稳定状态
            return _stabilityProvider.IsStable;
        }
    }

    /// <inheritdoc/>
    public Task<decimal> GetCurrentSpeedAsync(CancellationToken cancellationToken = default)
    {
        // 从反馈端口获取当前实际速度
        return Task.FromResult((decimal)_feedbackPort.GetCurrentSpeed());
    }
    
    /// <inheritdoc/>
    public bool IsReady
    {
        get
        {
            lock (_lock)
            {
                return _isReady;
            }
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("初始化生产主线驱动");
        
        try
        {
            // 启动驱动端口
            var started = await _drivePort.StartAsync(cancellationToken);
            if (!started)
            {
                _logger.LogError("启动主线驱动端口失败");
                lock (_lock)
                {
                    _isReady = false;
                }
                return false;
            }
            
            lock (_lock)
            {
                _isReady = true;
            }
            
            _logger.LogInformation("生产主线驱动初始化完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化生产主线驱动失败");
            
            lock (_lock)
            {
                _isReady = false;
            }
            
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("停机生产主线驱动");
        
        try
        {
            // 设置目标速度为 0
            await SetTargetSpeedAsync(0m, cancellationToken);
            
            // 等待速度降到阈值以下
            var shutdownThreshold = 50m;
            var maxWaitTime = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation("等待主线速度降到 {Threshold} mm/s 以下（最多等待 {MaxWait} 秒）",
                shutdownThreshold, maxWaitTime.TotalSeconds);
            
            while (true)
            {
                var currentSpeed = CurrentSpeedMmps;
                
                if (currentSpeed <= shutdownThreshold)
                {
                    _logger.LogInformation("主线速度已降到 {CurrentSpeed:F1} mm/s，可以安全停机", currentSpeed);
                    break;
                }
                
                var elapsed = DateTime.UtcNow - startTime;
                if (elapsed >= maxWaitTime)
                {
                    _logger.LogWarning(
                        "等待主线减速超时（{Elapsed:F1} 秒），当前速度: {CurrentSpeed:F1} mm/s，强制停机",
                        elapsed.TotalSeconds, currentSpeed);
                    break;
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
            
            // 停止驱动端口
            var stopped = await _drivePort.StopAsync(cancellationToken);
            if (!stopped)
            {
                _logger.LogWarning("停止主线驱动端口失败");
            }
            
            lock (_lock)
            {
                _isReady = false;
            }
            
            _logger.LogInformation("生产主线驱动已安全停机");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停机生产主线驱动失败");
            
            lock (_lock)
            {
                _isReady = false;
            }
            
            return false;
        }
    }
}
