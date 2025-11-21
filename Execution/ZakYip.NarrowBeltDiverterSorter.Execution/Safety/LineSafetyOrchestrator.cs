using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Safety;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;
using ZakYip.NarrowBeltDiverterSorter.Observability;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Safety;

/// <summary>
/// 线体安全编排器实现
/// 负责统一管理线体的安全状态和运行状态，协调各子系统的安全响应
/// </summary>
public class LineSafetyOrchestrator : ILineSafetyOrchestrator, IDisposable
{
    private readonly IMainLineDrive _mainLineDrive;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LineSafetyOrchestrator> _logger;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    
    // 使用 volatile 确保跨线程可见性
    private volatile LineRunState _currentLineRunState = LineRunState.Idle;
    private volatile SafetyState _currentSafetyState = SafetyState.Safe;
    
    // 安全输入状态追踪 - 使用线程安全集合
    private readonly ConcurrentDictionary<string, bool> _safetyInputStates = new();
    private bool _disposed;

    public LineSafetyOrchestrator(
        IMainLineDrive mainLineDrive,
        IEventBus eventBus,
        ILogger<LineSafetyOrchestrator> logger)
    {
        _mainLineDrive = mainLineDrive ?? throw new ArgumentNullException(nameof(mainLineDrive));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public LineRunState CurrentLineRunState => _currentLineRunState;

    public SafetyState CurrentSafetyState => _currentSafetyState;

    public async Task<bool> RequestStartAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            // 只有在 Idle 或 Recovering 状态才能启动
            if (_currentLineRunState != LineRunState.Idle && _currentLineRunState != LineRunState.Recovering)
            {
                _logger.LogWarning("无法启动: 当前状态 {CurrentState} 不允许启动", _currentLineRunState);
                return false;
            }

            // 检查安全状态
            if (_currentSafetyState != SafetyState.Safe)
            {
                _logger.LogWarning("无法启动: 安全状态 {SafetyState} 不满足启动条件", _currentSafetyState);
                return false;
            }

            // 转换到 Starting 状态
            TransitionToState(LineRunState.Starting, "开始启动流程");
        }
        finally
        {
            _stateLock.Release();
        }

        try
        {
            // 初始化主线驱动
            _logger.LogInformation("正在初始化主线驱动...");
            var initResult = await _mainLineDrive.InitializeAsync(cancellationToken);
            
            if (!initResult)
            {
                await _stateLock.WaitAsync(cancellationToken);
                try
                {
                    TransitionToState(LineRunState.Faulted, "主线驱动初始化失败");
                }
                finally
                {
                    _stateLock.Release();
                }
                return false;
            }

            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                // 转换到 Running 状态
                TransitionToState(LineRunState.Running, "启动完成");
            }
            finally
            {
                _stateLock.Release();
            }

            _logger.LogInformation("线体已成功启动");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动过程中发生异常");
            await _stateLock.WaitAsync(CancellationToken.None);
            try
            {
                TransitionToState(LineRunState.Faulted, $"启动异常: {ex.Message}");
            }
            finally
            {
                _stateLock.Release();
            }
            return false;
        }
    }

    public async Task<bool> RequestStopAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            // 只有在 Running 或 Paused 状态才能正常停机
            if (_currentLineRunState != LineRunState.Running && _currentLineRunState != LineRunState.Paused)
            {
                _logger.LogWarning("无法停止: 当前状态 {CurrentState} 不允许停止", _currentLineRunState);
                return false;
            }

            // 转换到 Stopping 状态
            TransitionToState(LineRunState.Stopping, "开始停机流程");
        }
        finally
        {
            _stateLock.Release();
        }

        try
        {
            // 关闭主线驱动
            _logger.LogInformation("正在关闭主线驱动...");
            var shutdownResult = await _mainLineDrive.ShutdownAsync(cancellationToken);
            
            if (!shutdownResult)
            {
                await _stateLock.WaitAsync(cancellationToken);
                try
                {
                    TransitionToState(LineRunState.Faulted, "主线驱动关闭失败");
                }
                finally
                {
                    _stateLock.Release();
                }
                return false;
            }

            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                // 转换到 Idle 状态
                TransitionToState(LineRunState.Idle, "停机完成");
            }
            finally
            {
                _stateLock.Release();
            }

            _logger.LogInformation("线体已成功停止");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停机过程中发生异常");
            await _stateLock.WaitAsync(CancellationToken.None);
            try
            {
                TransitionToState(LineRunState.Faulted, $"停机异常: {ex.Message}");
            }
            finally
            {
                _stateLock.Release();
            }
            return false;
        }
    }

    public async Task<bool> RequestPauseAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            // 只有在 Running 状态才能暂停
            if (_currentLineRunState != LineRunState.Running)
            {
                _logger.LogWarning("无法暂停: 当前状态 {CurrentState} 不允许暂停", _currentLineRunState);
                return false;
            }

            // 转换到 Paused 状态
            TransitionToState(LineRunState.Paused, "业务暂停");
            _logger.LogInformation("线体已暂停");
            return true;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<bool> RequestResumeAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            // 只有在 Paused 状态才能恢复
            if (_currentLineRunState != LineRunState.Paused)
            {
                _logger.LogWarning("无法恢复: 当前状态 {CurrentState} 不允许恢复", _currentLineRunState);
                return false;
            }

            // 检查安全状态
            if (_currentSafetyState != SafetyState.Safe)
            {
                _logger.LogWarning("无法恢复: 安全状态 {SafetyState} 不满足恢复条件", _currentSafetyState);
                return false;
            }

            // 转换到 Running 状态
            TransitionToState(LineRunState.Running, "从暂停恢复");
            _logger.LogInformation("线体已从暂停恢复");
            return true;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<bool> AcknowledgeFaultAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            // 只有在 Faulted 或 SafetyStopped 状态才能确认故障
            if (_currentLineRunState != LineRunState.Faulted && _currentLineRunState != LineRunState.SafetyStopped)
            {
                _logger.LogWarning("无法确认故障: 当前状态 {CurrentState} 不是故障状态", _currentLineRunState);
                return false;
            }

            // 检查安全状态是否已恢复
            if (_currentSafetyState != SafetyState.Safe)
            {
                _logger.LogWarning("无法确认故障: 安全状态 {SafetyState} 未恢复", _currentSafetyState);
                return false;
            }

            // 转换到 Recovering 状态
            TransitionToState(LineRunState.Recovering, "故障已确认，进入恢复流程");
            _logger.LogInformation("故障已确认，可以尝试重新启动");
            
            // 从 Recovering 直接进入 Idle，允许重新启动
            TransitionToState(LineRunState.Idle, "恢复完成");
            return true;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async void ReportSafetyInput(SafetyInputChangedEventArgs eventArgs)
    {
        // 更新安全输入状态 - ConcurrentDictionary 已线程安全
        _safetyInputStates[eventArgs.Source] = eventArgs.IsActive;

        _logger.LogInformation(
            "安全输入变化: {Source} ({InputType}) = {IsActive}",
            eventArgs.Source,
            eventArgs.InputType,
            eventArgs.IsActive ? "安全" : "不安全");

        await _stateLock.WaitAsync();
        try
        {
            // 计算新的安全状态
            var newSafetyState = CalculateSafetyState();

            // 如果安全状态变化，更新并通知
            if (newSafetyState != _currentSafetyState)
            {
                var oldSafetyState = _currentSafetyState;
                _currentSafetyState = newSafetyState;

                var safetyEventArgs = new SafetyStateChangedEventArgs
                {
                    State = newSafetyState,
                    Source = eventArgs.Source,
                    Message = $"安全状态从 {oldSafetyState} 变更为 {newSafetyState}",
                    OccurredAt = eventArgs.OccurredAt
                };

                _logger.LogWarning(
                    "安全状态变化: {OldState} -> {NewState}, 源: {Source}",
                    oldSafetyState,
                    newSafetyState,
                    eventArgs.Source);

                // 同时发布到事件总线 (Observability层事件)
                var observabilitySafetyEvent = new Observability.Events.SafetyStateChangedEventArgs
                {
                    State = newSafetyState.ToString(),
                    Source = eventArgs.Source,
                    Message = safetyEventArgs.Message,
                    OccurredAt = safetyEventArgs.OccurredAt
                };
                _ = _eventBus.PublishAsync(observabilitySafetyEvent);

                // 如果安全状态变为不安全，且线体正在运行，触发安全停机
                if (newSafetyState != SafetyState.Safe && 
                    (_currentLineRunState == LineRunState.Running || 
                     _currentLineRunState == LineRunState.Paused ||
                     _currentLineRunState == LineRunState.Starting))
                {
                    var lineStateToTransition = newSafetyState == SafetyState.DriveFault 
                        ? LineRunState.Faulted 
                        : LineRunState.SafetyStopped;

                    TransitionToState(lineStateToTransition, $"因安全原因停机: {newSafetyState}");

                    // 异步执行紧急停机
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogWarning("执行紧急停机...");
                            await _mainLineDrive.ShutdownAsync(CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "紧急停机过程中发生异常");
                        }
                    });
                }
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private SafetyState CalculateSafetyState()
    {
        // 检查是否有任何不安全的输入
        foreach (var kvp in _safetyInputStates)
        {
            if (!kvp.Value) // 如果输入不安全
            {
                // 根据输入源名称推断类型
                if (kvp.Key.Contains("EmergencyStop", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("EStop", StringComparison.OrdinalIgnoreCase))
                {
                    return SafetyState.EmergencyStop;
                }
                else if (kvp.Key.Contains("DriveFault", StringComparison.OrdinalIgnoreCase) ||
                         kvp.Key.Contains("VFD", StringComparison.OrdinalIgnoreCase))
                {
                    return SafetyState.DriveFault;
                }
                else if (kvp.Key.Contains("Interlock", StringComparison.OrdinalIgnoreCase))
                {
                    return SafetyState.InterlockOpen;
                }
                else if (kvp.Key.Contains("SafetyDoor", StringComparison.OrdinalIgnoreCase) ||
                         kvp.Key.Contains("Door", StringComparison.OrdinalIgnoreCase))
                {
                    return SafetyState.UnsafeInput;
                }
                else
                {
                    return SafetyState.UnsafeInput;
                }
            }
        }

        // 所有输入都安全
        return SafetyState.Safe;
    }

    private void TransitionToState(LineRunState newState, string message)
    {
        var oldState = _currentLineRunState;
        _currentLineRunState = newState;

        var eventArgs = new LineRunStateChangedEventArgs
        {
            State = newState,
            Message = message,
            OccurredAt = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "线体状态变化: {OldState} -> {NewState}, 原因: {Message}",
            oldState,
            newState,
            message);

        // 同时发布到事件总线 (Observability层事件)
        var observabilityEvent = new Observability.Events.LineRunStateChangedEventArgs
        {
            State = newState.ToString(),
            Message = message,
            OccurredAt = eventArgs.OccurredAt
        };
        _ = _eventBus.PublishAsync(observabilityEvent);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stateLock?.Dispose();
        _disposed = true;
    }
}
