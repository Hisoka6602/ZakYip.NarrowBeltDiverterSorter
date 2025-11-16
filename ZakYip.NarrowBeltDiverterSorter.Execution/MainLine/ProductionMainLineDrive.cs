using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

/// <summary>
/// 生产环境主驱动线控制实现
/// 包装真实的主线驱动端口（如 RemaMainLineDrive）和反馈端口
/// </summary>
public sealed class ProductionMainLineDrive : IMainLineDrive
{
    private readonly ILogger<ProductionMainLineDrive> _logger;
    private readonly IMainLineDrivePort _drivePort;
    private readonly IMainLineFeedbackPort _feedbackPort;
    private readonly IMainLineStabilityProvider _stabilityProvider;
    private decimal _targetSpeedMmps;
    private readonly object _lock = new();

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
}
