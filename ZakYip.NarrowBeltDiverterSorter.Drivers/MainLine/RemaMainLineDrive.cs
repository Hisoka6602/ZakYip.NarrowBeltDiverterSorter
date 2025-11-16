using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Drivers.MainLine;

/// <summary>
/// 雷马主线驱动器虚拟实现
/// 实现 IMainLineDrivePort 和 IMainLineFeedbackPort
/// 实际驱动器通信将在后续PR中实现
/// </summary>
public class RemaMainLineDrive : IMainLineDrivePort, IMainLineFeedbackPort
{
    private double _currentSpeed = 0.0;
    private double _targetSpeed = 0.0;
    private MainLineStatus _status = MainLineStatus.Stopped;
    private int? _faultCode = null;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<bool> SetTargetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _targetSpeed = speedMmPerSec;
            // 虚拟实现：立即模拟速度响应
            // 真实实现会通过通信协议发送给驱动器
            if (_status == MainLineStatus.Running)
            {
                // 模拟渐进变化，实际值将在反馈端口更新
                _currentSpeed = _targetSpeed * 0.95; // 模拟95%响应
            }
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_status == MainLineStatus.Fault)
            {
                return Task.FromResult(false);
            }

            _status = MainLineStatus.Starting;
            // 虚拟实现：模拟启动过程
            Task.Run(async () =>
            {
                await Task.Delay(500, cancellationToken);
                lock (_lock)
                {
                    _status = MainLineStatus.Running;
                    _currentSpeed = _targetSpeed * 0.9; // 初始90%目标速度
                }
            }, cancellationToken);

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _status = MainLineStatus.Stopping;
            // 虚拟实现：模拟停止过程
            Task.Run(async () =>
            {
                await Task.Delay(300, cancellationToken);
                lock (_lock)
                {
                    _status = MainLineStatus.Stopped;
                    _currentSpeed = 0.0;
                }
            }, cancellationToken);

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _status = MainLineStatus.Stopped;
            _currentSpeed = 0.0;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public double GetCurrentSpeed()
    {
        lock (_lock)
        {
            // 虚拟实现：逐渐接近目标速度
            if (_status == MainLineStatus.Running && _currentSpeed < _targetSpeed)
            {
                var delta = (_targetSpeed - _currentSpeed) * 0.1;
                _currentSpeed += delta;
            }
            return _currentSpeed;
        }
    }

    /// <inheritdoc/>
    public MainLineStatus GetCurrentStatus()
    {
        lock (_lock)
        {
            return _status;
        }
    }

    /// <inheritdoc/>
    public int? GetFaultCode()
    {
        lock (_lock)
        {
            return _faultCode;
        }
    }

    /// <summary>
    /// 模拟设置故障（仅用于测试）
    /// </summary>
    public void SimulateFault(int faultCode)
    {
        lock (_lock)
        {
            _faultCode = faultCode;
            _status = MainLineStatus.Fault;
        }
    }

    /// <summary>
    /// 清除故障（仅用于测试）
    /// </summary>
    public void ClearFault()
    {
        lock (_lock)
        {
            _faultCode = null;
            _status = MainLineStatus.Stopped;
        }
    }
}
