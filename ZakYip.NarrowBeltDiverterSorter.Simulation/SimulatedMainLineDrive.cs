using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 仿真主驱动线控制实现
/// 包装 FakeMainLineDrivePort 和 FakeMainLineFeedbackPort
/// 通过定时器/Loop 模拟速度逼近目标
/// </summary>
public sealed class SimulatedMainLineDrive : IMainLineDrive
{
    private readonly ILogger<SimulatedMainLineDrive> _logger;
    private readonly FakeMainLineDrivePort _drivePort;
    private readonly FakeMainLineFeedbackPort _feedbackPort;
    private readonly IMainLineStabilityProvider _stabilityProvider;
    private decimal _targetSpeedMmps;
    private readonly object _lock = new();

    public SimulatedMainLineDrive(
        ILogger<SimulatedMainLineDrive> logger,
        FakeMainLineDrivePort drivePort,
        FakeMainLineFeedbackPort feedbackPort,
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
        
        // 调用底层 FakeMainLineDrivePort 设置速度
        await _drivePort.SetTargetSpeedAsync((double)targetSpeedMmps, cancellationToken);
    }

    /// <inheritdoc/>
    public decimal CurrentSpeedMmps
    {
        get
        {
            // 从 FakeMainLineFeedbackPort 获取当前实际速度
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
}
