using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 仿真主线设定点（单例）
/// 用于控制仿真模式下的主线期望速度
/// </summary>
public class SimulationMainLineSetpoint : IMainLineSetpointProvider
{
    private bool _isEnabled;
    private decimal _targetMmps;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get
        {
            lock (_lock)
            {
                return _isEnabled;
            }
        }
    }

    /// <inheritdoc/>
    public decimal TargetMmps
    {
        get
        {
            lock (_lock)
            {
                return _targetMmps;
            }
        }
    }

    /// <summary>
    /// 设置设定点
    /// </summary>
    /// <param name="isEnabled">是否启用</param>
    /// <param name="targetMmps">目标速度（mm/s）</param>
    public void SetSetpoint(bool isEnabled, decimal targetMmps)
    {
        lock (_lock)
        {
            _isEnabled = isEnabled;
            _targetMmps = targetMmps;
        }
    }
}
