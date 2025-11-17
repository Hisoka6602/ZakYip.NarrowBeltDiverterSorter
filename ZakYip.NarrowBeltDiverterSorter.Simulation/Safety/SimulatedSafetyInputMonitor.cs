using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Safety;

/// <summary>
/// 仿真安全输入监控器
/// 用于仿真模式下模拟安全输入信号
/// </summary>
public class SimulatedSafetyInputMonitor : ISafetyInputMonitor
{
    private readonly ILogger<SimulatedSafetyInputMonitor> _logger;
    private readonly Dictionary<string, bool> _safetyInputStates = new();
    private bool _isMonitoring;

    public SimulatedSafetyInputMonitor(ILogger<SimulatedSafetyInputMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 初始化默认安全输入状态（全部为安全状态）
        _safetyInputStates["EmergencyStop1"] = true;
        _safetyInputStates["SafetyDoor1"] = true;
        _safetyInputStates["DriveFault1"] = true;
        _safetyInputStates["Interlock1"] = true;
    }

    public event EventHandler<SafetyInputChangedEventArgs>? SafetyInputChanged;

    public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("安全输入监控已在运行");
            return Task.CompletedTask;
        }

        _isMonitoring = true;
        _logger.LogInformation("仿真安全输入监控已启动");
        
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (!_isMonitoring)
        {
            _logger.LogWarning("安全输入监控未在运行");
            return Task.CompletedTask;
        }

        _isMonitoring = false;
        _logger.LogInformation("仿真安全输入监控已停止");
        
        return Task.CompletedTask;
    }

    public IDictionary<string, bool> GetCurrentSafetyInputStates()
    {
        return new Dictionary<string, bool>(_safetyInputStates);
    }

    /// <summary>
    /// 模拟触发急停
    /// </summary>
    public void SimulateEmergencyStop(bool pressed)
    {
        SetSafetyInput("EmergencyStop1", SafetyInputType.EmergencyStop, !pressed);
        _logger.LogInformation("模拟急停 {Status}", pressed ? "按下" : "释放");
    }

    /// <summary>
    /// 模拟安全门状态变化
    /// </summary>
    public void SimulateSafetyDoor(bool opened)
    {
        SetSafetyInput("SafetyDoor1", SafetyInputType.SafetyDoor, !opened);
        _logger.LogInformation("模拟安全门 {Status}", opened ? "打开" : "关闭");
    }

    /// <summary>
    /// 模拟驱动故障
    /// </summary>
    public void SimulateDriveFault(bool faulted)
    {
        SetSafetyInput("DriveFault1", SafetyInputType.DriveFault, !faulted);
        _logger.LogInformation("模拟驱动故障 {Status}", faulted ? "触发" : "清除");
    }

    /// <summary>
    /// 模拟外部联锁
    /// </summary>
    public void SimulateInterlock(bool opened)
    {
        SetSafetyInput("Interlock1", SafetyInputType.Interlock, !opened);
        _logger.LogInformation("模拟联锁 {Status}", opened ? "断开" : "闭合");
    }

    /// <summary>
    /// 设置安全输入状态并触发事件
    /// </summary>
    private void SetSafetyInput(string source, SafetyInputType inputType, bool isActive)
    {
        if (!_isMonitoring)
        {
            _logger.LogWarning("监控未启动，无法设置安全输入");
            return;
        }

        var oldState = _safetyInputStates.GetValueOrDefault(source, true);
        if (oldState == isActive)
        {
            return; // 状态未变化
        }

        _safetyInputStates[source] = isActive;

        var eventArgs = new SafetyInputChangedEventArgs
        {
            Source = source,
            InputType = inputType,
            IsActive = isActive,
            OccurredAt = DateTimeOffset.UtcNow
        };

        SafetyInputChanged?.Invoke(this, eventArgs);
    }
}
